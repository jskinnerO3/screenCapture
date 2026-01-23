using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenCapture.Editor.Tools;

public class ArrowTool : BaseTool
{
    public override string Name => "Arrow";

    private Line? _line;
    private Polygon? _arrowHead;

    public override void OnMouseDown(Point position)
    {
        base.OnMouseDown(position);

        _line = new Line
        {
            X1 = position.X,
            Y1 = position.Y,
            X2 = position.X,
            Y2 = position.Y,
            Stroke = new SolidColorBrush(CurrentColor),
            StrokeThickness = StrokeWidth,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };

        CurrentElement = _line;
    }

    public override void OnMouseMove(Point position)
    {
        if (!IsDrawing || _line == null) return;

        _line.X2 = position.X;
        _line.Y2 = position.Y;
    }

    public override void OnMouseUp(Point position)
    {
        if (_line != null)
        {
            AddArrowHead();
        }
        base.OnMouseUp(position);
    }

    private void AddArrowHead()
    {
        if (_line == null) return;

        double angle = Math.Atan2(_line.Y2 - _line.Y1, _line.X2 - _line.X1);
        double arrowLength = 15;
        double arrowAngle = Math.PI / 6;

        _arrowHead = new Polygon
        {
            Fill = new SolidColorBrush(CurrentColor),
            Points = new PointCollection
            {
                new Point(_line.X2, _line.Y2),
                new Point(
                    _line.X2 - arrowLength * Math.Cos(angle - arrowAngle),
                    _line.Y2 - arrowLength * Math.Sin(angle - arrowAngle)),
                new Point(
                    _line.X2 - arrowLength * Math.Cos(angle + arrowAngle),
                    _line.Y2 - arrowLength * Math.Sin(angle + arrowAngle))
            }
        };
    }

    public Polygon? GetArrowHead() => _arrowHead;
}
