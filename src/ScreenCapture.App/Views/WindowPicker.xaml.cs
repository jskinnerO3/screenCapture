using ScreenCapture.Core.Capture;
using System.Windows;

namespace ScreenCapture.App.Views;

public partial class WindowPicker : Window
{
    public WindowInfo? SelectedWindow { get; private set; }

    public WindowPicker(List<WindowInfo> windows)
    {
        InitializeComponent();
        WindowList.ItemsSource = windows;
        if (windows.Count > 0)
        {
            WindowList.SelectedIndex = 0;
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedWindow = WindowList.SelectedItem as WindowInfo;
        if (SelectedWindow != null)
        {
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
