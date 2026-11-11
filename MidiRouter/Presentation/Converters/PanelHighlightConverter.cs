using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
namespace MidiRouter;

public sealed class PanelHighlightConverter : IValueConverter
{
    public object Convert(object value, System.Type type, object parameter, CultureInfo culture)
    {
        var c = (value as SolidColorBrush)?.Color ?? Color.FromRgb(38, 43, 50);
        byte Light(byte channel) => (byte)System.Math.Min(255, System.Math.Round(channel * 1.25));
        var brush = new SolidColorBrush(Color.FromRgb(Light(c.R), Light(c.G), Light(c.B)));
        brush.Freeze();
        return brush;
    }
    public object ConvertBack(object value, System.Type type, object parameter, CultureInfo culture) => throw new System.NotSupportedException();
}
