using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using ScreenCapture.Core.Models;

namespace ScreenCapture.Core.Capture;

public class ScreenCaptureService
{
    public CaptureResult CaptureFullScreen()
    {
        var result = new CaptureResult
        {
            CapturedAt = DateTime.Now,
            Type = CaptureType.FullScreen
        };

        try
        {
            var bounds = GetVirtualScreenBounds();
            result.CaptureRegion = bounds;
            result.Image = CaptureRegionInternal(bounds);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Full screen capture failed: {ex.Message}");
        }

        return result;
    }

    public CaptureResult CapturePrimaryMonitor()
    {
        var result = new CaptureResult
        {
            CapturedAt = DateTime.Now,
            Type = CaptureType.FullScreen
        };

        try
        {
            var primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen != null)
            {
                result.CaptureRegion = primaryScreen.Bounds;
                result.SourceMonitorName = primaryScreen.DeviceName;
                result.Image = CaptureRegionInternal(primaryScreen.Bounds);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Primary monitor capture failed: {ex.Message}");
        }

        return result;
    }

    public CaptureResult CaptureMonitor(int monitorIndex)
    {
        var result = new CaptureResult
        {
            CapturedAt = DateTime.Now,
            Type = CaptureType.FullScreen
        };

        try
        {
            var screens = Screen.AllScreens;
            if (monitorIndex >= 0 && monitorIndex < screens.Length)
            {
                var screen = screens[monitorIndex];
                result.CaptureRegion = screen.Bounds;
                result.SourceMonitorName = screen.DeviceName;
                result.Image = CaptureRegionInternal(screen.Bounds);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Monitor capture failed: {ex.Message}");
        }

        return result;
    }

    public CaptureResult CaptureRegion(Rectangle region)
    {
        var result = new CaptureResult
        {
            CapturedAt = DateTime.Now,
            Type = CaptureType.Region,
            CaptureRegion = region
        };

        try
        {
            result.Image = CaptureRegionInternal(region);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Region capture failed: {ex.Message}");
        }

        return result;
    }

    private Bitmap CaptureRegionInternal(Rectangle region)
    {
        var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(region.Location, Point.Empty, region.Size, CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    public static Rectangle GetVirtualScreenBounds()
    {
        return new Rectangle(
            SystemInformation.VirtualScreen.X,
            SystemInformation.VirtualScreen.Y,
            SystemInformation.VirtualScreen.Width,
            SystemInformation.VirtualScreen.Height
        );
    }

    public static Screen[] GetAllMonitors()
    {
        return Screen.AllScreens;
    }
}
