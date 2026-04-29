using System.Windows;
using System.Windows.Threading;

namespace FingerprintBrowser.Views;

public partial class SplashWindow : HandyControl.Controls.Window
{
    private readonly DispatcherTimer _timer;
    private int _progress;
    
    public SplashWindow()
    {
        InitializeComponent();
        
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }
    
    public void UpdateStatus(string status)
    {
        Dispatcher.Invoke(() => StatusText.Text = status);
    }
    
    public void SetProgress(int value)
    {
        Dispatcher.Invoke(() => ProgressBar.Value = value);
    }
    
    private void Timer_Tick(object? sender, EventArgs e)
    {
        _progress += 2;
        if (_progress > 100)
        {
            _timer.Stop();
            return;
        }
        ProgressBar.Value = _progress;
    }
    
    public void Complete()
    {
        _timer.Stop();
        ProgressBar.Value = 100;
        StatusText.Text = "启动完成";
    }
}
