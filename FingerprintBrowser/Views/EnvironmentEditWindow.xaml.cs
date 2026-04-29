using System.Windows;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using Microsoft.EntityFrameworkCore;

namespace FingerprintBrowser.Views
{
    public partial class EnvironmentEditWindow : HandyControl.Controls.Window
    {
        private readonly int? _environmentId;
        private BrowserEnvironment? _environment;
        private readonly List<EnvironmentGroup> _groups;
        private readonly List<ProxyConfig> _proxies;

        public EnvironmentEditWindow(int? environmentId = null)
        {
            InitializeComponent();
            _environmentId = environmentId;

            using var db = new BrowserDbContext();
            _groups = db.Groups.ToList();
            _proxies = db.Proxies.ToList();

            GroupComboBox.ItemsSource = _groups;
            ProxyComboBox.ItemsSource = _proxies;

            if (environmentId.HasValue)
            {
                _environment = db.Environments
                    .Include(e => e.Group)
                    .Include(e => e.Proxy)
                    .FirstOrDefault(e => e.Id == environmentId.Value);

                if (_environment != null)
                {
                    Title = $"编辑环境 - {_environment.Name}";
                    LoadEnvironmentData();
                }
            }
            else
            {
                Title = "新建环境";
                _environment = new BrowserEnvironment();
            }
        }

        private void LoadEnvironmentData()
        {
            if (_environment == null) return;

            NameTextBox.Text = _environment.Name;
            StartupUrlTextBox.Text = _environment.StartupUrl;
            RemarkTextBox.Text = _environment.Remark;
            ResolutionComboBox.Text = _environment.Resolution;
            TimezoneTextBox.Text = _environment.Timezone;
            LanguagesTextBox.Text = _environment.Languages;
            UserAgentTextBox.Text = _environment.UserAgent;
            WebGLVendorTextBox.Text = _environment.WebGLVendor;
            WebGLRendererTextBox.Text = _environment.WebGLRenderer;

            WebRTCCheckBox.IsChecked = _environment.EnableWebRTC;
            CookiesCheckBox.IsChecked = _environment.EnableCookies;
            JavaScriptCheckBox.IsChecked = _environment.EnableJavaScript;

            var selectedGroup = _groups.FirstOrDefault(g => g.Id == _environment.GroupId);
            GroupComboBox.SelectedItem = selectedGroup;

            var selectedProxy = _proxies.FirstOrDefault(p => p.Id == _environment.ProxyId);
            ProxyComboBox.SelectedItem = selectedProxy;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                HandyControl.Controls.MessageBox.Show("请输入环境名称", "提示");
                return;
            }

            if (_environment == null) return;

            _environment.Name = NameTextBox.Text;
            _environment.StartupUrl = StartupUrlTextBox.Text ?? string.Empty;
            _environment.Remark = RemarkTextBox.Text ?? string.Empty;
            _environment.Resolution = ResolutionComboBox.Text ?? "1920x1080";
            _environment.Timezone = TimezoneTextBox.Text ?? "Asia/Shanghai";
            _environment.Languages = LanguagesTextBox.Text ?? "zh-CN,zh,en-US,en";
            _environment.UserAgent = UserAgentTextBox.Text ?? string.Empty;
            _environment.WebGLVendor = WebGLVendorTextBox.Text ?? string.Empty;
            _environment.WebGLRenderer = WebGLRendererTextBox.Text ?? string.Empty;
            _environment.EnableWebRTC = WebRTCCheckBox.IsChecked ?? true;
            _environment.EnableCookies = CookiesCheckBox.IsChecked ?? true;
            _environment.EnableJavaScript = JavaScriptCheckBox.IsChecked ?? true;

            if (GroupComboBox.SelectedItem is EnvironmentGroup group)
                _environment.GroupId = group.Id;

            if (ProxyComboBox.SelectedItem is ProxyConfig proxy)
                _environment.ProxyId = proxy.Id;

            using var db = new BrowserDbContext();

            if (_environmentId.HasValue)
            {
                db.Environments.Update(_environment);
            }
            else
            {
                _environment.CreatedAt = DateTime.Now;
                db.Environments.Add(_environment);
            }

            db.SaveChanges();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void GenerateUserAgent_Click(object sender, RoutedEventArgs e)
        {
            var agents = new[]
            {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
            };
            UserAgentTextBox.Text = agents[new Random().Next(agents.Length)];
        }
    }
}
