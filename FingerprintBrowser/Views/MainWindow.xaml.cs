using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FingerprintBrowser.ViewModels;
using FingerprintBrowser.Models;
using FingerprintBrowser.Data;
using Microsoft.EntityFrameworkCore;

namespace FingerprintBrowser.Views;

public partial class MainWindow : HandyControl.Controls.Window
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
        await _viewModel.LoadDataAsync();
        GroupsList.ItemsSource = _viewModel.Groups;
        EnvGrid.ItemsSource = _viewModel.FilteredEnvironments;
        UpdateStats();
    }
    
    private void UpdateStats()
    {
        var running = _viewModel.Environments.Count(x => x.Status == BrowserStatus.Running);
        var total = _viewModel.Environments.Count;
        StatsText.Text = $" | {running} 运行中 / {total} 总数";
    }
    
    private void BtnAddGroup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new InputDialog("新建分组", "请输入分组名称：");
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
        {
            _viewModel.AddGroup(dialog.InputText);
        }
    }
    
    private void BtnAddEnv_Click(object sender, RoutedEventArgs e)
    {
        var editWindow = new EnvironmentEditWindow();
        if (editWindow.ShowDialog() == true)
        {
            _viewModel.RefreshEnvironments();
            UpdateStats();
        }
    }
    
    private void BtnBatchStart_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.BatchStart();
        EnvGrid.Items.Refresh();
        UpdateStats();
    }
    
    private void BtnBatchStop_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.BatchStop();
        EnvGrid.Items.Refresh();
        UpdateStats();
    }
    
    private void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON文件|*.json|所有文件|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.ImportEnvironments(dialog.FileName);
            EnvGrid.Items.Refresh();
            UpdateStats();
        }
    }
    
    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON文件|*.json",
            FileName = "environments.json"
        };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.ExportEnvironments(dialog.FileName);
        }
    }
    
    private void BtnProxy_Click(object sender, RoutedEventArgs e)
    {
        var proxyWindow = new ProxyWindow();
        proxyWindow.ShowDialog();
    }
    
    private void GroupsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GroupsList.SelectedItem is EnvironmentGroup group)
        {
            _viewModel.FilterByGroup(group);
            EnvGrid.Items.Refresh();
        }
    }
    
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.SearchText = SearchBox.Text;
        EnvGrid.Items.Refresh();
    }
    
    private async void StartEnv_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            await _viewModel.StartEnvironmentAsync(id);
            EnvGrid.Items.Refresh();
            UpdateStats();
        }
    }
    
    private async void StopEnv_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            await _viewModel.StopEnvironmentAsync(id);
            EnvGrid.Items.Refresh();
            UpdateStats();
        }
    }
    
    private void EditEnv_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            var env = _viewModel.Environments.FirstOrDefault(x => x.Id == id);
            if (env != null)
            {
                var editWindow = new EnvironmentEditWindow(env);
                if (editWindow.ShowDialog() == true)
                {
                    _viewModel.RefreshEnvironments();
                    EnvGrid.Items.Refresh();
                }
            }
        }
    }
    
    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeCombo.SelectedIndex == 0)
            HandyControl.Themes.ThemeManager.Current.ApplicationTheme = HandyControl.Themes.ApplicationTheme.Dark;
        else if (ThemeCombo.SelectedIndex == 1)
            HandyControl.Themes.ThemeManager.Current.ApplicationTheme = HandyControl.Themes.ApplicationTheme.Light;
        else
            HandyControl.Themes.ThemeManager.Current.ApplicationTheme = HandyControl.Themes.ApplicationTheme.Dark;
    }
}

public class InputDialog : HandyControl.Controls.Window
{
    private readonly System.Windows.Controls.TextBox _inputBox;
    public string InputText => _inputBox.Text;
    
    public InputDialog(string title, string prompt)
    {
        Title = title;
        Width = 400;
        Height = 180;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        
        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        var label = new System.Windows.Controls.TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 10) };
        Grid.SetRow(label, 0);
        grid.Children.Add(label);
        
        _inputBox = new System.Windows.Controls.TextBox { Height = 32 };
        Grid.SetRow(_inputBox, 1);
        grid.Children.Add(_inputBox);
        
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
        var okBtn = new System.Windows.Controls.Button { Content = "确定", Width = 80, Height = 32, Margin = new Thickness(0, 0, 10, 0) };
        okBtn.Click += (s, e) => { DialogResult = true; Close(); };
        var cancelBtn = new System.Windows.Controls.Button { Content = "取消", Width = 80, Height = 32 };
        cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        Grid.SetRow(btnPanel, 2);
        grid.Children.Add(btnPanel);
        
        Content = grid;
    }
}
