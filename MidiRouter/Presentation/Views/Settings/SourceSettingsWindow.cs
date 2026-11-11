using System.Windows;
using System.Windows.Controls;
namespace MidiRouter;

public sealed class SourceSettingsWindow : SettingsWindow
{
    public SourceSettingsWindow(RouterViewModel model, Source source) : base(model, source) { }
}
