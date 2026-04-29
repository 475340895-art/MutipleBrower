using System.Windows;
using System.Windows.Threading;
using Serilog;

namespace FingerprintBrowser;

/// <summary>
/// 全局异常处理辅助类
/// </summary>
public static class ExceptionHandler
{
    /// <summary>
    /// 初始化全局异常处理
    /// </summary>
    public static void Initialize()
    {
        // UI 线程异常
        Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 非 UI 线程异常
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // 任务未观察异常
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        Log.Information("全局异常处理已初始化");
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "UI 线程未处理异常");

        var result = MessageBox.Show(
            $"发生错误:\n{e.Exception.Message}\n\n是否继续运行?",
            "错误",
            MessageBoxButton.YesNo,
            MessageBoxImage.Error);

        if (result == MessageBoxResult.Yes)
        {
            e.Handled = true;
        }
        else
        {
            Application.Current.Shutdown(1);
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;

        Log.Fatal(exception, "非 UI 线程未处理异常 (IsTerminating: {IsTerminating})", e.IsTerminating);

        if (e.IsTerminating)
        {
            MessageBox.Show(
                $"发生致命错误:\n{exception?.Message}\n\n应用程序将关闭。",
                "致命错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "任务未观察异常");
        e.SetObserved();
    }

    /// <summary>
    /// 安全地执行操作
    /// </summary>
    public static T SafeExecute<T>(Func<T> action, T defaultValue = default!, string? errorMessage = null)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            Log.Error(ex, errorMessage ?? "执行操作时发生异常");
            return defaultValue;
        }
    }

    /// <summary>
    /// 安全地执行异步操作
    /// </summary>
    public static async Task<T> SafeExecuteAsync<T>(Func<Task<T>> action, T defaultValue = default!, string? errorMessage = null)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            Log.Error(ex, errorMessage ?? "执行异步操作时发生异常");
            return defaultValue;
        }
    }

    /// <summary>
    /// 安全地执行操作（无返回值）
    /// </summary>
    public static void SafeExecute(Action action, string? errorMessage = null)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Log.Error(ex, errorMessage ?? "执行操作时发生异常");
        }
    }

    /// <summary>
    /// 获取异常的完整信息
    /// </summary>
    public static string GetExceptionDetails(Exception exception)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"类型: {exception.GetType().FullName}");
        sb.AppendLine($"消息: {exception.Message}");
        sb.AppendLine($"源: {exception.Source}");
        sb.AppendLine($"堆栈:");

        var stackTrace = exception.StackTrace?.Split('\n')
            .Take(10)
            .Select(line => $"  {line.Trim()}");

        if (stackTrace != null)
        {
            sb.AppendLine(string.Join("\n", stackTrace));
        }

        if (exception.InnerException != null)
        {
            sb.AppendLine("\n内部异常:");
            sb.AppendLine(GetExceptionDetails(exception.InnerException));
        }

        return sb.ToString();
    }
}
