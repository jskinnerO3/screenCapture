using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfCanvas = System.Windows.Controls.Canvas;

namespace ScreenCapture.Editor.Tools;

public class EllipseTool : BaseTool
{
    public override string Name => "Ellipse";

    private Ellipse? _ellipse;

    public override void OnMouseDown(Point position)
    {
        base.OnMouseDown(position);

        _ellipse = new Ellipse
        {
            Stroke = new SolidColorBrush(CurrentColor),
            StrokeThickness = StrokeWidth,
            Fill = Brushes.Transparent
        };

        WpfCanvas.SetLeft(_ellipse, position.X);
        WpfCanvas.SetTop(_ellipse, position.Y);
        CurrentElement = _ellipse;
    }

    public override void OnMouseMove(Point position)
    {
        if (!IsDrawing || _ellipse == null) return;

        double x = Math.Min(StartPoint.X, position.X);
        double y = Math.Min(StartPoint.Y, position.Y);
        double width = Math.Abs(position.X - StartPoint.X);
        double height = Math.Abs(position.Y - StartPoint.Y);

        WpfCanvas.SetLeft(_ellipse, x);
        WpfCanvas.SetTop(_ellipse, y);
        _ellipse.Width = width;
        _ellipse.Height = height;
    }
}
