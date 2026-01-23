using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfCanvas = System.Windows.Controls.Canvas;

namespace ScreenCapture.Editor.Tools;

public class CropTool : BaseTool
{
    public override string Name => "Crop";

    private Rectangle? _cropRect;
    private Rect _cropRegion;

    public Rect CropRegion => _cropRegion;

    public event EventHandler<Rect>? CropRegionSelected;

    public override void OnMouseDown(Point position)
    {
        base.OnMouseDown(position);

        _cropRect = new Rectangle
        {
            Stroke = new SolidColorBrush(Colors.Blue),
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 4, 2 },
            Fill = new SolidColorBrush(Color.FromArgb(50, 0, 0, 255))
        };

        WpfCanvas.SetLeft(_cropRect, position.X);
        WpfCanvas.SetTop(_cropRect, position.Y);
        CurrentElement = _cropRect;
    }

    public override void OnMouseMove(Point position)
    {
        if (!IsDrawing || _cropRect == null) return;

        double x = Math.Min(StartPoint.X, position.X);
        double y = Math.Min(StartPoint.Y, position.Y);
        double width = Math.Abs(position.X - StartPoint.X);
        double height = Math.Abs(position.Y - StartPoint.Y);

        WpfCanvas.SetLeft(_cropRect, x);
        WpfCanvas.SetTop(_cropRect, y);
        _cropRect.Width = width;
        _cropRect.Height = height;

        _cropRegion = new Rect(x, y, width, height);
    }

    public override void OnMouseUp(Point position)
    {
        if (_cropRegion.Width > 5 && _cropRegion.Height > 5)
        {
            CropRegionSelected?.Invoke(this, _cropRegion);
        }
        base.OnMouseUp(position);
    }
}
