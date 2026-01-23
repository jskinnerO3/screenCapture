using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using WpfCanvas = System.Windows.Controls.Canvas;

namespace ScreenCapture.Editor.Tools;

public class BlurTool : BaseTool
{
    public override string Name => "Blur";

    private Rectangle? _rectangle;

    public int PixelSize { get; set; } = 10;

    public override void OnMouseDown(Point position)
    {
        base.OnMouseDown(position);

        _rectangle = new Rectangle
        {
            Fill = new SolidColorBrush(Color.FromArgb(200, 128, 128, 128)),
            Stroke = Brushes.Transparent,
            Effect = new BlurEffect { Radius = PixelSize }
        };

        WpfCanvas.SetLeft(_rectangle, position.X);
        WpfCanvas.SetTop(_rectangle, position.Y);
        CurrentElement = _rectangle;
    }

    public override void OnMouseMove(Point position)
    {
        if (!IsDrawing || _rectangle == null) return;

        double x = Math.Min(StartPoint.X, position.X);
        double y = Math.Min(StartPoint.Y, position.Y);
        double width = Math.Abs(position.X - StartPoint.X);
        double height = Math.Abs(position.Y - StartPoint.Y);

        WpfCanvas.SetLeft(_rectangle, x);
        WpfCanvas.SetTop(_rectangle, y);
        _rectangle.Width = width;
        _rectangle.Height = height;
    }
}
