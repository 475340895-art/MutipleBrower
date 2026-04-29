using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using FingerprintBrowser.Data;
using FingerprintBrowser.Views;

namespace FingerprintBrowser;

public partial class App : System.Windows.Application
{
    private SplashWindow? _splash;
    
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // 显示启动画面
        _splash = new SplashWindow();
        _splash.Show();
        
        // 设置未处理异常处理
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        
        // 初始化数据库
        InitializeDatabase();
    }
    
    private void InitializeDatabase()
    {
        try
        {
            _splash?.UpdateStatus("正在初始化数据库...");
            _splash?.SetProgress(20);
            
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FingerprintBrowser",
                "data.db");
            
            var dbDir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }
            
            _splash?.UpdateStatus("正在创建数据库...");
            _splash?.SetProgress(40);
            
            DatabaseInitializer.Initialize(dbPath);
            
            _splash?.UpdateStatus("正在加载配置...");
            _splash?.SetProgress(70);
            
            // 关闭启动画面并显示主窗口
            _splash?.SetProgress(100);
            _splash?.Complete();
            
            _splash?.Close();
            
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            _splash?.Close();
            System.Windows.MessageBox.Show($"启动失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
    
    private void Application_Exit(object sender, ExitEventArgs e)
    {
        // 清理资源
        Services.BrowserService.Instance.Cleanup();
    }
    
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogError("未处理异常", e.Exception);
        System.Windows.MessageBox.Show($"发生错误: {e.Exception.Message}", "错误",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
    
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogError("未处理异常", ex);
        }
    }
    
    private static void LogError(string context, Exception ex)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FingerprintBrowser",
                "error.log");
            
            var logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            
            var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}: {ex}\n";
            File.AppendAllText(logPath, message);
        }
        catch { }
    }
}
