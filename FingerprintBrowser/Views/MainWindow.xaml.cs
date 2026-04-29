using System;
using System.Windows;
using System.Windows.Media;
using FingerprintBrowser.ViewModels;

namespace FingerprintBrowser.Views;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : HandyControl.Controls.Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Cleanup();
        base.OnClosed(e);
    }

    private void AddEnvironment_Click(object sender, RoutedEventArgs e)
    {
        var window = new EnvironmentEditWindow();
        window.Owner = this;
        if (window.ShowDialog() == true)
        {
            _ = _viewModel.LoadEnvironmentsAsync(true);
        }
    }

    private void EditEnvironment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is int id)
        {
            var window = new EnvironmentEditWindow(id);
            window.Owner = this;
            if (window.ShowDialog() == true)
            {
                _ = _viewModel.LoadEnvironmentsAsync(true);
            }
        }
    }

    private void CopyEnvironment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is int id)
        {
            _ = _viewModel.CopyEnvironmentAsync(id);
        }
    }

    private void DeleteEnvironment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is int id)
        {
            _ = _viewModel.DeleteEnvironmentAsync(id);
        }
    }

    private void StartBrowser_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is int id)
        {
            _ = _viewModel.StartBrowserAsync(id);
        }
    }

    private void StopBrowser_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is int id)
        {
            _ = _viewModel.StopBrowserAsync(id);
        }
    }

    private void OpenUrl_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is int id)
        {
            _viewModel.OpenBrowserUrl(id);
        }
    }

    private void BatchStart_Click(object sender, RoutedEventArgs e)
    {
        _ = _viewModel.BatchStartAsync();
    }

    private void BatchStop_Click(object sender, RoutedEventArgs e)
    {
        _ = _viewModel.BatchStopAsync();
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON文件|*.json|所有文件|*.*",
            Title = "导入环境"
        };

        if (dialog.ShowDialog() == true)
        {
            _ = _viewModel.ImportEnvironmentsAsync(dialog.FileName);
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON文件|*.json",
            Title = "导出环境",
            FileName = $"environments_{DateTime.Now:yyyyMMdd}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            _ = _viewModel.ExportEnvironmentsAsync(dialog.FileName);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow();
        window.Owner = this;
        window.ShowDialog();
    }

    private void ProxyManager_Click(object sender, RoutedEventArgs e)
    {
        var window = new ProxyWindow();
        window.Owner = this;
        window.ShowDialog();
    }

    private void ThemeLight_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ChangeTheme("浅色");
    }

    private void ThemeDark_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ChangeTheme("深色");
    }

    private void ThemeDarkBlue_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ChangeTheme("深蓝");
    }

    private void EnvironmentList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (EnvironmentList.SelectedItem is Models.BrowserEnvironment env)
        {
            _viewModel.ShowEnvironmentDetails(env);
        }
    }

    private void CloseWindow_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MaximizeWindow_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void GroupFilter_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.ComboBox combo && combo.SelectedItem is Models.EnvironmentGroup group)
        {
            _viewModel.SelectGroup(group);
        }
    }
}
