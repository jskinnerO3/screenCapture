using System.Drawing;

namespace ScreenCapture.Core.Models;

public class RecordingOptions
{
    public Rectangle CaptureRegion { get; set; }
    public bool CaptureSystemAudio { get; set; } = true;
    public bool CaptureMicrophone { get; set; } = false;
    public int FrameRate { get; set; } = 30;
    public VideoQuality Quality { get; set; } = VideoQuality.High;
    public string OutputPath { get; set; } = string.Empty;
    public IntPtr? WindowHandle { get; set; }
    public RecordingSource Source { get; set; } = RecordingSource.FullScreen;
}

public enum VideoQuality
{
    Low,
    Medium,
    High,
    Ultra
}

public enum RecordingSource
{
    FullScreen,
    Window,
    Region
}

public class RecordingState
{
    public bool IsRecording { get; set; }
    public bool IsPaused { get; set; }
    public TimeSpan Duration { get; set; }
    public string? OutputFile { get; set; }
}
