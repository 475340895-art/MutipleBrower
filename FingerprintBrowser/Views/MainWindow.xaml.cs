using System.Windows;
using FingerprintBrowser.ViewModels;

namespace FingerprintBrowser.Views
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadDataCommand.ExecuteAsync(null);
        }

        private void SearchBar_SearchStarted(object sender, HandyControl.Data.FunctionEventArgs<string> e)
        {
            ViewModel.SearchText = e.Info ?? string.Empty;
        }
    }
}
