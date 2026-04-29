using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Win32;
using FingerprintBrowser.Models;
using FingerprintBrowser.ViewModels;
using FingerprintBrowser.Services;
using HandyControl.Controls;
using MessageBox = HandyControl.Controls.MessageBox;
using Dialog = HandyControl.Controls.Dialog;

namespace FingerprintBrowser.Views
{
    public partial class MainWindow
    {
        private readonly MainViewModel _viewModel;
        private DispatcherTimer _statusTimer;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            
            SetupStatusTimer();
            Loaded += MainWindow_Loaded;
        }

        private void SetupStatusTimer()
        {
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _statusTimer.Tick += (s, e) => UpdateStatusDisplay();
            _statusTimer.Start();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.InitializeAsync();
                UpdateStatusDisplay();
                StatusText.Text = "加载完成";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"加载失败: {ex.Message}";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 已在 MainWindow_Loaded 中处理
        }

        private void UpdateStatusDisplay()
        {
            var running = _viewModel.Environments.Count(e => e.Status == "Running");
            RunningCountText.Text = $"运行中: {running}";
            TotalCountText.Text = $"总计: {_viewModel.Environments.Count}";
            EnvCountText.Text = $" ({_viewModel.Environments.Count})";
        }

        #region 环境管理

        private void NewEnvironment_Click(object sender, RoutedEventArgs e)
        {
            var window = new EnvironmentEditWindow();
            window.Owner = this;
            if (window.ShowDialog() == true)
            {
                _ = _viewModel.LoadEnvironmentsAsync();
            }
        }

        private void EditEnvironment_Click(object sender, RoutedEventArgs e)
        {
            var env = EnvironmentGrid.SelectedItem as BrowserEnvironment;
            if (env == null)
            {
                Growl.Warning("请先选择一个环境");
                return;
            }

            var window = new EnvironmentEditWindow(env.Id);
            window.Owner = this;
            if (window.ShowDialog() == true)
            {
                _ = _viewModel.LoadEnvironmentsAsync();
            }
        }

        private async void StartEnvironment_Click(object sender, RoutedEventArgs e)
        {
            var env = EnvironmentGrid.SelectedItem as BrowserEnvironment;
            if (env == null)
            {
                Growl.Warning("请先选择一个环境");
                return;
            }

            await StartEnvironmentAsync(env);
        }

        private async Task StartEnvironmentAsync(BrowserEnvironment env)
        {
            try
            {
                ShowLoading("正在启动浏览器...");
                await _viewModel.StartBrowserAsync(env.Id);
                Growl.Success($"环境 '{env.Name}' 启动成功");
            }
            catch (Exception ex)
            {
                Growl.Error($"启动失败: {ex.Message}");
            }
            finally
            {
                HideLoading();
                await _viewModel.LoadEnvironmentsAsync();
            }
        }

        private async void StopEnvironment_Click(object sender, RoutedEventArgs e)
        {
            var env = EnvironmentGrid.SelectedItem as BrowserEnvironment;
            if (env == null)
            {
                Growl.Warning("请先选择一个环境");
                return;
            }

            await StopEnvironmentAsync(env);
        }

        private async Task StopEnvironmentAsync(BrowserEnvironment env)
        {
            try
            {
                await _viewModel.StopBrowserAsync(env.Id);
                Growl.Success($"环境 '{env.Name}' 已停止");
                await _viewModel.LoadEnvironmentsAsync();
            }
            catch (Exception ex)
            {
                Growl.Error($"停止失败: {ex.Message}");
            }
        }

        private async void CopyEnvironment_Click(object sender, RoutedEventArgs e)
        {
            var env = EnvironmentGrid.SelectedItem as BrowserEnvironment;
            if (env == null)
            {
                Growl.Warning("请先选择一个环境");
                return;
            }

            try
            {
                await _viewModel.CopyEnvironmentAsync(env.Id);
                Growl.Success("环境复制成功");
                await _viewModel.LoadEnvironmentsAsync();
            }
            catch (Exception ex)
            {
                Growl.Error($"复制失败: {ex.Message}");
            }
        }

