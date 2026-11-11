using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MidiRouter;

public static class SettingsFooter
{
    public static void Attach(Panel footer)
    {
        var buttons = footer.Children.OfType<Button>().ToArray();
        var selected = buttons.First(button => button.IsDefault);
        void Select(Button button)
        {
            selected = button;
            foreach (var candidate in buttons)
                candidate.IsDefault = candidate == button;
        }
        foreach (var button in buttons)
        {
            button.MinWidth = 86;
            button.FontSize = 14;
            button.GotKeyboardFocus += (_, _) => Select(button);
        }
        footer.PreviewKeyDown += (_, e) =>
        {
            if (e.Key is not (Key.Left or Key.Right))
                return;
            int index = Array.FindIndex(buttons, button => button.IsKeyboardFocusWithin);
            if (index < 0)
                return;
            int next = Math.Clamp(index + (e.Key == Key.Left ? -1 : 1), 0, buttons.Length - 1);
            Select(buttons[next]);
            buttons[next].Focus();
            e.Handled = true;
        };
        if (footer.IsLoaded)
            footer.Dispatcher.BeginInvoke(new Action(() => selected.Focus()));
        else
            footer.Loaded += (_, _) => selected.Focus();
    }
}
