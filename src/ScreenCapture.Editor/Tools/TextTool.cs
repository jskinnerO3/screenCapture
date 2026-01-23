using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfCanvas = System.Windows.Controls.Canvas;

namespace ScreenCapture.Editor.Tools;

public class TextTool : BaseTool
{
    public override string Name => "Text";

    public event EventHandler<TextBox>? TextBoxCreated;

    private TextBox? _textBox;

    public override void OnMouseDown(Point position)
    {
        base.OnMouseDown(position);

        _textBox = new TextBox
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(CurrentColor),
            Foreground = new SolidColorBrush(CurrentColor),
            FontSize = 14 + StrokeWidth * 2,
            MinWidth = 100,
            AcceptsReturn = true
        };

        WpfCanvas.SetLeft(_textBox, position.X);
        WpfCanvas.SetTop(_textBox, position.Y);

        CurrentElement = _textBox;
        TextBoxCreated?.Invoke(this, _textBox);
    }

    public TextBlock? ConvertToTextBlock()
    {
        if (_textBox == null || string.IsNullOrWhiteSpace(_textBox.Text))
            return null;

        var textBlock = new TextBlock
        {
            Text = _textBox.Text,
            Foreground = new SolidColorBrush(CurrentColor),
            FontSize = _textBox.FontSize
        };

        WpfCanvas.SetLeft(textBlock, WpfCanvas.GetLeft(_textBox));
        WpfCanvas.SetTop(textBlock, WpfCanvas.GetTop(_textBox));

        return textBlock;
    }
}
