using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using ScreenCapture.Core.Models;

namespace ScreenCapture.Core.Capture;

public class WindowCaptureService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    private const uint PW_RENDERFULLCONTENT = 2;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public Rectangle ToRectangle() => new Rectangle(Left, Top, Right - Left, Bottom - Top);
    }

    public CaptureResult CaptureActiveWindow()
    {
        var hwnd = GetForegroundWindow();
        return CaptureWindow(hwnd);
    }

    public CaptureResult CaptureWindow(IntPtr hwnd)
    {
        var result = new CaptureResult
        {
            CapturedAt = DateTime.Now,
            Type = CaptureType.Window
        };

        try
        {
            if (hwnd == IntPtr.Zero)
            {
                return result;
            }

            result.SourceWindowTitle = GetWindowTitle(hwnd);
            var rect = GetWindowRectangle(hwnd);
            result.CaptureRegion = rect;

            if (rect.Width > 0 && rect.Height > 0)
            {
                result.Image = CaptureWindowBitmap(hwnd, rect);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Window capture failed: {ex.Message}");
        }

        return result;
    }

    private Bitmap CaptureWindowBitmap(IntPtr hwnd, Rectangle rect)
    {
        var bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        var hdc = graphics.GetHdc();

        try
        {
            if (!PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT))
            {
                graphics.ReleaseHdc(hdc);
                graphics.CopyFromScreen(rect.Location, Point.Empty, rect.Size, CopyPixelOperation.SourceCopy);
            }
            else
            {
                graphics.ReleaseHdc(hdc);
            }
        }
        catch
        {
            try { graphics.ReleaseHdc(hdc); } catch { }
            graphics.CopyFromScreen(rect.Location, Point.Empty, rect.Size, CopyPixelOperation.SourceCopy);
        }

        return bitmap;
    }

    public static Rectangle GetWindowRectangle(IntPtr hwnd)
    {
        if (DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT dwmRect, Marshal.SizeOf<RECT>()) == 0)
        {
            return dwmRect.ToRectangle();
        }

        GetWindowRect(hwnd, out RECT rect);
        return rect.ToRectangle();
    }

    public static string GetWindowTitle(IntPtr hwnd)
    {
        int length = GetWindowTextLength(hwnd);
        if (length == 0) return string.Empty;

        var sb = new StringBuilder(length + 1);
        GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    public List<WindowInfo> GetOpenWindows()
    {
        var windows = new List<WindowInfo>();

        EnumWindows((hwnd, lParam) =>
        {
            if (IsWindowVisible(hwnd))
            {
                var title = GetWindowTitle(hwnd);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    var rect = GetWindowRectangle(hwnd);
                    if (rect.Width > 0 && rect.Height > 0)
                    {
                        windows.Add(new WindowInfo
                        {
                            Handle = hwnd,
                            Title = title,
                            Bounds = rect
                        });
                    }
                }
            }
            return true;
        }, IntPtr.Zero);

        return windows;
    }
}

public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public Rectangle Bounds { get; set; }
    public Bitmap? Thumbnail { get; set; }
}
