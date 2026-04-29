using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Controls;

/// <summary>
/// 环境列表项控件
/// </summary>
public class EnvironmentListItem : Control
{
    public static readonly DependencyProperty EnvironmentProperty =
        DependencyProperty.Register(nameof(Environment), typeof(BrowserEnvironment), typeof(EnvironmentListItem),
            new PropertyMetadata(null, OnEnvironmentChanged));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(EnvironmentListItem),
            new PropertyMetadata(false));

    public BrowserEnvironment? Environment
    {
        get => (BrowserEnvironment?)GetValue(EnvironmentProperty);
        set => SetValue(EnvironmentProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    private static void OnEnvironmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // 环境变更时的处理逻辑
    }
}

/// <summary>
/// 状态指示器
/// </summary>
public class StatusIndicator : Ellipse
{
    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(nameof(Status), typeof(BrowserStatus), typeof(StatusIndicator),
            new PropertyMetadata(BrowserStatus.Stopped, OnStatusChanged));

    public BrowserStatus Status
    {
        get => (BrowserStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusIndicator indicator)
        {
            indicator.UpdateColor();
        }
    }

    private void UpdateColor()
    {
        Fill = Status switch
        {
            BrowserStatus.Running => new SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 211, 153)),
            BrowserStatus.Error => new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 113, 113)),
            _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128))
        };

        Width = 8;
        Height = 8;
    }
}

/// <summary>
/// 分组颜色指示器
/// </summary>
public class GroupColorIndicator : Border
{
    public static readonly DependencyProperty ColorProperty =
        DependencyProperty.Register(nameof(Color), typeof(string), typeof(GroupColorIndicator),
            new PropertyMetadata("#3B82F6", OnColorChanged));

    public string Color
    {
        get => (string)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GroupColorIndicator indicator)
        {
            indicator.UpdateColor();
        }
    }

    private void UpdateColor()
    {
        try
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Color);
            Background = new SolidColorBrush(color);
        }
        catch
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
        }

        Width = 4;
        CornerRadius = new CornerRadius(2);
    }
}
