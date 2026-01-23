using System.Drawing;
using ScreenCapture.Core.Models;

namespace ScreenCapture.Core.Capture;

public class RegionCaptureService
{
    private readonly ScreenCaptureService _screenCaptureService;

    public RegionCaptureService()
    {
        _screenCaptureService = new ScreenCaptureService();
    }

    public CaptureResult CaptureRegion(Rectangle region)
    {
        if (region.Width <= 0 || region.Height <= 0)
        {
            return new CaptureResult
            {
                CapturedAt = DateTime.Now,
                Type = CaptureType.Region,
                CaptureRegion = region
            };
        }

        return _screenCaptureService.CaptureRegion(region);
    }

    public CaptureResult CaptureRegion(Point start, Point end)
    {
        var region = NormalizeRectangle(start, end);
        return CaptureRegion(region);
    }

    public static Rectangle NormalizeRectangle(Point start, Point end)
    {
        int x = Math.Min(start.X, end.X);
        int y = Math.Min(start.Y, end.Y);
        int width = Math.Abs(end.X - start.X);
        int height = Math.Abs(end.Y - start.Y);

        return new Rectangle(x, y, width, height);
    }
}
