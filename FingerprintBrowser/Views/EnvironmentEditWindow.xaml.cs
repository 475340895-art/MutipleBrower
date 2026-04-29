using System.Windows;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Views;

public partial class EnvironmentEditWindow : HandyControl.Controls.Window
{
    public BrowserEnvironment? Result { get; private set; }

    public EnvironmentEditWindow(BrowserEnvironment? env = null)
    {
        InitializeComponent();
        Result = env ?? new BrowserEnvironment();
        DataContext = Result;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Result.Name))
        {
            HandyControl.Controls.MessageBox.Show("请输入环境名称", "提示");
            return;
        }
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void RandomizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (Result != null)
        {
            var random = new Random();
            Result.UserAgent = GenerateRandomUserAgent();
            Result.Languages = "en-US,en;q=0.9";
            Result.WebGLVendor = new[] { "Intel Inc.", "NVIDIA Corporation", "AMD" }[random.Next(3)];
            Result.WebGLRenderer = new[] { "Intel Iris OpenGL Engine", "NVIDIA GeForce GTX 1060", "AMD Radeon Pro 5500M" }[random.Next(3)];
            DataContext = null;
            DataContext = Result;
        }
    }

    private string GenerateRandomUserAgent()
    {
        var agents = new[]
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.2 Safari/605.1.15"
        };
        return agents[new Random().Next(agents.Length)];
    }
}
