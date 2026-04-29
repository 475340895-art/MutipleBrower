namespace FingerprintBrowser.Views;

/// <summary>
/// 启动画面窗口
/// </summary>
public partial class SplashWindow : HandyControl.Controls.Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public void UpdateStatus(string status)
    {
        LoadingText.Text = status;
    }
}
