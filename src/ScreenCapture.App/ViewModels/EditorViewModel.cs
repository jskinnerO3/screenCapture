using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Drawing;
using System.Windows.Media;

namespace ScreenCapture.App.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _currentTool = "Selection";

    [ObservableProperty]
    private System.Windows.Media.Color _currentColor = Colors.Red;

    [ObservableProperty]
    private double _strokeWidth = 2;

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private int _zoomLevel = 100;

    private readonly Bitmap _originalImage;
    private int _stepNumber = 1;

    public EditorViewModel(Bitmap image)
    {
        _originalImage = image;
    }

    [RelayCommand]
    private void SelectTool(string tool)
    {
        CurrentTool = tool;
        StatusMessage = $"Tool: {tool}";
    }

    [RelayCommand]
    private void SetColor(System.Windows.Media.Color color)
    {
        CurrentColor = color;
    }

    [RelayCommand]
    private void SetStrokeWidth(double width)
    {
        StrokeWidth = width;
    }

    [RelayCommand]
    private void ZoomIn()
    {
        if (ZoomLevel < 400)
        {
            ZoomLevel += 25;
        }
    }

    [RelayCommand]
    private void ZoomOut()
    {
        if (ZoomLevel > 25)
        {
            ZoomLevel -= 25;
        }
    }

    [RelayCommand]
    private void ResetZoom()
    {
        ZoomLevel = 100;
    }

    public int GetNextStepNumber()
    {
        return _stepNumber++;
    }

    public void ResetStepNumbers()
    {
        _stepNumber = 1;
    }

    public void UpdateUndoRedoState(int undoCount, int redoCount)
    {
        CanUndo = undoCount > 0;
        CanRedo = redoCount > 0;
    }
}
