using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MidiRouter;

internal static class SplitKeyboardPreview
{
    public static void Draw(Canvas keyboard, int low, int high)
    {
        keyboard.Children.Clear();
        bool IsBlack(int n) => new[] { 1, 3, 6, 8, 10 }.Contains(n % 12);
        double width = keyboard.ActualWidth / Enumerable.Range(0, 128).Count(n => !IsBlack(n));
        int whiteIndex = 0;
        for (int n = 0; n < 128; n++)
        {
            if (IsBlack(n))
                continue;
            var key = new Rectangle { Width = Math.Max(.5, width - .35), Height = 32, Fill = n >= low && n <= high ? new SolidColorBrush(Color.FromRgb(53, 203, 125)) : Brushes.LightGray };
            Canvas.SetLeft(key, whiteIndex++ * width);
            Canvas.SetTop(key, 0);
            keyboard.Children.Add(key);
        }
        whiteIndex = 0;
        for (int n = 0; n < 128; n++)
        {
            if (!IsBlack(n))
            {
                whiteIndex++;
                continue;
            }
            var key = new Rectangle { Width = Math.Max(.5, width * .62), Height = 20, Fill = n >= low && n <= high ? new SolidColorBrush(Color.FromRgb(22, 110, 67)) : new SolidColorBrush(Color.FromRgb(24, 27, 32)) };
            Canvas.SetLeft(key, (whiteIndex - .31) * width);
            Canvas.SetTop(key, 0);
            keyboard.Children.Add(key);
        }
    }
}
