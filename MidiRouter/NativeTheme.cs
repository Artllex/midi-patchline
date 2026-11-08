using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
namespace MidiRouter;
public static class NativeTheme
{
 [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr handle,int attribute,ref int value,int size);
 [StructLayout(LayoutKind.Sequential)] private struct CaptionRect {public int Left,Top,Right,Bottom;}
 [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr window,int attribute,out CaptionRect rect,int size);
 public static bool HasNativeCaptionButtons(Window window){
  int result=DwmGetWindowAttribute(new WindowInteropHelper(window).Handle,5,out var rect,Marshal.SizeOf<CaptionRect>());
  return result==0 && rect.Right>rect.Left && rect.Bottom>rect.Top;
 }
 public static void Apply(Window window){
  var handle=new WindowInteropHelper(window).Handle;
  int corners=2;DwmSetWindowAttribute(handle,33,ref corners,4);
  int dark=1;DwmSetWindowAttribute(handle,20,ref dark,4);
  int caption=0x00221B1C;DwmSetWindowAttribute(handle,35,ref caption,4);
  int text=0x00FEFBFB;DwmSetWindowAttribute(handle,36,ref text,4);
 }
}


