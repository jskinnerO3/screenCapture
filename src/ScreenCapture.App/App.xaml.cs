using System.Windows;

namespace ScreenCapture.App;

public partial class App : Application
{
    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string appName = "ScreenCapture_SingleInstance";
        _mutex = new Mutex(true, appName, out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show("Screen Capture is already running.", "Screen Capture",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
