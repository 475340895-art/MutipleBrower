using System.Windows;
using FingerprintBrowser.Models;
using FingerprintBrowser.ViewModels;

namespace FingerprintBrowser.Views;

/// <summary>
/// 环境编辑窗口
/// </summary>
public partial class EnvironmentEditWindow : HandyControl.Controls.Window
{
    private readonly EnvironmentEditViewModel _viewModel;

    public EnvironmentEditWindow(BrowserEnvironment? environment = null)
    {
        InitializeComponent();

        _viewModel = new EnvironmentEditViewModel();
        DataContext = _viewModel;

        if (environment != null)
        {
            _viewModel.LoadEnvironment(environment);
            Title = $"编辑环境 - {environment.Name}";
        }
        else
        {
            _viewModel.NewEnvironment();
            Title = "新建环境";
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SaveCommand.ExecuteAsync(null);
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
