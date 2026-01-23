using ScreenCapture.App.ViewModels;
using System.Windows;

namespace ScreenCapture.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var vm = (SettingsViewModel)DataContext;
        vm.SaveCommand.Execute(null);
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
