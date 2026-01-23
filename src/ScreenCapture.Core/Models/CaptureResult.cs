using System.Drawing;

namespace ScreenCapture.Core.Models;

public class CaptureResult
{
    public Bitmap? Image { get; set; }
    public DateTime CapturedAt { get; set; }
    public CaptureType Type { get; set; }
    public Rectangle CaptureRegion { get; set; }
    public string? SourceWindowTitle { get; set; }
    public string? SourceMonitorName { get; set; }

    public bool IsSuccess => Image != null;

    public void Dispose()
    {
        Image?.Dispose();
        Image = null;
    }
}

public enum CaptureType
{
    FullScreen,
    Window,
    Region,
    ScrollingCapture
}