        private async void DeleteEnvironment_Click(object sender, RoutedEventArgs e)
        {
            var env = EnvironmentGrid.SelectedItem as BrowserEnvironment;
            if (env == null)
            {
                Growl.Warning("请先选择一个环境");
                return;
            }

            var result = HandyControl.Controls.MessageBox.Show(
                $"确定要删除环境 '{env.Name}' 吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _viewModel.DeleteEnvironmentAsync(env.Id);
                    Growl.Success("删除成功");
                    await _viewModel.LoadEnvironmentsAsync();
                }
                catch (Exception ex)
                {
                    Growl.Error($"删除失败: {ex.Message}");
                }
            }
        }

        private async void BatchStart_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.BatchStartAsync();
            Growl.Success("批量启动完成");
            await _viewModel.LoadEnvironmentsAsync();
        }

        private async void BatchStop_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.BatchStopAsync();
            Growl.Success("批量停止完成");
            await _viewModel.LoadEnvironmentsAsync();
        }

        private void EnvironmentGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var env = EnvironmentGrid.SelectedItem as BrowserEnvironment;
            if (env != null && env.Status != "Running")
            {
                _ = StartEnvironmentAsync(env);
            }
            else if (env?.Status == "Running")
            {
                // 打开已运行的浏览器
                _viewModel.OpenBrowserUrl(env.Id);
            }
        }

        #endregion

        #region 分组管理

        private void GroupSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.FilterGroups(GroupSearchBox.Text);
        }

        private void GroupListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GroupListBox.SelectedItem is EnvironmentGroup group)
            {
                _viewModel.SelectGroup(group);
            }
        }

        #endregion

        #region 环境搜索

        private void EnvSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.FilterEnvironments(EnvSearchBox.Text);
        }

        #endregion

        #region 右侧详情面板

        public void UpdateDetailPanel(BrowserEnvironment env)
        {
            if (env == null)
            {
                DetailPanel.Visibility = Visibility.Collapsed;
                EmptyDetailPanel.Visibility = Visibility.Visible;
                return;
            }

            DetailPanel.Visibility = Visibility.Visible;
            EmptyDetailPanel.Visibility = Visibility.Collapsed;

            DetailName.Text = env.Name;
            DetailGroup.Text = env.GroupName ?? "默认分组";
            DetailStatus.Text = GetStatusText(env.Status);
            DetailProxy.Text = string.IsNullOrEmpty(env.ProxyConfig) ? "无代理" : env.ProxyConfig;
            DetailUrl.Text = string.IsNullOrEmpty(env.StartupUrl) ? "未设置" : env.StartupUrl;
            DetailCreated.Text = env.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            DetailUA.Text = string.IsNullOrEmpty(env.UserAgent) ? "-" : env.UserAgent;
            DetailResolution.Text = $"{env.ScreenWidth}x{env.ScreenHeight}";
            DetailTimezone.Text = string.IsNullOrEmpty(env.Timezone) ? "-" : env.Timezone;
            DetailLanguage.Text = string.IsNullOrEmpty(env.Languages) ? "-" : env.Languages;

            // 状态颜色
            DetailStatusDot.Fill = GetStatusBrush(env.Status);
        }

        private string GetStatusText(string status)
        {
            return status switch
            {
                "Running" => "运行中",
                "Starting" => "启动中",
                "Stopping" => "停止中",
                "Error" => "错误",
                _ => "空闲"
            };
        }

        private SolidColorBrush GetStatusBrush(string status)
        {
            return status switch
            {
                "Running" => new SolidColorBrush(Color.FromRgb(0, 255, 136)),
                "Starting" or "Stopping" => new SolidColorBrush(Color.FromRgb(255, 170, 0)),
                "Error" => new SolidColorBrush(Color.FromRgb(255, 68, 102)),
                _ => new SolidColorBrush(Color.FromRgb(102, 102, 136))
            };
        }

        #endregion

        #region 其他功能

        private void ProxyManage_Click(object sender, RoutedEventArgs e)
        {
            var window = new ProxyWindow();
            window.Owner = this;
            window.ShowDialog();
        }

        private void ImportExport_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            
            var importItem = new MenuItem { Header = "导入配置" };
            importItem.Click += async (s, args) =>
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    Title = "导入环境配置"
                };
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        await _viewModel.ImportEnvironmentsAsync(dialog.FileName);
                        Growl.Success("导入成功");
                        await _viewModel.LoadEnvironmentsAsync();
                    }
                    catch (Exception ex)
                    {
                        Growl.Error($"导入失败: {ex.Message}");
                    }
                }
            };
            
            var exportItem = new MenuItem { Header = "导出配置" };
            exportItem.Click += async (s, args) =>
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON 文件 (*.json)|*.json",
                    Title = "导出环境配置",
                    FileName = $"environments_{DateTime.Now:yyyyMMdd}"
                };
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        await _viewModel.ExportEnvironmentsAsync(dialog.FileName);
                        Growl.Success("导出成功");
                    }
                    catch (Exception ex)
                    {
                        Growl.Error($"导出失败: {ex.Message}");
                    }
                }
            };

            menu.Items.Add(importItem);
            menu.Items.Add(exportItem);
            menu.IsOpen = true;
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var window = new SettingsWindow();
            window.Owner = this;
            window.ShowDialog();
        }

        #endregion

        #region 主题切换

        private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeCombo.SelectedItem is ComboBoxItem item)
            {
                var theme = item.Tag?.ToString() ?? "Dark";
                ApplyTheme(theme);
            }
        }

        private void ApplyTheme(string theme)
        {
            var app = Application.Current;
            var resources = app.Resources.MergedDictionaries;

            // 移除现有主题
            var toRemove = resources.Where(d => 
                d.Source?.ToString().Contains("HandyControl") == true &&
                d.Source.ToString().Contains("Skin")).ToList();
            
            foreach (var dict in toRemove)
            {
                resources.Remove(dict);
            }

            // 添加新主题
            string skinPath = theme switch
            {
                "Light" => "pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml",
                "DeepBlue" => "pack://application:,,,/HandyControl;component/Themes/SkinDeepBlue.xaml",
                _ => "pack://application:,,,/HandyControl;component/Themes/SkinDark.xaml"
            };

            resources.Insert(0, new ResourceDictionary { Source = new Uri(skinPath) });
        }

        #endregion

        #region 加载状态

        public void ShowLoading(string message = "加载中...")
        {
            LoadingText.Text = message;
            LoadingOverlay.Visibility = Visibility.Visible;
        }

        public void HideLoading()
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}
