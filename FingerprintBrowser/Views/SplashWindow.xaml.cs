using System.Windows;
using HandyControl.Controls;

namespace FingerprintBrowser.Views
{
    /// <summary>
    /// 启动画面窗口
    /// </summary>
    public partial class SplashWindow : HandyControl.Controls.Window
    {
        private System.Windows.Controls.TextBlock? _loadingText;

        public SplashWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => _loadingText = FindName("LoadingText") as System.Windows.Controls.TextBlock;
        }

        public void UpdateStatus(string status)
        {
            if (_loadingText != null)
            {
                _loadingText.Text = status;
            }
        }
    }
}
