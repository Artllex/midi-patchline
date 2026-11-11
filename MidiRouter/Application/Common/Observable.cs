using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace MidiRouter;

public abstract class Observable : INotifyPropertyChanged
{
    public bool IsRenaming { get; private set; }
    public string DraftName { get; set; } = "";
    public void BeginRename(string name)
    {
        DraftName = name;
        Changed(nameof(DraftName));
        IsRenaming = true;
        Changed(nameof(IsRenaming));
    }
    public void CancelRename()
    {
        IsRenaming = false;
        Changed(nameof(IsRenaming));
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
