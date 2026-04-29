using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Controls
{
    /// <summary>
    /// 浏览器状态指示器
    /// </summary>
    public class BrowserStatusIndicator : Ellipse
    {
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(nameof(Status), typeof(BrowserEnvironmentStatus), typeof(BrowserStatusIndicator),
                new PropertyMetadata(BrowserEnvironmentStatus.Idle, OnStatusChanged));

        public BrowserEnvironmentStatus Status
        {
            get => (BrowserEnvironmentStatus)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BrowserStatusIndicator indicator)
            {
                indicator.UpdateColor();
            }
        }

        private void UpdateColor()
        {
            Fill = Status switch
            {
                BrowserEnvironmentStatus.Idle => new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                BrowserEnvironmentStatus.Starting => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                BrowserEnvironmentStatus.Running => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                BrowserEnvironmentStatus.Stopping => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                BrowserEnvironmentStatus.Error => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                _ => new SolidColorBrush(Color.FromRgb(128, 128, 128))
            };

            Width = 10;
            Height = 10;
        }

        public BrowserStatusIndicator()
        {
            UpdateColor();
        }
    }

    /// <summary>
    /// 代理状态指示器
    /// </summary>
    public class ProxyStatusIndicator : Ellipse
    {
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(nameof(Status), typeof(ProxyStatus), typeof(ProxyStatusIndicator),
                new PropertyMetadata(ProxyStatus.Untested, OnStatusChanged));

        public ProxyStatus Status
        {
            get => (ProxyStatus)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProxyStatusIndicator indicator)
            {
                indicator.UpdateColor();
            }
        }

        private void UpdateColor()
        {
            Fill = Status switch
            {
                ProxyStatus.Untested => new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                ProxyStatus.Valid => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                ProxyStatus.Invalid => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                ProxyStatus.Timeout => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                _ => new SolidColorBrush(Color.FromRgb(128, 128, 128))
            };

            Width = 10;
            Height = 10;
        }

        public ProxyStatusIndicator()
        {
            UpdateColor();
        }
    }

    /// <summary>
    /// 颜色指示器（用于分组）
    /// </summary>
    public class ColorIndicator : Rectangle
    {
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(nameof(Color), typeof(string), typeof(ColorIndicator),
                new PropertyMetadata("#1890ff", OnColorChanged));

        public string Color
        {
            get => (string)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorIndicator indicator)
            {
                indicator.UpdateFill();
            }
        }

        private void UpdateFill()
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(Color);
                Fill = new SolidColorBrush(color);
            }
            catch
            {
                Fill = new SolidColorBrush(Color.FromRgb(24, 144, 255));
            }

            Width = 4;
            RadiusX = 2;
            RadiusY = 2;
        }

        public ColorIndicator()
        {
            UpdateFill();
        }
    }
}
