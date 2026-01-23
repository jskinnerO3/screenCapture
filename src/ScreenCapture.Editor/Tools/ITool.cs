using System.Windows;
using System.Windows.Media;

namespace ScreenCapture.Editor.Tools;

public interface ITool
{
    string Name { get; }
    void OnMouseDown(Point position);
    void OnMouseMove(Point position);
    void OnMouseUp(Point position);
    UIElement? GetCurrentElement();
    void SetColor(Color color);
    void SetStrokeWidth(double width);
}
