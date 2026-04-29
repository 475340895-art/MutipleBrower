using System.Windows;
using System.Windows.Media;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Controls;

public static class StatusIndicatorFactory
{
    public static System.Windows.Shapes.Ellipse CreateBrowserStatusIndicator(BrowserStatus status)
    {
        var brush = status switch
        {
            BrowserStatus.Idle => Brushes.Gray,
            BrowserStatus.Starting => Brushes.Orange,
            BrowserStatus.Running => Brushes.LimeGreen,
            BrowserStatus.Stopped => Brushes.Gray,
            BrowserStatus.Error => Brushes.Red,
            _ => Brushes.Gray
        };

        return new System.Windows.Shapes.Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = brush,
            Stroke = Brushes.White,
            StrokeThickness = 1
        };
    }

    public static System.Windows.Shapes.Ellipse CreateProxyStatusIndicator(ProxyStatus status)
    {
        var brush = status switch
        {
            ProxyStatus.Available => Brushes.LimeGreen,
            ProxyStatus.Unavailable => Brushes.Red,
            ProxyStatus.Testing => Brushes.Orange,
            ProxyStatus.Unknown => Brushes.Gray,
            _ => Brushes.Gray
        };

        return new System.Windows.Shapes.Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = brush,
            Stroke = Brushes.White,
            StrokeThickness = 1
        };
    }
}
