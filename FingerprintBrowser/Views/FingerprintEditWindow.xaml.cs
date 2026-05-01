using System.Windows;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Views
{
    public partial class FingerprintEditWindow : Window
    {
        private BrowserEnvironment _original;

        public FingerprintEditWindow(BrowserEnvironment env)
        {
            InitializeComponent();
            _original = env;
            FingerprintBox.Text = env.FingerprintHash ?? "";
            if (!string.IsNullOrEmpty(env.UserAgent)) UABox.Text = env.UserAgent;
            if (!string.IsNullOrEmpty(env.Resolution)) ResolutionBox.Text = env.Resolution;
            if (!string.IsNullOrEmpty(env.WebGLVendor)) WebGLVendorBox.Text = env.WebGLVendor;
            if (!string.IsNullOrEmpty(env.WebGLRenderer)) WebGLRendererBox.Text = env.WebGLRenderer;
            WebRTCCheck.IsChecked = env.EnableWebRTC;
            if (!string.IsNullOrEmpty(env.Languages)) LangBox.Text = env.Languages;
            if (!string.IsNullOrEmpty(env.Timezone)) TimezoneBox.Text = env.Timezone;
        }

        public BrowserEnvironment GetEnvironment()
        {
            return _original;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _original.UserAgent = UABox.Text;
            _original.Resolution = ResolutionBox.Text;
            _original.WebGLVendor = WebGLVendorBox.Text;
            _original.WebGLRenderer = WebGLRendererBox.Text;
            _original.EnableWebRTC = WebRTCCheck.IsChecked == true;
            _original.Languages = LangBox.Text;
            _original.Timezone = TimezoneBox.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
