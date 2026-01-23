using ScreenCapture.Core.Models;
using ScreenCapture.Core.Recording;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ScreenCapture.App.Views;

public partial class RecordingControls : Window
{
    private readonly ScreenRecorder _recorder;
    private readonly AudioRecorder _audioRecorder;
    private readonly DispatcherTimer _timer;
    private DateTime _startTime;
    private TimeSpan _pausedDuration;
    private bool _isPaused;

    public event EventHandler? RecordingStopped;

    public RecordingControls(RecordingOptions options)
    {
        InitializeComponent();

        _recorder = new ScreenRecorder(options);
        _audioRecorder = new AudioRecorder
        {
            CaptureSystemAudio = options.CaptureSystemAudio,
            CaptureMicrophone = options.CaptureMicrophone
        };

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += Timer_Tick;

        _recorder.RecordingCompleted += (s, path) =>
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Recording saved to:\n{path}", "Recording Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });
        };

        _recorder.RecordingError += (s, error) =>
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Recording error: {error}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            });
        };

        Loaded += RecordingControls_Loaded;
        Closing += RecordingControls_Closing;

        Left = SystemParameters.PrimaryScreenWidth - Width - 20;
        Top = 20;
    }

    private void RecordingControls_Loaded(object sender, RoutedEventArgs e)
    {
        StartRecording();
    }

    private void RecordingControls_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _timer.Stop();
        _recorder.Dispose();
        _audioRecorder.Dispose();
    }

    private void StartRecording()
    {
        _startTime = DateTime.Now;
        _pausedDuration = TimeSpan.Zero;
        _isPaused = false;

        _recorder.StartRecording();
        _audioRecorder.StartRecording();
        _timer.Start();
    }

    private async void StopRecording()
    {
        _timer.Stop();
        _audioRecorder.StopRecording();
        await _recorder.StopRecordingAsync();
        RecordingStopped?.Invoke(this, EventArgs.Empty);
        Close();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (!_isPaused)
        {
            var elapsed = DateTime.Now - _startTime - _pausedDuration;
            DurationText.Text = elapsed.ToString(@"hh\:mm\:ss");
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        _isPaused = !_isPaused;

        if (_isPaused)
        {
            _recorder.PauseRecording();
            PauseIcon.Text = "▶";
            RecordingIndicator.Opacity = 0.3;
        }
        else
        {
            _recorder.ResumeRecording();
            PauseIcon.Text = "⏸";
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        StopRecording();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Stop recording and discard?", "Stop Recording",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _timer.Stop();
            _recorder.Dispose();
            _audioRecorder.Dispose();
            RecordingStopped?.Invoke(this, EventArgs.Empty);
            Close();
        }
    }
}
