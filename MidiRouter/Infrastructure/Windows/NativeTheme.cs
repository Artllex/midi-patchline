using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
namespace MidiRouter;

public static class NativeTheme
{
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);
    [StructLayout(LayoutKind.Sequential)]
    private struct CaptionRect
    {
        public int Left, Top, Right, Bottom;
    }
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out CaptionRect rect, int size);
    public static bool HasNativeCaptionButtons(Window window)
    {
        int result = DwmGetWindowAttribute(new WindowInteropHelper(window).Handle, 5, out var rect, Marshal.SizeOf<CaptionRect>());
        return result == 0 && rect.Right > rect.Left && rect.Bottom > rect.Top;
    }
    public static void Apply(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return;
        int corners = 2;
        DwmSetWindowAttribute(handle, 33, ref corners, 4);
        int dark = UiPresentation.IsLightTheme ? 0 : 1;
        DwmSetWindowAttribute(handle, 20, ref dark, 4);
        var captionColor = ((System.Windows.Media.SolidColorBrush)UiPresentation.ThemeBrush(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(28, 27, 34)))).Color;
        int caption = captionColor.R | (captionColor.G << 8) | (captionColor.B << 16);
        DwmSetWindowAttribute(handle, 35, ref caption, 4);
        var textColor = ((System.Windows.Media.SolidColorBrush)UiPresentation.ThemeBrush(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 251, 254)), "Foreground")).Color;
        int text = textColor.R | (textColor.G << 8) | (textColor.B << 16);
        DwmSetWindowAttribute(handle, 36, ref text, 4);
    }
}
