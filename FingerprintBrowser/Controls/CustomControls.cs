using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Controls;

/// <summary>
/// 状态指示器 - 辅助类，用于创建状态指示器
/// </summary>
public static class StatusIndicatorFactory
{
    public static Ellipse Create(BrowserStatus status)
    {
        var ellipse = new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = GetStatusBrush(status)
        };
        return ellipse;
    }

    public static SolidColorBrush GetStatusBrush(BrowserStatus status)
    {
        return status switch
        {
            BrowserStatus.Running => new SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 211, 153)),
            BrowserStatus.Error => new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 113, 113)),
            _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128))
        };
    }
}

/// <summary>
/// 分组颜色指示器 - 辅助类
/// </summary>
public static class GroupColorIndicatorFactory
{
    public static System.Windows.Controls.Border Create(string? hexColor)
    {
        var color = TryParseColor(hexColor);
        return new System.Windows.Controls.Border
        {
            Width = 4,
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(color)
        };
    }

    private static System.Windows.Media.Color TryParseColor(string? hexColor)
    {
        try
        {
            if (!string.IsNullOrEmpty(hexColor))
            {
                return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor);
            }
        }
        catch { }
        return System.Windows.Media.Color.FromRgb(59, 130, 246);
    }
}
