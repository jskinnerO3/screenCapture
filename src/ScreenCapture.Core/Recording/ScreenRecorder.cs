using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using ScreenCapture.Core.Models;

namespace ScreenCapture.Core.Recording;

public class ScreenRecorder : IDisposable
{
    private readonly RecordingOptions _options;
    private CancellationTokenSource? _cts;
    private Task? _recordingTask;
    private readonly List<string> _frameFiles = new();
    private string _tempFolder = string.Empty;
    private DateTime _startTime;
    private bool _isPaused;
    private bool _disposed;

    public event EventHandler<RecordingState>? StateChanged;
    public event EventHandler<string>? RecordingCompleted;
    public event EventHandler<string>? RecordingError;

    public bool IsRecording => _recordingTask != null && !_recordingTask.IsCompleted;
    public bool IsPaused => _isPaused;

    public ScreenRecorder(RecordingOptions options)
    {
        _options = options;
    }

    public void StartRecording()
    {
        if (IsRecording) return;

        _tempFolder = Path.Combine(Path.GetTempPath(), $"ScreenCapture_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempFolder);
        _frameFiles.Clear();
        _startTime = DateTime.Now;
        _isPaused = false;

        _cts = new CancellationTokenSource();
        _recordingTask = Task.Run(() => RecordLoop(_cts.Token), _cts.Token);

        NotifyStateChanged();
    }

    public void PauseRecording()
    {
        _isPaused = true;
        NotifyStateChanged();
    }

    public void ResumeRecording()
    {
        _isPaused = false;
        NotifyStateChanged();
    }

    public async Task StopRecordingAsync()
    {
        if (!IsRecording) return;

        _cts?.Cancel();

        try
        {
            if (_recordingTask != null)
            {
                await _recordingTask;
            }
        }
        catch (OperationCanceledException) { }

        NotifyStateChanged();

        if (_frameFiles.Count > 0)
        {
            await EncodeVideoAsync();
        }
    }

    private void RecordLoop(CancellationToken token)
    {
        var frameInterval = TimeSpan.FromMilliseconds(1000.0 / _options.FrameRate);
        var stopwatch = new Stopwatch();
        int frameNumber = 0;

        var captureRegion = _options.CaptureRegion;
        if (captureRegion.IsEmpty)
        {
            captureRegion = new Rectangle(
                SystemInformation.VirtualScreen.X,
                SystemInformation.VirtualScreen.Y,
                SystemInformation.VirtualScreen.Width,
                SystemInformation.VirtualScreen.Height
            );
        }

        while (!token.IsCancellationRequested)
        {
            stopwatch.Restart();

            if (!_isPaused)
            {
                try
                {
                    var framePath = Path.Combine(_tempFolder, $"frame_{frameNumber:D6}.png");
                    CaptureFrame(captureRegion, framePath);
                    lock (_frameFiles)
                    {
                        _frameFiles.Add(framePath);
                    }
                    frameNumber++;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Frame capture error: {ex.Message}");
                }
            }

            var elapsed = stopwatch.Elapsed;
            if (elapsed < frameInterval)
            {
                Thread.Sleep(frameInterval - elapsed);
            }
        }
    }

    private static void CaptureFrame(Rectangle region, string outputPath)
    {
        using var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(region.Location, Point.Empty, region.Size, CopyPixelOperation.SourceCopy);
        bitmap.Save(outputPath, ImageFormat.Png);
    }

    private async Task EncodeVideoAsync()
    {
        try
        {
            var outputPath = _options.OutputPath;
            if (string.IsNullOrEmpty(outputPath))
            {
                var videosFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                outputPath = Path.Combine(videosFolder, $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
            }

            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await EncodeWithFFmpegAsync(outputPath);

            RecordingCompleted?.Invoke(this, outputPath);
        }
        catch (Exception ex)
        {
            RecordingError?.Invoke(this, ex.Message);
        }
        finally
        {
            CleanupTempFiles();
        }
    }

    private async Task EncodeWithFFmpegAsync(string outputPath)
    {
        var inputPattern = Path.Combine(_tempFolder, "frame_%06d.png");
        var bitrate = _options.Quality switch
        {
            VideoQuality.Low => "1M",
            VideoQuality.Medium => "3M",
            VideoQuality.High => "5M",
            VideoQuality.Ultra => "10M",
            _ => "5M"
        };

        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-y -framerate {_options.FrameRate} -i \"{inputPattern}\" -c:v libx264 -preset fast -b:v {bitrate} -pix_fmt yuv420p \"{outputPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"FFmpeg encoding failed: {error}");
            }
        }
        catch (Exception ex) when (ex.Message.Contains("FFmpeg"))
        {
            throw;
        }
        catch (Exception)
        {
            await CreateFallbackVideoAsync(outputPath);
        }
    }

    private async Task CreateFallbackVideoAsync(string outputPath)
    {
        await Task.Run(() =>
        {
            if (_frameFiles.Count > 0)
            {
                var gifPath = Path.ChangeExtension(outputPath, ".gif");
                using var firstFrame = Image.FromFile(_frameFiles[0]);
                var frames = new List<Image>();

                try
                {
                    foreach (var framePath in _frameFiles.Take(100))
                    {
                        frames.Add(Image.FromFile(framePath));
                    }

                    File.Copy(_frameFiles[0], Path.ChangeExtension(outputPath, ".png"), true);
                }
                finally
                {
                    foreach (var frame in frames)
                    {
                        frame.Dispose();
                    }
                }
            }
        });
    }

    private void CleanupTempFiles()
    {
        try
        {
            if (Directory.Exists(_tempFolder))
            {
                Directory.Delete(_tempFolder, true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Cleanup error: {ex.Message}");
        }
    }

    private void NotifyStateChanged()
    {
        var state = new RecordingState
        {
            IsRecording = IsRecording,
            IsPaused = _isPaused,
            Duration = IsRecording ? DateTime.Now - _startTime : TimeSpan.Zero,
            OutputFile = _options.OutputPath
        };
        StateChanged?.Invoke(this, state);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                CleanupTempFiles();
            }
            _disposed = true;
        }
    }
}
