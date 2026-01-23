using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfCanvas = System.Windows.Controls.Canvas;

namespace ScreenCapture.Editor.Export;

public class ImageExporter
{
    public static void SaveToFile(Image backgroundImage, WpfCanvas annotationCanvas, string filePath)
    {
        var bitmap = RenderToBitmap(backgroundImage, annotationCanvas);
        var encoder = GetEncoder(filePath);
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = File.OpenWrite(filePath);
        encoder.Save(stream);
    }

    public static void CopyToClipboard(Image backgroundImage, WpfCanvas annotationCanvas)
    {
        var bitmap = RenderToBitmap(backgroundImage, annotationCanvas);
        Clipboard.SetImage(bitmap);
    }

    public static RenderTargetBitmap RenderToBitmap(Image backgroundImage, WpfCanvas annotationCanvas)
    {
        int width = (int)annotationCanvas.Width;
        int height = (int)annotationCanvas.Height;

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

        var drawingVisual = new DrawingVisual();
        using (var context = drawingVisual.RenderOpen())
        {
            context.DrawImage(backgroundImage.Source, new Rect(0, 0, width, height));
            var annotationBrush = new VisualBrush(annotationCanvas);
            context.DrawRectangle(annotationBrush, null, new Rect(0, 0, width, height));
        }

        bitmap.Render(drawingVisual);
        return bitmap;
    }

    private static BitmapEncoder GetEncoder(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 95 },
            ".bmp" => new BmpBitmapEncoder(),
            ".gif" => new GifBitmapEncoder(),
            _ => new PngBitmapEncoder()
        };
    }

    public static RenderTargetBitmap CropImage(RenderTargetBitmap source, Rect cropRegion)
    {
        var croppedBitmap = new CroppedBitmap(
            source,
            new Int32Rect((int)cropRegion.X, (int)cropRegion.Y, (int)cropRegion.Width, (int)cropRegion.Height)
        );

        var result = new RenderTargetBitmap(
            (int)cropRegion.Width,
            (int)cropRegion.Height,
            96, 96, PixelFormats.Pbgra32
        );

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(croppedBitmap, new Rect(0, 0, cropRegion.Width, cropRegion.Height));
        }
        result.Render(visual);

        return result;
    }
}
