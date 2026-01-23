using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreenCapture.Core.Capture;
using ScreenCapture.Core.Models;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace ScreenCapture.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ScreenCaptureService _screenCaptureService;
    private readonly WindowCaptureService _windowCaptureService;
    private readonly RegionCaptureService _regionCaptureService;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private CaptureResult? _lastCapture;

    [ObservableProperty]
    private string _savePath;

    [ObservableProperty]
    private string? _lastSavedFilePath;

    public MainViewModel()
    {
        _screenCaptureService = new ScreenCaptureService();
        _windowCaptureService = new WindowCaptureService();
        _regionCaptureService = new RegionCaptureService();
        _savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");

        if (!Directory.Exists(_savePath))
        {
            Directory.CreateDirectory(_savePath);
        }
    }

    [RelayCommand]
    private void CaptureFullScreen()
    {
        StatusMessage = "Capturing full screen...";
        var result = _screenCaptureService.CaptureFullScreen();
        HandleCaptureResult(result);
    }

    [RelayCommand]
    private void CapturePrimaryMonitor()
    {
        StatusMessage = "Capturing primary monitor...";
        var result = _screenCaptureService.CapturePrimaryMonitor();
        HandleCaptureResult(result);
    }

    [RelayCommand]
    private void CaptureActiveWindow()
    {
        StatusMessage = "Capturing active window...";
        var result = _windowCaptureService.CaptureActiveWindow();
        HandleCaptureResult(result);
    }

    public void CaptureRegion(Rectangle region)
    {
        StatusMessage = "Capturing region...";
        var result = _regionCaptureService.CaptureRegion(region);
        HandleCaptureResult(result);
    }

    [RelayCommand]
    private void StartRegionCapture()
    {
        StatusMessage = "Select a region to capture...";
    }

    [RelayCommand]
    private void StartScrollingCapture()
    {
        StatusMessage = "Starting scrolling capture...";
    }

    [RelayCommand]
    private void ToggleRecording()
    {
        IsRecording = !IsRecording;
        StatusMessage = IsRecording ? "Recording..." : "Recording stopped";
    }

    [RelayCommand]
    private void OpenSettings()
    {
        StatusMessage = "Opening settings...";
    }

    [RelayCommand]
    private void Exit()
    {
        if (Application.Current.MainWindow is MainWindow mainWindow)
        {
            mainWindow.ExitApplication();
        }
        else
        {
            Application.Current.Shutdown();
        }
    }

    private void HandleCaptureResult(CaptureResult result)
    {
        if (result.IsSuccess && result.Image != null)
        {
            LastCapture = result;

            // Auto-save to Screenshots folder
            var savedPath = QuickSave();
            if (!string.IsNullOrEmpty(savedPath))
            {
                LastSavedFilePath = savedPath;
                StatusMessage = $"Captured and saved at {result.CapturedAt:HH:mm:ss}";
            }
            else
            {
                LastSavedFilePath = null;
                StatusMessage = $"Captured at {result.CapturedAt:HH:mm:ss}";
            }
        }
        else
        {
            StatusMessage = "Capture failed";
        }
    }

    public string QuickSave()
    {
        if (LastCapture?.Image == null)
        {
            return string.Empty;
        }

        var filename = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filepath = Path.Combine(SavePath, filename);
        LastCapture.Image.Save(filepath, ImageFormat.Png);
        StatusMessage = $"Saved to {filepath}";
        return filepath;
    }

    public void CopyToClipboard()
    {
        if (LastCapture?.Image == null) return;

        using var ms = new MemoryStream();
        LastCapture.Image.Save(ms, ImageFormat.Png);
        ms.Position = 0;
        var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = ms;
        bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        Clipboard.SetImage(bitmapImage);
        StatusMessage = "Copied to clipboard";
    }

    public List<WindowInfo> GetOpenWindows()
    {
        return _windowCaptureService.GetOpenWindows();
    }

    public void CaptureWindow(IntPtr hwnd)
    {
        StatusMessage = "Capturing window...";
        var result = _windowCaptureService.CaptureWindow(hwnd);
        HandleCaptureResult(result);
    }
}
