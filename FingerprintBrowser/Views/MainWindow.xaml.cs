using System.Windows;
using FingerprintBrowser.ViewModels;

namespace FingerprintBrowser.Views;

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

    private async void NewEnvironment_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.CreateEnvironmentAsync();
    }

    private async void EditEnvironment_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedEnvironment != null)
            await _viewModel.EditEnvironmentAsync(_viewModel.SelectedEnvironment);
    }

    private async void DeleteEnvironment_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedEnvironment != null)
            await _viewModel.DeleteEnvironmentAsync(_viewModel.SelectedEnvironment);
    }

    private async void CopyEnvironment_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedEnvironment != null)
            await _viewModel.CopyEnvironmentAsync(_viewModel.SelectedEnvironment);
    }

    private async void StartBrowser_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedEnvironment != null)
            await _viewModel.StartBrowserAsync(_viewModel.SelectedEnvironment);
    }

    private async void StopBrowser_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedEnvironment != null)
            await _viewModel.StopBrowserAsync(_viewModel.SelectedEnvironment);
    }

    private async void BatchStart_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.BatchStartAsync();
    }

    private async void BatchStop_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.BatchStopAsync();
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.ImportEnvironmentsAsync();
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.ExportEnvironmentsAsync();
    }

    private void OpenProxyWindow_Click(object sender, RoutedEventArgs e)
    {
        var win = new ProxyWindow();
        win.ShowDialog();
    }

    private void OpenSettingsWindow_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow();
        win.ShowDialog();
    }

    private void ThemeDark_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ChangeTheme("Dark");
    }

    private void ThemeLight_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ChangeTheme("Light");
    }

    private void ThemeBlue_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ChangeTheme("Blue");
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _viewModel.SearchText = (sender as System.Windows.Controls.TextBox)?.Text ?? "";
        _viewModel.FilterEnvironments();
    }

    private void GroupList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _viewModel.SelectGroup(GroupList.SelectedItem as Models.EnvironmentGroup);
    }
}
