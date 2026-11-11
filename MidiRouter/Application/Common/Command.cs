using System.Windows.Input;
namespace MidiRouter;

public sealed class Command : ICommand
{
    private readonly Action<object?> execute;
    public Command(Action<object?> action) => execute = action;
    public bool CanExecute(object? p) => true;
    public void Execute(object? p) => execute(p);
    public event EventHandler? CanExecuteChanged { add { } remove { } }
}
