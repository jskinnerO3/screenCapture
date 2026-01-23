using NAudio.Wave;
using System.Diagnostics;
using System.IO;

namespace ScreenCapture.Core.Recording;

public class AudioRecorder : IDisposable
{
    private WasapiLoopbackCapture? _loopbackCapture;
    private WaveInEvent? _microphoneCapture;
    private WaveFileWriter? _loopbackWriter;
    private WaveFileWriter? _microphoneWriter;
    private readonly string _tempFolder;
    private bool _isRecording;
    private bool _disposed;

    public string? SystemAudioFile { get; private set; }
    public string? MicrophoneFile { get; private set; }
    public bool CaptureSystemAudio { get; set; } = true;
    public bool CaptureMicrophone { get; set; } = false;

    public AudioRecorder()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), $"ScreenCapture_Audio_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempFolder);
    }

    public void StartRecording()
    {
        if (_isRecording) return;

        if (CaptureSystemAudio)
        {
            StartSystemAudioCapture();
        }

        if (CaptureMicrophone)
        {
            StartMicrophoneCapture();
        }

        _isRecording = true;
    }

    private void StartSystemAudioCapture()
    {
        try
        {
            SystemAudioFile = Path.Combine(_tempFolder, "system_audio.wav");
            _loopbackCapture = new WasapiLoopbackCapture();
            _loopbackWriter = new WaveFileWriter(SystemAudioFile, _loopbackCapture.WaveFormat);

            _loopbackCapture.DataAvailable += (s, e) =>
            {
                _loopbackWriter?.Write(e.Buffer, 0, e.BytesRecorded);
            };

            _loopbackCapture.RecordingStopped += (s, e) =>
            {
                _loopbackWriter?.Dispose();
                _loopbackWriter = null;
            };

            _loopbackCapture.StartRecording();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"System audio capture failed: {ex.Message}");
            SystemAudioFile = null;
        }
    }

    private void StartMicrophoneCapture()
    {
        try
        {
            if (WaveInEvent.DeviceCount == 0) return;

            MicrophoneFile = Path.Combine(_tempFolder, "microphone.wav");
            _microphoneCapture = new WaveInEvent
            {
                WaveFormat = new WaveFormat(44100, 16, 1)
            };
            _microphoneWriter = new WaveFileWriter(MicrophoneFile, _microphoneCapture.WaveFormat);

            _microphoneCapture.DataAvailable += (s, e) =>
            {
                _microphoneWriter?.Write(e.Buffer, 0, e.BytesRecorded);
            };

            _microphoneCapture.RecordingStopped += (s, e) =>
            {
                _microphoneWriter?.Dispose();
                _microphoneWriter = null;
            };

            _microphoneCapture.StartRecording();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Microphone capture failed: {ex.Message}");
            MicrophoneFile = null;
        }
    }

    public void StopRecording()
    {
        if (!_isRecording) return;

        _loopbackCapture?.StopRecording();
        _microphoneCapture?.StopRecording();

        _isRecording = false;
    }

    public void Cleanup()
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
            Debug.WriteLine($"Audio cleanup error: {ex.Message}");
        }
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
                StopRecording();
                _loopbackCapture?.Dispose();
                _microphoneCapture?.Dispose();
                _loopbackWriter?.Dispose();
                _microphoneWriter?.Dispose();
                Cleanup();
            }
            _disposed = true;
        }
    }
}
