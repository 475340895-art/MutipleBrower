using System.Windows;
using System.Windows.Media;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Controls;

public static class StatusIndicatorFactory
{
    public static System.Windows.Shapes.Ellipse CreateBrowserStatusIndicator(BrowserStatus status)
    {
        return new System.Windows.Shapes.Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = GetBrowserStatusBrush(status),
            Stroke = Brushes.White,
            StrokeThickness = 1
        };
    }
    
    public static System.Windows.Shapes.Rectangle CreateProxyStatusIndicator(ProxyStatus status)
    {
        return new System.Windows.Shapes.Rectangle
        {
            Width = 10,
            Height = 10,
            Fill = GetProxyStatusBrush(status),
            RadiusX = 2,
            RadiusY = 2
        };
    }
    
    public static System.Windows.Shapes.Ellipse CreateColorIndicator(string hexColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        return new System.Windows.Shapes.Ellipse
        {
            Width = 16,
            Height = 16,
            Fill = new SolidColorBrush(color)
        };
    }
    
    private static Brush GetBrowserStatusBrush(BrowserStatus status)
    {
        return status switch
        {
            BrowserStatus.Running => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ade80")),
            BrowserStatus.Starting => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fbbf24")),
            BrowserStatus.Stopped => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9ca3af")),
            BrowserStatus.Error => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f87171")),
            _ => Brushes.Gray
        };
    }
    
    private static Brush GetProxyStatusBrush(ProxyStatus status)
    {
        return status switch
        {
            ProxyStatus.Available => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ade80")),
            ProxyStatus.Testing => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fbbf24")),
            ProxyStatus.Unavailable => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f87171")),
            _ => Brushes.Gray
        };
    }
}
