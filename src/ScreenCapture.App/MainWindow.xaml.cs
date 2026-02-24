using ScreenCapture.App.ViewModels;
using ScreenCapture.App.Views;
using ScreenCapture.Core.Capture;
using ScreenCapture.Core.Hotkeys;
using ScreenCapture.Core.Models;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ScreenCapture.App;

public partial class MainWindow : Window
{
    private GlobalHotkeyManager? _hotkeyManager;
    private RecordingControls? _recordingControls;
    private readonly ScrollingCaptureService _scrollingCaptureService;
    private bool _isActuallyClosing;
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        _scrollingCaptureService = new ScrollingCaptureService();
        Loaded += MainWindow_Loaded;
        StateChanged += MainWindow_StateChanged;
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        _hotkeyManager = new GlobalHotkeyManager(hwnd);
        RegisterDefaultHotkeys();
        UpdateTrayIcon();
    }

    private void RegisterDefaultHotkeys()
    {
        if (_hotkeyManager == null) return;

        _hotkeyManager.RegisterHotkey(
            GlobalHotkeyManager.KeyModifiers.None,
            GlobalHotkeyManager.VirtualKey.PrintScreen,
            () => Dispatcher.Invoke(() =>
            {
                ViewModel.CaptureFullScreenCommand.Execute(null);
                UpdatePreview();
            }));

        _hotkeyManager.RegisterHotkey(
            GlobalHotkeyManager.KeyModifiers.Alt,
            GlobalHotkeyManager.VirtualKey.PrintScreen,
            () => Dispatcher.Invoke(() =>
            {
                ViewModel.CaptureActiveWindowCommand.Execute(null);
                UpdatePreview();
            }));

        _hotkeyManager.RegisterHotkey(
            GlobalHotkeyManager.KeyModifiers.Control | GlobalHotkeyManager.KeyModifiers.Shift,
            GlobalHotkeyManager.VirtualKey.S,
            () => Dispatcher.Invoke(StartRegionCapture));

        _hotkeyManager.RegisterHotkey(
            GlobalHotkeyManager.KeyModifiers.Control | GlobalHotkeyManager.KeyModifiers.Shift,
            GlobalHotkeyManager.VirtualKey.R,
            () => Dispatcher.Invoke(() => ViewModel.ToggleRecordingCommand.Execute(null)));
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isActuallyClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _hotkeyManager?.Dispose();
        TrayIcon.Dispose();
    }

    public void ExitApplication()
    {
        _isActuallyClosing = true;
        Close();
        Application.Current.Shutdown();
    }

    private void UpdateTrayIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
            if (File.Exists(iconPath))
            {
                TrayIcon.Icon = new System.Drawing.Icon(iconPath);
            }
            else
            {
                TrayIcon.Icon = System.Drawing.SystemIcons.Application;
            }
        }
        catch
        {
            TrayIcon.Icon = System.Drawing.SystemIcons.Application;
        }
    }

    private void ShowWindow_Click(object sender, RoutedEventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void RegionCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        StartRegionCapture();
    }

    private void StartRegionCapture()
    {
        Hide();

        // Hide any open editor windows during region selection
        var editorWindows = Application.Current.Windows.OfType<EditorWindow>().ToList();
        foreach (var editorWindow in editorWindows)
        {
            editorWindow.Hide();
        }

        Thread.Sleep(200);

        var overlay = new CaptureOverlay();
        overlay.RegionSelected += (s, region) =>
        {
            ViewModel.CaptureRegion(region);
            if (ViewModel.LastCapture?.Image != null)
            {
                ViewModel.CopyToClipboard();
                // Close any previous editor windows since we're opening a new one
                foreach (var editorWindow in editorWindows)
                {
                    editorWindow.Close();
                }
                var editor = new EditorWindow(ViewModel.LastCapture.Image, ViewModel.LastSavedFilePath);
                editor.Show();
            }
            else
            {
                // Restore editor windows if capture failed
                foreach (var editorWindow in editorWindows)
                {
                    editorWindow.Show();
                }
                Show();
                Activate();
            }
        };
        overlay.Cancelled += (s, _) =>
        {
            // Restore editor windows on cancel
            foreach (var editorWindow in editorWindows)
            {
                editorWindow.Show();
            }
            Show();
            Activate();
        };
        overlay.Show();
    }

    private void WindowPickerButton_Click(object sender, RoutedEventArgs e)
    {
        var windows = ViewModel.GetOpenWindows();
        var picker = new WindowPicker(windows);
        if (picker.ShowDialog() == true && picker.SelectedWindow != null)
        {
            ViewModel.CaptureWindow(picker.SelectedWindow.Handle);
            UpdatePreview();
        }
    }

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (_recordingControls != null && _recordingControls.IsVisible)
        {
            return;
        }

        var options = new RecordingOptions
        {
            FrameRate = 30,
            Quality = VideoQuality.High,
            CaptureSystemAudio = true,
            CaptureMicrophone = false,
            Source = RecordingSource.FullScreen
        };

        _recordingControls = new RecordingControls(options);
        _recordingControls.RecordingStopped += (s, _) =>
        {
            Dispatcher.Invoke(() =>
            {
                ViewModel.IsRecording = false;
                _recordingControls = null;
            });
        };

        ViewModel.IsRecording = true;
        _recordingControls.Show();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow();
        settings.Owner = this;
        settings.ShowDialog();
    }

    private async void ScrollingCaptureButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        await Task.Delay(200);

        var overlay = new CaptureOverlay();
        overlay.RegionSelected += async (s, region) =>
        {
            ViewModel.StatusMessage = "Capturing scrolling content...";

            var result = await _scrollingCaptureService.CaptureScrollingRegionAsync(region);

            if (result.IsSuccess && result.Image != null)
            {
                ViewModel.LastCapture?.Dispose();
                typeof(MainViewModel).GetProperty("LastCapture")?.SetValue(ViewModel, result);
            }

            Show();
            Activate();
            UpdatePreview();
            ViewModel.StatusMessage = result.IsSuccess ? "Scrolling capture complete" : "Scrolling capture failed";
        };
        overlay.Cancelled += (s, _) =>
        {
            Show();
            Activate();
        };
        overlay.Show();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyToClipboard();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var path = ViewModel.QuickSave();
        if (!string.IsNullOrEmpty(path))
        {
            MessageBox.Show($"Saved to: {path}", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.LastCapture?.Image == null)
        {
            MessageBox.Show("No capture to edit", "Edit", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var editor = new EditorWindow(ViewModel.LastCapture.Image, ViewModel.LastSavedFilePath);
        editor.Show();
    }

    private void UpdatePreview()
    {
        if (ViewModel.LastCapture?.Image != null)
        {
            PreviewImage.Source = ConvertToBitmapImage(ViewModel.LastCapture.Image);
        }
    }

    private static BitmapImage ConvertToBitmapImage(Bitmap bitmap)
    {
        using var memory = new MemoryStream();
        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
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
