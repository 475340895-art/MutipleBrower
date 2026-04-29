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
            BrowserStatus.Idle => System.Windows.Media.Brushes.Gray,
            BrowserStatus.Starting => System.Windows.Media.Brushes.Orange,
            BrowserStatus.Running => System.Windows.Media.Brushes.LimeGreen,
            BrowserStatus.Stopped => System.Windows.Media.Brushes.Gray,
            BrowserStatus.Error => System.Windows.Media.Brushes.Red,
            _ => System.Windows.Media.Brushes.Gray
        };

        return new System.Windows.Shapes.Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = brush,
            Stroke = System.Windows.Media.Brushes.White,
            StrokeThickness = 1
        };
    }

    public static System.Windows.Shapes.Ellipse CreateProxyStatusIndicator(ProxyStatus status)
    {
        var brush = status switch
        {
            ProxyStatus.Available => System.Windows.Media.Brushes.LimeGreen,
            ProxyStatus.Unavailable => System.Windows.Media.Brushes.Red,
            ProxyStatus.Testing => System.Windows.Media.Brushes.Orange,
            _ => System.Windows.Media.Brushes.Gray
        };

        return new System.Windows.Shapes.Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = brush,
            Stroke = System.Windows.Media.Brushes.White,
            StrokeThickness = 1
        };
    }
}
