using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FingerprintBrowser.Models;
using FingerprintBrowser.ViewModels;
using HandyControl.Controls;
using Window = HandyControl.Controls.Window;

namespace FingerprintBrowser.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
        }

        private async void CreateEnvironment_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.CreateEnvironmentCommand.ExecuteAsync(null);
        }

        private async void EditEnvironment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is BrowserEnvironment env)
            {
                await _viewModel.EditEnvironmentCommand.ExecuteAsync(env);
            }
        }

        private async void CopyEnvironment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is BrowserEnvironment env)
            {
                await _viewModel.CopyEnvironmentCommand.ExecuteAsync(env);
            }
        }

        private async void DeleteEnvironment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is BrowserEnvironment env)
            {
                await _viewModel.DeleteEnvironmentCommand.ExecuteAsync(env);
            }
        }

        private async void StartBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is BrowserEnvironment env)
            {
                await _viewModel.StartBrowserCommand.ExecuteAsync(env);
            }
        }

        private async void StopBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is BrowserEnvironment env)
            {
                await _viewModel.StopBrowserCommand.ExecuteAsync(env);
            }
        }

        private async void OpenBrowserUrl_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is BrowserEnvironment env)
            {
                await _viewModel.OpenBrowserUrlCommand.ExecuteAsync(env);
            }
        }

        private async void BatchStart_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.BatchStartCommand.ExecuteAsync(null);
        }

        private async void BatchStop_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.BatchStopCommand.ExecuteAsync(null);
        }

        private async void ImportEnvironments_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.ImportEnvironmentsCommand.ExecuteAsync(null);
        }

        private async void ExportEnvironments_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.ExportEnvironmentsCommand.ExecuteAsync(null);
        }

        private void OpenProxyManager_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.OpenProxyManagerCommand.Execute(null);
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.OpenSettingsCommand.Execute(null);
        }

        private async void CreateGroup_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.CreateGroupCommand.ExecuteAsync(null);
        }

        private async void DeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is EnvironmentGroup group)
            {
                await _viewModel.DeleteGroupCommand.ExecuteAsync(group);
            }
        }

        private async void Theme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Header != null)
            {
                var theme = menuItem.Header.ToString()?.Replace("主题: ", "").Trim();
                if (!string.IsNullOrEmpty(theme))
                {
                    await _viewModel.ChangeThemeAsync(theme);
                }
            }
        }

        private void EnvironmentList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (EnvironmentList?.SelectedItem is BrowserEnvironment env)
            {
                _viewModel.SelectedEnvironment = env;
            }
        }

        private async void GroupList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (GroupList?.SelectedItem is EnvironmentGroup group)
            {
                await _viewModel.SelectGroupCommand.ExecuteAsync(group);
            }
        }

        protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            await _viewModel.CleanupAsync();
            base.OnClosing(e);
        }
    }
}
