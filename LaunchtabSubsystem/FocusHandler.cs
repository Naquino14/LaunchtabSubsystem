using System.Runtime.InteropServices;
using System.Diagnostics;

public class FocusHandler
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetForegroundWindow(IntPtr hWnd, ShowWindowEnum flags);

    [DllImport("user32.dll")]
    private static extern int SetForegroundWindow(IntPtr hWnd);

    public enum ShowWindowEnum
    {
        Hide = 0,
        ShowNormal = 1,
        ShowMinimized = 2,
        ShowMaximized = 3,
        Maximize = 3,
        ShowNormalNoActivate = 4,
        Show = 5,
        Minimize = 6,
        ShowMinNoActivate = 7,
        ShowNoActivate = 8,
        Restore = 9,
        ShowDefault = 10,
        ForceMinimized = 11
    }

    public static void Focus() => SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);

    public static void Focus(ShowWindowEnum flag) => SetForegroundWindow(flag);

    
}