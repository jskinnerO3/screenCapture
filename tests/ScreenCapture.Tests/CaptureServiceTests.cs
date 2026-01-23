using ScreenCapture.Core.Capture;
using ScreenCapture.Core.Models;
using Xunit;

namespace ScreenCapture.Tests;

public class CaptureServiceTests
{
    [Fact]
    public void ScreenCaptureService_CaptureFullScreen_ReturnsValidResult()
    {
        var service = new ScreenCaptureService();
        var result = service.CaptureFullScreen();

        Assert.NotNull(result);
        Assert.Equal(CaptureType.FullScreen, result.Type);
        Assert.True(result.CaptureRegion.Width > 0);
        Assert.True(result.CaptureRegion.Height > 0);

        result.Dispose();
    }

    [Fact]
    public void ScreenCaptureService_CapturePrimaryMonitor_ReturnsValidResult()
    {
        var service = new ScreenCaptureService();
        var result = service.CapturePrimaryMonitor();

        Assert.NotNull(result);
        Assert.Equal(CaptureType.FullScreen, result.Type);

        result.Dispose();
    }

    [Fact]
    public void WindowCaptureService_GetOpenWindows_ReturnsWindows()
    {
        var service = new WindowCaptureService();
        var windows = service.GetOpenWindows();

        Assert.NotNull(windows);
        Assert.True(windows.Count > 0);
    }

    [Fact]
    public void RegionCaptureService_NormalizeRectangle_HandlesInvertedCoordinates()
    {
        var start = new System.Drawing.Point(100, 100);
        var end = new System.Drawing.Point(50, 50);

        var rect = RegionCaptureService.NormalizeRectangle(start, end);

        Assert.Equal(50, rect.X);
        Assert.Equal(50, rect.Y);
        Assert.Equal(50, rect.Width);
        Assert.Equal(50, rect.Height);
    }
}
