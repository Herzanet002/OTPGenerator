using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace OTPGenerator;

internal static class WindowUtils
{
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
        string? lpszWindow);

    [DllImport("user32.dll")]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, uint msg, int wParam, string lParam);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    internal static int SendMessageNative(IntPtr hWnd, uint msg, int wParam, string lParam)
        => SendMessage(hWnd, msg, wParam, lParam);

    internal static IntPtr FindWindowExNative(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
        string? lpszWindow)
        => FindWindowEx(hwndParent, hwndChildAfter, lpszClass, lpszWindow);

    internal static IntPtr FindWindow(string findPattern)
    {
        var result = IntPtr.Zero;
        EnumWindows((hWnd, lParam) =>
        {
            var sb = new System.Text.StringBuilder(256);
            GetWindowText(hWnd, sb, 256);
            var title = sb.ToString();

            if (!Regex.IsMatch(title, findPattern))
            {
                return true;
            }

            result = hWnd;
            return false;
        }, IntPtr.Zero);

        return result;
    }
}