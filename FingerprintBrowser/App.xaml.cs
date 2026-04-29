using System.Windows;
using FingerprintBrowser.Data;
using FingerprintBrowser.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FingerprintBrowser;

/// <summary>
/// 应用程序入口类
/// </summary>
public partial class App : Application
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FingerprintBrowser", "logs", "app-.log");

    public App()
    {
        // 配置 Serilog 日志
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(LogPath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        // 全局异常处理
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            Log.Fatal(args.ExceptionObject as Exception, "未处理的异常");
            Log.CloseAndFlush();
        };

        DispatcherUnhandledException += (sender, args) =>
        {
            Log.Error(args.Exception, "UI 线程异常");
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Log.Error(args.Exception, "未观察的任务异常");
            args.SetObserved();
        };
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Log.Information("=== 应用程序启动 ===");

        try
        {
            // 初始化数据库
            InitializeDatabase();

            // 初始化服务
            ServiceLocator.Initialize();

            // 初始化 Playwright
            _ = PlaywrightService.Instance.EnsureInstalledAsync();

            Log.Information("应用程序初始化完成");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "应用程序启动失败");
            MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("=== 应用程序退出 ===");
        ServiceLocator.Cleanup();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void InitializeDatabase()
    {
        Log.Information("初始化数据库...");

        using var context = new BrowserDbContext();
        context.Database.EnsureCreated();

        Log.Information("数据库初始化完成");
    }
}

/// <summary>
/// 服务定位器 - 简单的依赖注入容器
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> Services = new();

    public static void Initialize()
    {
        // 注册服务
        Register<IDialogService>(new DialogService());
        Register<IBrowserService>(new BrowserService());
        Register<IProxyService>(new ProxyService());
        Register<IImportExportService>(new ImportExportService());

        Log.Information("服务定位器初始化完成");
    }

    public static void Register<T>(T service) where T : class
    {
        Services[typeof(T)] = service;
    }

    public static T Get<T>() where T : class
    {
        return Services.TryGetValue(typeof(T), out var service)
            ? (T)service
            : throw new InvalidOperationException($"服务 {typeof(T).Name} 未注册");
    }

    public static void Cleanup()
    {
        // 清理服务
        if (Services.TryGetValue(typeof(IBrowserService), out var browserService))
        {
            (browserService as IDisposable)?.Dispose();
        }
        Services.Clear();
    }
}
