using System.Windows;
using System.Windows.Input;
using FingerprintBrowser.ViewModels;
using Serilog;

namespace FingerprintBrowser.Views;

/// <summary>
/// 主窗口
/// </summary>
public partial class MainWindow : HandyControl.Controls.Window
{
    public bool IsMaximized { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        Log.Information("主窗口已初始化");
    }

    protected override void OnClosed(EventArgs e)
    {
        Log.Information("主窗口已关闭");
        base.OnClosed(e);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            WindowState = WindowState.Normal;
        else
            WindowState = WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// 双击环境列表项
    /// </summary>
    private void EnvironmentList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.SelectedEnvironment != null)
        {
            // 打开编辑窗口
            var editWindow = new EnvironmentEditWindow(vm.SelectedEnvironment);
            editWindow.Owner = this;
            editWindow.ShowDialog();

            // 刷新数据
            vm.LoadDataCommand.Execute(null);
        }
    }
}
