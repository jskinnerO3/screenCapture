using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ScreenCapture.Core.Models;

namespace ScreenCapture.Core.Capture;

public class ScrollingCaptureService
{
    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    private const uint MOUSEEVENTF_WHEEL = 0x0800;

    private readonly ScreenCaptureService _captureService;

    public int ScrollDelay { get; set; } = 300;
    public int ScrollAmount { get; set; } = 3;
    public int MaxScrolls { get; set; } = 50;
    public int OverlapThreshold { get; set; } = 50;

    public ScrollingCaptureService()
    {
        _captureService = new ScreenCaptureService();
    }

    public async Task<CaptureResult> CaptureScrollingRegionAsync(Rectangle region, CancellationToken cancellationToken = default)
    {
        var result = new CaptureResult
        {
            CapturedAt = DateTime.Now,
            Type = CaptureType.ScrollingCapture,
            CaptureRegion = region
        };

        var frames = new List<Bitmap>();

        try
        {
            Bitmap? previousFrame = null;
            int unchangedCount = 0;

            for (int i = 0; i < MaxScrolls && !cancellationToken.IsCancellationRequested; i++)
            {
                await Task.Delay(ScrollDelay, cancellationToken);

                var capture = _captureService.CaptureRegion(region);
                if (capture.Image == null) break;

                var currentFrame = new Bitmap(capture.Image);

                if (previousFrame != null)
                {
                    var similarity = CalculateImageSimilarity(previousFrame, currentFrame);
                    if (similarity > 0.99)
                    {
                        unchangedCount++;
                        if (unchangedCount >= 2)
                        {
                            currentFrame.Dispose();
                            break;
                        }
                    }
                    else
                    {
                        unchangedCount = 0;
                    }
                }

                frames.Add(currentFrame);
                previousFrame?.Dispose();
                previousFrame = new Bitmap(currentFrame);

                ScrollDown();
            }

            previousFrame?.Dispose();

            if (frames.Count > 0)
            {
                result.Image = StitchImages(frames);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            foreach (var frame in frames)
            {
                frame.Dispose();
            }
        }

        return result;
    }

    private void ScrollDown()
    {
        for (int i = 0; i < ScrollAmount; i++)
        {
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, -120, 0);
            Thread.Sleep(50);
        }
    }

    private static double CalculateImageSimilarity(Bitmap img1, Bitmap img2)
    {
        if (img1.Width != img2.Width || img1.Height != img2.Height)
            return 0;

        int sampleSize = 100;
        int matchCount = 0;
        var rand = new Random(42);

        for (int i = 0; i < sampleSize; i++)
        {
            int x = rand.Next(img1.Width);
            int y = rand.Next(img1.Height);

            var pixel1 = img1.GetPixel(x, y);
            var pixel2 = img2.GetPixel(x, y);

            if (Math.Abs(pixel1.R - pixel2.R) < 10 &&
                Math.Abs(pixel1.G - pixel2.G) < 10 &&
                Math.Abs(pixel1.B - pixel2.B) < 10)
            {
                matchCount++;
            }
        }

        return matchCount / (double)sampleSize;
    }

    private Bitmap StitchImages(List<Bitmap> frames)
    {
        if (frames.Count == 0)
            throw new ArgumentException("No frames to stitch");

        if (frames.Count == 1)
            return new Bitmap(frames[0]);

        var stitchedFrames = new List<(Bitmap Image, int OverlapTop)>();
        stitchedFrames.Add((frames[0], 0));

        for (int i = 1; i < frames.Count; i++)
        {
            var overlap = FindOverlap(frames[i - 1], frames[i]);
            stitchedFrames.Add((frames[i], overlap));
        }

        int totalHeight = stitchedFrames[0].Image.Height;
        for (int i = 1; i < stitchedFrames.Count; i++)
        {
            totalHeight += stitchedFrames[i].Image.Height - stitchedFrames[i].OverlapTop;
        }

        int width = frames[0].Width;
        var result = new Bitmap(width, totalHeight, PixelFormat.Format32bppArgb);

        using var graphics = Graphics.FromImage(result);
        graphics.Clear(Color.White);

        int currentY = 0;
        for (int i = 0; i < stitchedFrames.Count; i++)
        {
            var (image, overlap) = stitchedFrames[i];
            int sourceY = i == 0 ? 0 : overlap;
            int sourceHeight = image.Height - sourceY;

            graphics.DrawImage(
                image,
                new Rectangle(0, currentY, width, sourceHeight),
                new Rectangle(0, sourceY, width, sourceHeight),
                GraphicsUnit.Pixel
            );

            currentY += sourceHeight;
        }

        return result;
    }

    private int FindOverlap(Bitmap top, Bitmap bottom)
    {
        int height = Math.Min(top.Height, bottom.Height);
        int maxOverlap = height / 2;

        for (int overlap = OverlapThreshold; overlap < maxOverlap; overlap += 10)
        {
            bool match = true;
            int samplePoints = 20;

            for (int s = 0; s < samplePoints && match; s++)
            {
                int x = (top.Width * s) / samplePoints;
                int topY = top.Height - overlap;

                if (topY < 0 || topY >= top.Height) continue;

                var topPixel = top.GetPixel(x, topY);
                var bottomPixel = bottom.GetPixel(x, 0);

                if (Math.Abs(topPixel.R - bottomPixel.R) > 20 ||
                    Math.Abs(topPixel.G - bottomPixel.G) > 20 ||
                    Math.Abs(topPixel.B - bottomPixel.B) > 20)
                {
                    match = false;
                }
            }

            if (match)
            {
                return overlap;
            }
        }

        return OverlapThreshold;
    }
}
