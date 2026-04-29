using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Controls;

public static class StatusIndicator
{
    public static Ellipse CreateBrowserStatus(BrowserStatus status)
    {
        var ellipse = new Ellipse { Width = 10, Height = 10 };
        ellipse.SetValue(FillProperty, GetStatusBrush(status));
        return ellipse;
    }

    public static Ellipse CreateProxyStatus(ProxyStatus status)
    {
        var ellipse = new Ellipse { Width = 10, Height = 10 };
        ellipse.SetValue(FillProperty, GetProxyStatusBrush(status));
        return ellipse;
    }

    private static Brush GetStatusBrush(BrowserStatus status)
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

    private static Brush GetProxyStatusBrush(ProxyStatus status)
    {
        return status switch
        {
            ProxyStatus.Available => new SolidColorBrush(Color.FromRgb(52, 211, 153)),
            ProxyStatus.Testing => new SolidColorBrush(Color.FromRgb(96, 165, 250)),
            ProxyStatus.Unavailable => new SolidColorBrush(Color.FromRgb(248, 113, 113)),
            _ => new SolidColorBrush(Color.FromRgb(156, 163, 175))
        };
    }

    public static Rectangle CreateColorIndicator(string color)
    {
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(color);
            return new Rectangle
            {
                Width = 16,
                Height = 16,
                Fill = new SolidColorBrush(c),
                RadiusX = 4,
                RadiusY = 4
            };
        }
        catch
        {
            return new Rectangle { Width = 16, Height = 16, Fill = Brushes.Gray };
        }
    }
}

public static class ColorIndicator
{
    public static Brush ParseColor(string? colorStr)
    {
        if (string.IsNullOrEmpty(colorStr)) return Brushes.Gray;
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(colorStr);
            return new SolidColorBrush(c);
        }
        catch
        {
            return Brushes.Gray;
        }
    }
}
