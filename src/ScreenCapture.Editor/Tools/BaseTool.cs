using System.Windows;
using System.Windows.Media;

namespace ScreenCapture.Editor.Tools;

public abstract class BaseTool : ITool
{
    public abstract string Name { get; }
    protected Color CurrentColor { get; private set; } = Colors.Red;
    protected double StrokeWidth { get; private set; } = 2;
    protected Point StartPoint { get; set; }
    protected bool IsDrawing { get; set; }
    protected UIElement? CurrentElement { get; set; }

    public virtual void OnMouseDown(Point position)
    {
        StartPoint = position;
        IsDrawing = true;
    }

    public virtual void OnMouseMove(Point position)
    {
    }

    public virtual void OnMouseUp(Point position)
    {
        IsDrawing = false;
    }

    public virtual UIElement? GetCurrentElement() => CurrentElement;

    public virtual void SetColor(Color color)
    {
        CurrentColor = color;
    }

    public virtual void SetStrokeWidth(double width)
    {
        StrokeWidth = width;
    }
}
