using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;
using FingerprintBrowser.ViewModels;
using HandyControl.Controls;

namespace FingerprintBrowser.Views
{
    public partial class EnvironmentEditWindow
    {
        private readonly EnvironmentEditViewModel _viewModel;
        private readonly int? _environmentId;

        public EnvironmentEditWindow(int? environmentId = null)
        {
            InitializeComponent();
            _environmentId = environmentId;
            _viewModel = new EnvironmentEditViewModel();
            
            Loaded += async (s, e) => await InitializeAsync();
        }

        private async System.Threading.Tasks.Task InitializeAsync()
        {
            try
            {
                await _viewModel.LoadGroupsAsync();
                GroupComboBox.ItemsSource = _viewModel.Groups;
                GroupComboBox.DisplayMemberPath = "Name";

                if (_environmentId.HasValue)
                {
                    TitleText.Text = "编辑环境";
                    await _viewModel.LoadEnvironmentAsync(_environmentId.Value);
                    BindEnvironmentData();
                }
                else
                {
                    TitleText.Text = "新建环境";
                    // 默认选中第一个分组
                    if (_viewModel.Groups.Any())
                    {
                        GroupComboBox.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.Error($"加载失败: {ex.Message}");
            }
        }

        private void BindEnvironmentData()
        {
            var env = _viewModel.Environment;
            if (env == null) return;

            NameTextBox.Text = env.Name;
            RemarkTextBox.Text = env.Remark;
            StartupUrlTextBox.Text = env.StartupUrl;
            UserAgentTextBox.Text = env.UserAgent;

            // 分组
            var group = _viewModel.Groups.FirstOrDefault(g => g.Id == env.GroupId);
            if (group != null)
            {
                GroupComboBox.SelectedItem = group;
            }

            // 分辨率
            var resolution = $"{env.ScreenWidth} x {env.ScreenHeight}";
            foreach (ComboBoxItem item in ResolutionComboBox.Items)
            {
                if (item.Content?.ToString() == resolution)
                {
                    ResolutionComboBox.SelectedItem = item;
                    break;
                }
            }

            // 代理配置
            if (!string.IsNullOrEmpty(env.ProxyConfig))
            {
                var parts = env.ProxyConfig.Split(':');
                if (parts.Length >= 2)
                {
                    ProxyTypeComboBox.SelectedIndex = parts[0] switch
                    {
                        "http" => 1,
                        "https" => 2,
                        "socks5" => 3,
                        _ => 0
                    };
                    ProxyHostTextBox.Text = parts[1].TrimStart('/');
                    if (parts.Length >= 3)
                    {
                        ProxyPortTextBox.Text = parts[2].Split('@')[0];
                    }
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    Growl.Warning("请输入环境名称");
                    NameTextBox.Focus();
                    return;
                }

                // 构建环境对象
                var env = new BrowserEnvironment
                {
                    Name = NameTextBox.Text.Trim(),
                    Remark = RemarkTextBox.Text?.Trim(),
                    StartupUrl = StartupUrlTextBox.Text?.Trim(),
                    UserAgent = UserAgentTextBox.Text?.Trim(),
                    UpdatedAt = DateTime.Now
                };

                // 分组
                if (GroupComboBox.SelectedItem is EnvironmentGroup selectedGroup)
                {
                    env.GroupId = selectedGroup.Id;
                }

                // 分辨率
                if (ResolutionComboBox.SelectedItem is ComboBoxItem resItem)
                {
                    var parts = resItem.Content?.ToString()?.Split('x');
                    if (parts?.Length == 2)
                    {
                        env.ScreenWidth = int.Parse(parts[0].Trim());
                        env.ScreenHeight = int.Parse(parts[1].Trim());
                    }
                }

                // 代理配置
                if (ProxyTypeComboBox.SelectedIndex > 0)
                {
                    var proxyType = ProxyTypeComboBox.SelectedIndex switch
                    {
                        1 => "http",
                        2 => "https",
                        3 => "socks5",
                        _ => ""
                    };
                    var host = ProxyHostTextBox.Text?.Trim();
                    var port = ProxyPortTextBox.Text?.Trim();
                    if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(port))
                    {
                        env.ProxyConfig = $"{proxyType}://{host}:{port}";
                    }
                }

                if (_environmentId.HasValue)
                {
                    env.Id = _environmentId.Value;
                    _viewModel.UpdateEnvironment(env);
                    Growl.Success("环境更新成功");
                }
                else
                {
                    env.CreatedAt = DateTime.Now;
                    _viewModel.CreateEnvironment(env);
                    Growl.Success("环境创建成功");
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Growl.Error($"保存失败: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void TestProxy_Click(object sender, RoutedEventArgs e)
        {
            var proxyConfig = ProxyHostTextBox.Text?.Trim();
            var port = ProxyPortTextBox.Text?.Trim();

            if (string.IsNullOrEmpty(proxyConfig) || string.IsNullOrEmpty(port))
            {
                Growl.Warning("请输入代理地址和端口");
                return;
            }

            TestProxyButton.IsEnabled = false;
            TestProxyButton.Content = "测试中...";

            try
            {
                var proxyType = ProxyTypeComboBox.SelectedIndex switch
                {
                    1 => "http",
                    2 => "https",
                    3 => "socks5",
                    _ => "http"
                };
                
                var fullProxy = $"{proxyType}://{proxyConfig}:{port}";
                var result = await ProxyService.TestProxyAsync(fullProxy);
                
                if (result.Success)
                {
                    Growl.Success($"代理可用 (延迟: {result.Latency}ms)");
                }
                else
                {
                    Growl.Error($"代理不可用: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                Growl.Error($"测试失败: {ex.Message}");
            }
            finally
            {
                TestProxyButton.IsEnabled = true;
                TestProxyButton.Content = "测试代理";
            }
        }
    }
}
