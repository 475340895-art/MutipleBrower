using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Controls;

public static class StatusIndicator
{
    public static SolidColorBrush GetStatusColor(BrowserStatus status)
    {
        return status switch
        {
            BrowserStatus.Running => new SolidColorBrush(Color.FromRgb(52, 211, 153)),
            BrowserStatus.Starting => new SolidColorBrush(Color.FromRgb(251, 191, 36)),
            BrowserStatus.Stopping => new SolidColorBrush(Color.FromRgb(251, 191, 36)),
            BrowserStatus.Error => new SolidColorBrush(Color.FromRgb(248, 113, 113)),
            _ => new SolidColorBrush(Color.FromRgb(156, 163, 175))
        };
    }

    public static string GetStatusText(BrowserStatus status)
    {
        return status switch
        {
            BrowserStatus.Running => "运行中",
            BrowserStatus.Starting => "启动中",
            BrowserStatus.Stopping => "关闭中",
            BrowserStatus.Error => "错误",
            _ => "空闲"
        };
    }

    public static SolidColorBrush GetProxyStatusColor(ProxyStatus status)
    {
        return status switch
        {
            ProxyStatus.Available => new SolidColorBrush(Color.FromRgb(52, 211, 153)),
            ProxyStatus.Unavailable => new SolidColorBrush(Color.FromRgb(248, 113, 113)),
            ProxyStatus.Testing => new SolidColorBrush(Color.FromRgb(251, 191, 36)),
            _ => new SolidColorBrush(Color.FromRgb(156, 163, 175))
        };
    }

    public static string GetProxyStatusText(ProxyStatus status)
    {
        return status switch
        {
            ProxyStatus.Available => "可用",
            ProxyStatus.Unavailable => "不可用",
            ProxyStatus.Testing => "测试中",
            _ => "未知"
        };
    }

    public static Border CreateStatusIndicator(BrowserStatus status)
    {
        var border = new Border
        {
            Width = 8,
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Background = GetStatusColor(status),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        return border;
    }

    public static Border CreateColorIndicator(string hexColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        
        var border = new Border
        {
            Width = 12,
            Height = 12,
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(color),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        return border;
    }
}
