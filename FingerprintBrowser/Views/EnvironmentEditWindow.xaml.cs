using System.Windows;
using FingerprintBrowser.ViewModels;

namespace FingerprintBrowser.Views;

public partial class EnvironmentEditWindow : HandyControl.Controls.Window
{
    private readonly EnvironmentEditViewModel _viewModel;

    public EnvironmentEditWindow(Models.BrowserEnvironment? environment = null)
    {
        InitializeComponent();
        _viewModel = new EnvironmentEditViewModel(environment);
        DataContext = _viewModel;
        Loaded += (s, e) => NameTextBox.Focus();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SaveAsync();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void GenerateUserAgent_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.GenerateUserAgent();
    }

    private void GenerateFingerprint_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.GenerateFingerprint();
    }
}
