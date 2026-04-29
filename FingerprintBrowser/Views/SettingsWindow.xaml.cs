using System.Windows;

namespace FingerprintBrowser.Views;

public partial class SettingsWindow : HandyControl.Controls.Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
