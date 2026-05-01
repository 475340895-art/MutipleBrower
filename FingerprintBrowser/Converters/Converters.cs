using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Converters
{
    public class BrowserStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BrowserStatus status)
            {
                return status switch
                {
                    BrowserStatus.Running => new SolidColorBrush(Color.FromRgb(0x52, 0xc4, 0x1a)),
                    BrowserStatus.Stopped => new SolidColorBrush(Color.FromRgb(0xd9, 0xd9, 0xd9)),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BrowserStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BrowserStatus status)
            {
                return status switch
                {
                    BrowserStatus.Running => "运行中",
                    BrowserStatus.Stopped => "已停止",
                    _ => "未知"
                };
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ProxyStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProxyStatus status)
            {
                return status switch
                {
                    ProxyStatus.Normal => new SolidColorBrush(Color.FromRgb(0x52, 0xc4, 0x1a)),
                    ProxyStatus.Failed => new SolidColorBrush(Color.FromRgb(0xff, 0x4d, 0x4f)),
                    ProxyStatus.Testing => new SolidColorBrush(Color.FromRgb(0xfa, 0x8c, 0x16)),
                    ProxyStatus.Untested => new SolidColorBrush(Color.FromRgb(0xd9, 0xd9, 0xd9)),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ProxyStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProxyStatus status)
            {
                return status switch
                {
                    ProxyStatus.Normal => "正常",
                    ProxyStatus.Failed => "失败",
                    ProxyStatus.Testing => "测试中",
                    ProxyStatus.Untested => "未测试",
                    _ => "未知"
                };
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorStr && !string.IsNullOrEmpty(colorStr))
            {
                try
                {
                    if (colorStr.StartsWith("#") && colorStr.Length == 7)
                    {
                        var r = byte.Parse(colorStr.Substring(1, 2), NumberStyles.HexNumber);
                        var g = byte.Parse(colorStr.Substring(3, 2), NumberStyles.HexNumber);
                        var b = byte.Parse(colorStr.Substring(5, 2), NumberStyles.HexNumber);
                        return new SolidColorBrush(Color.FromRgb(r, g, b));
                    }
                }
                catch { }
            }
            return new SolidColorBrush(Color.FromRgb(0x16, 0x77, 0xff));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }
    }

    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count) return count > 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
