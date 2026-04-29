using System.Windows;
using System.Windows.Controls;
using FingerprintBrowser.ViewModels;
using FingerprintBrowser.Models;
using FingerprintBrowser.Data;

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
        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"初始化失败: {ex.Message}");
        }
    }
    
    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 主题切换通过 HandyControl 的 SkinType
        var app = System.Windows.Application.Current;
        if (app?.Resources.MergedDictionaries.Count > 0)
        {
            var skinUri = ThemeCombo.SelectedIndex == 1 
                ? new Uri("pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml")
                : new Uri("pack://application:,,,/HandyControl;component/Themes/SkinDark.xaml");
            
            try
            {
                var skin = new ResourceDictionary { Source = skinUri };
                app.Resources.MergedDictionaries[0] = skin;
            }
            catch { }
        }
    }
    
    private async void NewEnv_Click(object sender, RoutedEventArgs e)
    {
        var win = new EnvironmentEditWindow();
        if (win.ShowDialog() == true)
        {
            await _viewModel.LoadDataAsync();
        }
    }
    
    private async void EditEnv_Click(object sender, RoutedEventArgs e)
    {
        if (EnvGrid.SelectedItem is BrowserEnvironment env)
        {
            var win = new EnvironmentEditWindow(env);
            if (win.ShowDialog() == true)
            {
                await _viewModel.LoadDataAsync();
            }
        }
    }
    
    private void Proxy_Click(object sender, RoutedEventArgs e)
    {
        var win = new ProxyWindow();
        win.ShowDialog();
    }
    
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow();
        win.ShowDialog();
    }
    
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel?.FilterEnvironments(SearchBox.Text);
    }
    
    private void GroupFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is EnvironmentGroup group)
        {
            _viewModel?.SelectGroup(group);
        }
    }
    
    private void EnvGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EnvGrid.SelectedItem is BrowserEnvironment env)
        {
            _viewModel.SelectedEnvironment = env;
        }
    }
    
    private void StartBrowser_Click(object sender, RoutedEventArgs e)
    {
        if (EnvGrid.SelectedItem is BrowserEnvironment env)
        {
            _ = _viewModel.StartBrowserAsync(env);
        }
    }
    
    private void StopBrowser_Click(object sender, RoutedEventArgs e)
    {
        if (EnvGrid.SelectedItem is BrowserEnvironment env)
        {
            _ = _viewModel.StopBrowserAsync(env);
        }
    }
    
    private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
    
    private void MaximizeBtn_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
    
    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Close();
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
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        var label = new System.Windows.Controls.TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 10) };
        Grid.SetRow(label, 0);
        
        _inputBox = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 0, 0, 10) };
        Grid.SetRow(_inputBox, 1);
        
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var okBtn = new System.Windows.Controls.Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
        okBtn.Click += (s, e) => { DialogResult = true; Close(); };
        var cancelBtn = new System.Windows.Controls.Button { Content = "取消", Width = 80 };
        cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        Grid.SetRow(btnPanel, 2);
        
        grid.Children.Add(label);
        grid.Children.Add(_inputBox);
        grid.Children.Add(btnPanel);
        Content = grid;
    }
}
