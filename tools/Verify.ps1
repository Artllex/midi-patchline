param(
    [string]$OutputDirectory,
    [string[]]$Checks = @("self-test", "startup-check", "settings-check")
)
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $repoRoot "MidiRouter\MidiRouter.csproj"
$checksProject = Join-Path $repoRoot "tests\MidiPatchline.Checks\MidiPatchline.Checks.csproj"
if (!$OutputDirectory) { $OutputDirectory = Join-Path $repoRoot "publish" }

dotnet publish $appProject -c Release --self-contained false -o $OutputDirectory --nologo
if ($LASTEXITCODE -ne 0) { throw "Application build failed." }
dotnet build $checksProject -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw "Checks build failed." }
$checksAssembly = Join-Path $repoRoot "tests\MidiPatchline.Checks\bin\Release\net6.0-windows\MidiPatchline.Checks.dll"
Copy-Item -LiteralPath $checksAssembly -Destination (Join-Path $OutputDirectory "MidiPatchline.Checks.dll") -Force
$exe = Join-Path $OutputDirectory "MidiPatchline.exe"
foreach ($check in $Checks) {
    if ($check -notin @("self-test", "startup-check", "settings-check")) { throw "Unknown check: $check" }
    $errorFile = Join-Path $OutputDirectory "$check-error.txt"
    $resultFile = Join-Path $OutputDirectory "$check-result.txt"
    foreach ($oldResult in @($errorFile, $resultFile, (Join-Path $OutputDirectory "diagnostic-error.txt"))) {
        if (Test-Path -LiteralPath $oldResult) { Remove-Item -LiteralPath $oldResult }
    }
    $process = Start-Process -FilePath $exe -ArgumentList "--$check" -PassThru -WindowStyle Hidden
    if (!$process.WaitForExit(30000)) {
        Stop-Process -Id $process.Id -Force
        throw "Check timed out: $check"
    }
    $process.Refresh()
    if ((Test-Path -LiteralPath $errorFile) -or
        (Test-Path -LiteralPath (Join-Path $OutputDirectory "diagnostic-error.txt")) -or
        !(Test-Path -LiteralPath $resultFile)) {
        throw "Check has no verified success result: $check. Inspect *-error.txt in $OutputDirectory."
    }
    if ($process.ExitCode -ne 0) { throw "Check failed: $check. Inspect *-error.txt in $OutputDirectory." }
    Write-Output "PASS $check"
}
