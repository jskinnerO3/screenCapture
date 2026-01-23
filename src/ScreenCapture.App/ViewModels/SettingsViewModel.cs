using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Text.Json;

namespace ScreenCapture.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScreenCapture",
        "settings.json");

    [ObservableProperty]
    private string _savePath;

    [ObservableProperty]
    private string _imageFormat = "PNG";

    [ObservableProperty]
    private int _jpegQuality = 95;

    [ObservableProperty]
    private bool _copyToClipboardAfterCapture = true;

    [ObservableProperty]
    private bool _showPreviewAfterCapture = true;

    [ObservableProperty]
    private bool _playSound = true;

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private int _recordingFrameRate = 30;

    [ObservableProperty]
    private string _videoQuality = "High";

    [ObservableProperty]
    private bool _captureSystemAudio = true;

    [ObservableProperty]
    private bool _captureMicrophone;

    public string[] ImageFormats { get; } = { "PNG", "JPEG", "BMP", "GIF" };
    public string[] VideoQualities { get; } = { "Low", "Medium", "High", "Ultra" };
    public int[] FrameRates { get; } = { 15, 24, 30, 60 };

    public SettingsViewModel()
    {
        _savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
        Load();
    }

    [RelayCommand]
    private void BrowseSavePath()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            SelectedPath = SavePath,
            Description = "Select save location"
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            SavePath = dialog.SelectedPath;
        }
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var settings = new SettingsData
            {
                SavePath = SavePath,
                ImageFormat = ImageFormat,
                JpegQuality = JpegQuality,
                CopyToClipboardAfterCapture = CopyToClipboardAfterCapture,
                ShowPreviewAfterCapture = ShowPreviewAfterCapture,
                PlaySound = PlaySound,
                StartMinimized = StartMinimized,
                MinimizeToTray = MinimizeToTray,
                RecordingFrameRate = RecordingFrameRate,
                VideoQuality = VideoQuality,
                CaptureSystemAudio = CaptureSystemAudio,
                CaptureMicrophone = CaptureMicrophone
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<SettingsData>(json);

                if (settings != null)
                {
                    SavePath = settings.SavePath ?? SavePath;
                    ImageFormat = settings.ImageFormat ?? ImageFormat;
                    JpegQuality = settings.JpegQuality;
                    CopyToClipboardAfterCapture = settings.CopyToClipboardAfterCapture;
                    ShowPreviewAfterCapture = settings.ShowPreviewAfterCapture;
                    PlaySound = settings.PlaySound;
                    StartMinimized = settings.StartMinimized;
                    MinimizeToTray = settings.MinimizeToTray;
                    RecordingFrameRate = settings.RecordingFrameRate;
                    VideoQuality = settings.VideoQuality ?? VideoQuality;
                    CaptureSystemAudio = settings.CaptureSystemAudio;
                    CaptureMicrophone = settings.CaptureMicrophone;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
        ImageFormat = "PNG";
        JpegQuality = 95;
        CopyToClipboardAfterCapture = true;
        ShowPreviewAfterCapture = true;
        PlaySound = true;
        StartMinimized = false;
        MinimizeToTray = true;
        RecordingFrameRate = 30;
        VideoQuality = "High";
        CaptureSystemAudio = true;
        CaptureMicrophone = false;
    }
}

public class SettingsData
{
    public string? SavePath { get; set; }
    public string? ImageFormat { get; set; }
    public int JpegQuality { get; set; }
    public bool CopyToClipboardAfterCapture { get; set; }
    public bool ShowPreviewAfterCapture { get; set; }
    public bool PlaySound { get; set; }
    public bool StartMinimized { get; set; }
    public bool MinimizeToTray { get; set; }
    public int RecordingFrameRate { get; set; }
    public string? VideoQuality { get; set; }
    public bool CaptureSystemAudio { get; set; }
    public bool CaptureMicrophone { get; set; }
}
