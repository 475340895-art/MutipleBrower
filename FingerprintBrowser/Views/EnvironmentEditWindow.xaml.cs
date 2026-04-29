using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FingerprintBrowser.ViewModels;
using HandyControl.Controls;
using Window = HandyControl.Controls.Window;

namespace FingerprintBrowser.Views
{
    public partial class EnvironmentEditWindow : Window
    {
        private readonly EnvironmentEditViewModel _viewModel;

        public EnvironmentEditWindow(int? editingId = null)
        {
            InitializeComponent();
            _viewModel = new EnvironmentEditViewModel(editingId);
            DataContext = _viewModel;
            Loaded += async (s, e) => await _viewModel.LoadDataAsync();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.SaveCommand.ExecuteAsync(null);
            if (_viewModel.DialogResult)
            {
                DialogResult = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Randomize_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RandomizeFingerprintCommand.Execute(null);
        }
    }
}
