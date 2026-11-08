param([Parameter(Mandatory=$true)][string]$Png,[Parameter(Mandatory=$true)][string]$Ico)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$source=[Drawing.Bitmap]::new($Png)
$chunks=[Collections.Generic.List[byte[]]]::new()
$sizes=@(16,24,32,48,64,128,256)
foreach($size in $sizes){
 $bitmap=[Drawing.Bitmap]::new($size,$size,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $graphics=[Drawing.Graphics]::FromImage($bitmap)
 $graphics.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
 $graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 $graphics.Clear([Drawing.Color]::Transparent)
 $graphics.DrawImage($source,0,0,$size,$size)
 $stream=[IO.MemoryStream]::new()
 $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
 $chunks.Add($stream.ToArray())
 $stream.Dispose();$graphics.Dispose();$bitmap.Dispose()
}
$source.Dispose()
$writer=[IO.BinaryWriter]::new([IO.File]::Create($Ico))
$writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$sizes.Count)
$offset=6+16*$sizes.Count
for($i=0;$i -lt $sizes.Count;$i++){
 $dimension=if($sizes[$i] -eq 256){0}else{$sizes[$i]}
 $writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0)
 $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$chunks[$i].Length);$writer.Write([uint32]$offset)
 $offset+=$chunks[$i].Length
}
foreach($chunk in $chunks){$writer.Write($chunk)}
$writer.Dispose()
