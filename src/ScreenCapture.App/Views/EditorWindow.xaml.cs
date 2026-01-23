using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfPoint = System.Windows.Point;
using WpfRectangle = System.Windows.Shapes.Rectangle;
using WpfEllipse = System.Windows.Shapes.Ellipse;

namespace ScreenCapture.App.Views;

public partial class EditorWindow : Window
{
    private Bitmap _currentImage;
    private string _currentTool = "Selection";
    private System.Windows.Media.Color _currentColor = Colors.Red;
    private double _strokeWidth = 2;
    private WpfPoint _startPoint;
    private bool _isDrawing;
    private UIElement? _currentShape;
    private int _stepNumber = 1;

    private readonly Stack<UIElement> _undoStack = new();
    private readonly Stack<UIElement> _redoStack = new();

    public EditorWindow(Bitmap image)
    {
        InitializeComponent();
        _currentImage = image;

        Loaded += EditorWindow_Loaded;
        KeyDown += EditorWindow_KeyDown;
    }

    private void EditorWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var bitmapImage = ConvertToBitmapImage(_currentImage);
        BackgroundImage.Source = bitmapImage;
        AnnotationCanvas.Width = bitmapImage.Width;
        AnnotationCanvas.Height = bitmapImage.Height;
        SizeText.Text = $"{bitmapImage.PixelWidth} x {bitmapImage.PixelHeight}";
    }

    private void EditorWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Key == Key.Z) UndoButton_Click(sender, e);
            if (e.Key == Key.Y) RedoButton_Click(sender, e);
            if (e.Key == Key.S) SaveButton_Click(sender, e);
            if (e.Key == Key.C) CopyButton_Click(sender, e);
        }
        else
        {
            switch (e.Key)
            {
                case Key.V: SelectionTool.IsChecked = true; break;
                case Key.A: ArrowTool.IsChecked = true; break;
                case Key.L: LineTool.IsChecked = true; break;
                case Key.R: RectangleTool.IsChecked = true; break;
                case Key.E: EllipseTool.IsChecked = true; break;
                case Key.H: HighlighterTool.IsChecked = true; break;
                case Key.T: TextTool.IsChecked = true; break;
                case Key.N: StepTool.IsChecked = true; break;
                case Key.B: BlurTool.IsChecked = true; break;
                case Key.C: CropTool.IsChecked = true; break;
            }
        }
    }

    private void Tool_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tool)
        {
            _currentTool = tool;
            StatusText.Text = $"Tool: {tool}";
        }
    }

    private void ColorPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ColorPicker.SelectedItem is ComboBoxItem item &&
            item.Content is WpfRectangle rect &&
            rect.Fill is SolidColorBrush brush)
        {
            _currentColor = brush.Color;
        }
    }

    private void StrokeWidth_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StrokeWidthPicker.SelectedItem is ComboBoxItem item && item.Tag is string width)
        {
            _strokeWidth = double.Parse(width);
        }
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(AnnotationCanvas);
        _isDrawing = true;

        switch (_currentTool)
        {
            case "Arrow":
            case "Line":
                _currentShape = CreateLine();
                break;
            case "Rectangle":
            case "Blur":
                _currentShape = CreateRectangle();
                break;
            case "Ellipse":
                _currentShape = CreateEllipse();
                break;
            case "Highlighter":
                _currentShape = CreateHighlighter();
                break;
            case "Text":
                CreateTextBox();
                _isDrawing = false;
                return;
            case "Step":
                CreateStepNumber();
                _isDrawing = false;
                return;
            case "Crop":
                _currentShape = CreateCropSelection();
                break;
        }

        if (_currentShape != null)
        {
            AnnotationCanvas.Children.Add(_currentShape);
        }

        Mouse.Capture(AnnotationCanvas);
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDrawing || _currentShape == null) return;

        var currentPoint = e.GetPosition(AnnotationCanvas);

        switch (_currentTool)
        {
            case "Arrow":
            case "Line":
                UpdateLine(currentPoint);
                break;
            case "Rectangle":
            case "Blur":
            case "Highlighter":
            case "Crop":
                UpdateRectangle(currentPoint);
                break;
            case "Ellipse":
                UpdateEllipse(currentPoint);
                break;
        }
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDrawing) return;

        _isDrawing = false;
        Mouse.Capture(null);

        if (_currentShape != null)
        {
            if (_currentTool == "Arrow")
            {
                AddArrowHead((Line)_currentShape);
            }
            else if (_currentTool == "Blur")
            {
                ApplyBlurEffect((WpfRectangle)_currentShape);
            }
            else if (_currentTool == "Crop")
            {
                ApplyCrop((WpfRectangle)_currentShape);
                _currentShape = null;
                return; // Don't add to undo stack - crop modifies the image directly
            }

            _undoStack.Push(_currentShape);
            _redoStack.Clear();
            _currentShape = null;
        }
    }

    private Line CreateLine()
    {
        return new Line
        {
            X1 = _startPoint.X,
            Y1 = _startPoint.Y,
            X2 = _startPoint.X,
            Y2 = _startPoint.Y,
            Stroke = new SolidColorBrush(_currentColor),
            StrokeThickness = _strokeWidth,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
    }

    private void UpdateLine(WpfPoint currentPoint)
    {
        if (_currentShape is Line line)
        {
            line.X2 = currentPoint.X;
            line.Y2 = currentPoint.Y;
        }
    }

    private void AddArrowHead(Line line)
    {
        double angle = Math.Atan2(line.Y2 - line.Y1, line.X2 - line.X1);
        double arrowLength = 15;
        double arrowAngle = Math.PI / 6;

        var arrowHead = new Polygon
        {
            Fill = new SolidColorBrush(_currentColor),
            Points = new PointCollection
            {
                new WpfPoint(line.X2, line.Y2),
                new WpfPoint(
                    line.X2 - arrowLength * Math.Cos(angle - arrowAngle),
                    line.Y2 - arrowLength * Math.Sin(angle - arrowAngle)),
                new WpfPoint(
                    line.X2 - arrowLength * Math.Cos(angle + arrowAngle),
                    line.Y2 - arrowLength * Math.Sin(angle + arrowAngle))
            }
        };

        AnnotationCanvas.Children.Add(arrowHead);
        _undoStack.Push(arrowHead);
    }

    private WpfRectangle CreateRectangle()
    {
        var rect = new WpfRectangle
        {
            Stroke = new SolidColorBrush(_currentColor),
            StrokeThickness = _strokeWidth,
            Fill = System.Windows.Media.Brushes.Transparent
        };
        Canvas.SetLeft(rect, _startPoint.X);
        Canvas.SetTop(rect, _startPoint.Y);
        return rect;
    }

    private void UpdateRectangle(WpfPoint currentPoint)
    {
        if (_currentShape is WpfRectangle rect)
        {
            double x = Math.Min(_startPoint.X, currentPoint.X);
            double y = Math.Min(_startPoint.Y, currentPoint.Y);
            double width = Math.Abs(currentPoint.X - _startPoint.X);
            double height = Math.Abs(currentPoint.Y - _startPoint.Y);

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            rect.Width = width;
            rect.Height = height;
        }
    }

    private WpfEllipse CreateEllipse()
    {
        var ellipse = new WpfEllipse
        {
            Stroke = new SolidColorBrush(_currentColor),
            StrokeThickness = _strokeWidth,
            Fill = System.Windows.Media.Brushes.Transparent
        };
        Canvas.SetLeft(ellipse, _startPoint.X);
        Canvas.SetTop(ellipse, _startPoint.Y);
        return ellipse;
    }

    private void UpdateEllipse(WpfPoint currentPoint)
    {
        if (_currentShape is WpfEllipse ellipse)
        {
            double x = Math.Min(_startPoint.X, currentPoint.X);
            double y = Math.Min(_startPoint.Y, currentPoint.Y);
            double width = Math.Abs(currentPoint.X - _startPoint.X);
            double height = Math.Abs(currentPoint.Y - _startPoint.Y);

            Canvas.SetLeft(ellipse, x);
            Canvas.SetTop(ellipse, y);
            ellipse.Width = width;
            ellipse.Height = height;
        }
    }

    private WpfRectangle CreateHighlighter()
    {
        var color = _currentColor;
        var rect = new WpfRectangle
        {
            Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, color.R, color.G, color.B)),
            Stroke = System.Windows.Media.Brushes.Transparent
        };
        Canvas.SetLeft(rect, _startPoint.X);
        Canvas.SetTop(rect, _startPoint.Y);
        return rect;
    }

    private void CreateTextBox()
    {
        var textBox = new TextBox
        {
            Background = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(_currentColor),
            Foreground = new SolidColorBrush(_currentColor),
            FontSize = 14 + _strokeWidth * 2,
            MinWidth = 100,
            AcceptsReturn = true
        };

        Canvas.SetLeft(textBox, _startPoint.X);
        Canvas.SetTop(textBox, _startPoint.Y);
        AnnotationCanvas.Children.Add(textBox);

        Dispatcher.BeginInvoke(new Action(() => textBox.Focus()), System.Windows.Threading.DispatcherPriority.Input);
        textBox.LostFocus += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                AnnotationCanvas.Children.Remove(textBox);
            }
            else
            {
                var textBlock = new TextBlock
                {
                    Text = textBox.Text,
                    Foreground = new SolidColorBrush(_currentColor),
                    FontSize = textBox.FontSize
                };
                Canvas.SetLeft(textBlock, Canvas.GetLeft(textBox));
                Canvas.SetTop(textBlock, Canvas.GetTop(textBox));
                AnnotationCanvas.Children.Remove(textBox);
                AnnotationCanvas.Children.Add(textBlock);
                _undoStack.Push(textBlock);
                _redoStack.Clear();
            }
        };
    }

    private void CreateStepNumber()
    {
        var border = new Border
        {
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(14),
            Background = new SolidColorBrush(_currentColor),
            Child = new TextBlock
            {
                Text = _stepNumber.ToString(),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        Canvas.SetLeft(border, _startPoint.X - 14);
        Canvas.SetTop(border, _startPoint.Y - 14);
        AnnotationCanvas.Children.Add(border);
        _undoStack.Push(border);
        _redoStack.Clear();
        _stepNumber++;
    }

    private WpfRectangle CreateCropSelection()
    {
        var rect = new WpfRectangle
        {
            Stroke = System.Windows.Media.Brushes.White,
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 4, 4 },
            Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(64, 0, 120, 215))
        };
        Canvas.SetLeft(rect, _startPoint.X);
        Canvas.SetTop(rect, _startPoint.Y);
        return rect;
    }

    private void ApplyCrop(WpfRectangle rect)
    {
        // Get the crop region coordinates
        int x = (int)Canvas.GetLeft(rect);
        int y = (int)Canvas.GetTop(rect);
        int width = (int)rect.Width;
        int height = (int)rect.Height;

        // Remove the selection rectangle
        AnnotationCanvas.Children.Remove(rect);

        if (width <= 0 || height <= 0) return;

        // Clamp to image bounds
        x = Math.Max(0, Math.Min(x, _currentImage.Width - 1));
        y = Math.Max(0, Math.Min(y, _currentImage.Height - 1));
        width = Math.Min(width, _currentImage.Width - x);
        height = Math.Min(height, _currentImage.Height - y);

        if (width <= 0 || height <= 0) return;

        // Create the cropped bitmap
        var croppedBitmap = new Bitmap(width, height);
        using (var g = Graphics.FromImage(croppedBitmap))
        {
            g.DrawImage(_currentImage,
                new System.Drawing.Rectangle(0, 0, width, height),
                new System.Drawing.Rectangle(x, y, width, height),
                GraphicsUnit.Pixel);
        }

        // Update the current image
        var oldImage = _currentImage;
        _currentImage = croppedBitmap;
        oldImage.Dispose();

        // Update the display
        var bitmapImage = ConvertToBitmapImage(_currentImage);
        BackgroundImage.Source = bitmapImage;
        AnnotationCanvas.Width = width;
        AnnotationCanvas.Height = height;
        SizeText.Text = $"{width} x {height}";

        // Clear all annotations since their positions are now invalid
        AnnotationCanvas.Children.Clear();
        _undoStack.Clear();
        _redoStack.Clear();

        StatusText.Text = "Image cropped";
    }

    private void ApplyBlurEffect(WpfRectangle rect)
    {
        // Get the region coordinates
        int x = (int)Canvas.GetLeft(rect);
        int y = (int)Canvas.GetTop(rect);
        int width = (int)rect.Width;
        int height = (int)rect.Height;

        if (width <= 0 || height <= 0) return;

        // Clamp to image bounds
        x = Math.Max(0, Math.Min(x, _currentImage.Width - 1));
        y = Math.Max(0, Math.Min(y, _currentImage.Height - 1));
        width = Math.Min(width, _currentImage.Width - x);
        height = Math.Min(height, _currentImage.Height - y);

        if (width <= 0 || height <= 0) return;

        // Create pixelated version of the region
        int pixelSize = 8; // Size of each pixelation block
        var pixelatedBitmap = new Bitmap(width, height);

        using (var g = Graphics.FromImage(pixelatedBitmap))
        {
            // Process in blocks
            for (int py = 0; py < height; py += pixelSize)
            {
                for (int px = 0; px < width; px += pixelSize)
                {
                    int blockWidth = Math.Min(pixelSize, width - px);
                    int blockHeight = Math.Min(pixelSize, height - py);

                    // Sample the center of the block from the original image
                    int sampleX = x + px + blockWidth / 2;
                    int sampleY = y + py + blockHeight / 2;
                    sampleX = Math.Min(sampleX, _currentImage.Width - 1);
                    sampleY = Math.Min(sampleY, _currentImage.Height - 1);

                    var pixelColor = _currentImage.GetPixel(sampleX, sampleY);

                    // Fill the entire block with the sampled color
                    using var brush = new System.Drawing.SolidBrush(pixelColor);
                    g.FillRectangle(brush, px, py, blockWidth, blockHeight);
                }
            }
        }

        // Convert to WPF ImageBrush
        var bitmapImage = ConvertToBitmapImage(pixelatedBitmap);
        rect.Fill = new ImageBrush(bitmapImage);
        rect.Stroke = System.Windows.Media.Brushes.Transparent;
        rect.Effect = null; // No blur effect needed - we have true pixelation

        pixelatedBitmap.Dispose();
    }

    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_undoStack.Count > 0)
        {
            var element = _undoStack.Pop();
            AnnotationCanvas.Children.Remove(element);
            _redoStack.Push(element);
        }
    }

    private void RedoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_redoStack.Count > 0)
        {
            var element = _redoStack.Pop();
            AnnotationCanvas.Children.Add(element);
            _undoStack.Push(element);
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp",
            DefaultExt = ".png",
            FileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (dialog.ShowDialog() == true)
        {
            SaveImage(dialog.FileName);
            StatusText.Text = $"Saved to {dialog.FileName}";
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        var bitmap = RenderToBitmap();
        Clipboard.SetImage(bitmap);
        StatusText.Text = "Copied to clipboard";
    }

    private void SaveImage(string path)
    {
        var bitmap = RenderToBitmap();
        var encoder = GetEncoder(path);
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = File.OpenWrite(path);
        encoder.Save(stream);
    }

    private BitmapEncoder GetEncoder(string path)
    {
        var ext = System.IO.Path.GetExtension(path).ToLower();
        return ext switch
        {
            ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 95 },
            ".bmp" => new BmpBitmapEncoder(),
            _ => new PngBitmapEncoder()
        };
    }

    private RenderTargetBitmap RenderToBitmap()
    {
        var container = new Grid();
        container.Children.Add(new System.Windows.Controls.Image { Source = BackgroundImage.Source });

        var annotationsCopy = new Canvas
        {
            Width = AnnotationCanvas.Width,
            Height = AnnotationCanvas.Height
        };

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var brush = new VisualBrush(AnnotationCanvas);
            context.DrawRectangle(brush, null, new Rect(0, 0, AnnotationCanvas.Width, AnnotationCanvas.Height));
        }

        var width = (int)AnnotationCanvas.Width;
        var height = (int)AnnotationCanvas.Height;
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

        var drawingVisual = new DrawingVisual();
        using (var context = drawingVisual.RenderOpen())
        {
            context.DrawImage(BackgroundImage.Source, new Rect(0, 0, width, height));
            var annotationBrush = new VisualBrush(AnnotationCanvas);
            context.DrawRectangle(annotationBrush, null, new Rect(0, 0, width, height));
        }

        bitmap.Render(drawingVisual);
        return bitmap;
    }

    private static BitmapImage ConvertToBitmapImage(Bitmap bitmap)
    {
        using var memory = new MemoryStream();
        bitmap.Save(memory, ImageFormat.Png);
        memory.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memory;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
    }
}
