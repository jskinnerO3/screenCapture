using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ScreenCapture.Editor.Tools;

namespace ScreenCapture.Editor.Canvas;

public class AnnotationCanvas : System.Windows.Controls.Canvas
{
    private ITool? _currentTool;
    private bool _isDrawing;
    private readonly Stack<UIElement> _undoStack = new();
    private readonly Stack<UIElement> _redoStack = new();

    public event EventHandler<UIElement>? ElementAdded;
    public event EventHandler? UndoRedoStateChanged;

    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    public AnnotationCanvas()
    {
        Background = Brushes.Transparent;
        ClipToBounds = true;
    }

    public void SetTool(ITool tool)
    {
        _currentTool = tool;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (_currentTool == null) return;

        var position = e.GetPosition(this);
        _currentTool.OnMouseDown(position);
        _isDrawing = true;

        var element = _currentTool.GetCurrentElement();
        if (element != null)
        {
            Children.Add(element);
        }

        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_isDrawing || _currentTool == null) return;

        var position = e.GetPosition(this);
        _currentTool.OnMouseMove(position);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (!_isDrawing || _currentTool == null) return;

        var position = e.GetPosition(this);
        _currentTool.OnMouseUp(position);
        _isDrawing = false;

        ReleaseMouseCapture();

        var element = _currentTool.GetCurrentElement();
        if (element != null)
        {
            _undoStack.Push(element);
            _redoStack.Clear();
            ElementAdded?.Invoke(this, element);
            UndoRedoStateChanged?.Invoke(this, EventArgs.Empty);
        }

        if (_currentTool is ArrowTool arrowTool)
        {
            var arrowHead = arrowTool.GetArrowHead();
            if (arrowHead != null)
            {
                Children.Add(arrowHead);
                _undoStack.Push(arrowHead);
            }
        }
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;

        var element = _undoStack.Pop();
        Children.Remove(element);
        _redoStack.Push(element);
        UndoRedoStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;

        var element = _redoStack.Pop();
        Children.Add(element);
        _undoStack.Push(element);
        UndoRedoStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearAll()
    {
        foreach (var element in _undoStack)
        {
            Children.Remove(element);
        }
        _undoStack.Clear();
        _redoStack.Clear();
        UndoRedoStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddElement(UIElement element)
    {
        Children.Add(element);
        _undoStack.Push(element);
        _redoStack.Clear();
        UndoRedoStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
