using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DrawingRectangle = System.Drawing.Rectangle;
using WpfPoint = System.Windows.Point;

namespace ScreenCapture.App.Views;

public partial class CaptureOverlay : Window
{
    private WpfPoint _startPoint;
    private bool _isSelecting;
    private readonly Rectangle _clearRect;

    public event EventHandler<DrawingRectangle>? RegionSelected;
    public event EventHandler? Cancelled;

    public CaptureOverlay()
    {
        InitializeComponent();

        _clearRect = new Rectangle
        {
            Fill = Brushes.Transparent,
            Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
            StrokeThickness = 2
        };

        SelectionCanvas.Children.Add(_clearRect);
        _clearRect.Visibility = Visibility.Collapsed;

        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        KeyDown += OnKeyDown;

        Loaded += (s, e) =>
        {
            // Position window to span all monitors (virtual screen)
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
            Focus();
        };
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(SelectionCanvas);
        _isSelecting = true;
        _clearRect.Visibility = Visibility.Visible;
        SelectionRect.Visibility = Visibility.Visible;
        SizeIndicator.Visibility = Visibility.Visible;

        Canvas.SetLeft(_clearRect, _startPoint.X);
        Canvas.SetTop(_clearRect, _startPoint.Y);
        _clearRect.Width = 0;
        _clearRect.Height = 0;

        Mouse.Capture(this);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isSelecting) return;

        var currentPoint = e.GetPosition(SelectionCanvas);

        var x = Math.Min(_startPoint.X, currentPoint.X);
        var y = Math.Min(_startPoint.Y, currentPoint.Y);
        var width = Math.Abs(currentPoint.X - _startPoint.X);
        var height = Math.Abs(currentPoint.Y - _startPoint.Y);

        Canvas.SetLeft(_clearRect, x);
        Canvas.SetTop(_clearRect, y);
        _clearRect.Width = width;
        _clearRect.Height = height;

        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = width;
        SelectionRect.Height = height;

        SizeText.Text = $"{(int)width} x {(int)height}";
        Canvas.SetLeft(SizeIndicator, x);
        Canvas.SetTop(SizeIndicator, y + height + 5);

        UpdateDarkOverlay(x, y, width, height);
    }

    private void UpdateDarkOverlay(double x, double y, double width, double height)
    {
        var geometry = new CombinedGeometry(
            GeometryCombineMode.Exclude,
            new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)),
            new RectangleGeometry(new Rect(x, y, width, height))
        );

        DarkOverlay.Clip = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));
        DarkOverlay.OpacityMask = new DrawingBrush(
            new GeometryDrawing(Brushes.Black, null, geometry)
        );
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting) return;

        _isSelecting = false;
        Mouse.Capture(null);

        var currentPoint = e.GetPosition(SelectionCanvas);
        var x = (int)Math.Min(_startPoint.X, currentPoint.X);
        var y = (int)Math.Min(_startPoint.Y, currentPoint.Y);
        var width = (int)Math.Abs(currentPoint.X - _startPoint.X);
        var height = (int)Math.Abs(currentPoint.Y - _startPoint.Y);

        if (width > 5 && height > 5)
        {
            // Offset by virtual screen origin to get absolute screen coordinates
            var screenX = x + (int)SystemParameters.VirtualScreenLeft;
            var screenY = y + (int)SystemParameters.VirtualScreenTop;
            var region = new DrawingRectangle(screenX, screenY, width, height);
            Close();
            RegionSelected?.Invoke(this, region);
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }
}
