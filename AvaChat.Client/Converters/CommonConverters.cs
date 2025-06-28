using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AvaChat.Client.Converters;

/// <summary>
/// 用户状态到颜色的转换器
/// </summary>
public class StatusColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UserStatus status)
        {
            return status switch
            {
                UserStatus.Online => new SolidColorBrush(Colors.Green),
                UserStatus.Away => new SolidColorBrush(Colors.Orange),
                UserStatus.Busy => new SolidColorBrush(Colors.Red),
                UserStatus.Invisible => new SolidColorBrush(Colors.Gray),
                UserStatus.Offline => new SolidColorBrush(Colors.Gray),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 获取昵称首字符的转换器
/// </summary>
public class FirstCharConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrEmpty(str))
        {
            return str[0].ToString().ToUpper();
        }
        return "?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 空值到可见性的转换器
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 非空值到可见性的转换器
/// </summary>
public class NotNullToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 显示名称转换器（优先显示备注名）
/// </summary>
public class DisplayNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var remark = value as string;
        var nickname = parameter as string;

        return !string.IsNullOrEmpty(remark) ? remark : nickname ?? "未知用户";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 相对时间转换器
/// </summary>
public class RelativeTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime time)
        {
            var diff = DateTime.Now - time;

            if (diff.TotalMinutes < 1)
                return "刚刚";
            else if (diff.TotalHours < 1)
                return $"{(int)diff.TotalMinutes}分钟前";
            else if (diff.TotalDays < 1)
                return $"{(int)diff.TotalHours}小时前";
            else if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays}天前";
            else
                return time.ToString("MM/dd");
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 判断是否为当前用户的转换器
/// </summary>
public class IsCurrentUserConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string senderNumber)
        {
            // TODO: 从应用状态获取当前用户号码
            return senderNumber == "10000001"; // 临时硬编码
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 判断是否不是当前用户的转换器
/// </summary>
public class IsNotCurrentUserConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string senderNumber)
        {
            // TODO: 从应用状态获取当前用户号码
            return senderNumber != "10000001"; // 临时硬编码
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 消息状态图标转换器
/// </summary>
public class MessageStatusIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MessageStatus status)
        {
            return status switch
            {
                MessageStatus.Sending => "M12 2l4 4h-3v6h-2V6H8l4-4z",
                MessageStatus.Sent => "M9 16.2L4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z",
                MessageStatus.Delivered => "M18 7l-1.41-1.41-6.34 6.34 1.41 1.41L18 7zm4.24-1.41L11.66 16.17 7.48 12l-1.41 1.41L11.66 19l12-12-1.42-1.41zM.41 13.41L6 19l1.41-1.41L1.83 12 .41 13.41z",
                MessageStatus.Read => "M18 7l-1.41-1.41-6.34 6.34 1.41 1.41L18 7zm4.24-1.41L11.66 16.17 7.48 12l-1.41 1.41L11.66 19l12-12-1.42-1.41zM.41 13.41L6 19l1.41-1.41L1.83 12 .41 13.41z",
                MessageStatus.Failed => "M12 2C6.47 2 2 6.47 2 12s4.47 10 10 10 10-4.47 10-10S17.53 2 12 2zm5 13.59L15.59 17 12 13.41 8.41 17 7 15.59 10.59 12 7 8.41 8.41 7 12 10.59 15.59 7 17 8.41 13.41 12 17 15.59z",
                _ => ""
            };
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 导航背景颜色转换器
/// </summary>
public class NavigationBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NavigationType currentNav && parameter is string navTypeStr)
        {
            if (Enum.TryParse<NavigationType>(navTypeStr, out var targetNav))
            {
                return currentNav == targetNav ?
                    new SolidColorBrush(Color.FromArgb(60, 0, 120, 215)) :
                    new SolidColorBrush(Colors.Transparent);
            }
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 导航前景颜色转换器
/// </summary>
public class NavigationForegroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NavigationType currentNav && parameter is string navTypeStr)
        {
            if (Enum.TryParse<NavigationType>(navTypeStr, out var targetNav))
            {
                return currentNav == targetNav ?
                    new SolidColorBrush(Color.FromRgb(0, 120, 215)) :
                    new SolidColorBrush(Color.FromRgb(96, 96, 96));
            }
        }
        return new SolidColorBrush(Color.FromRgb(96, 96, 96));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 连接状态颜色转换器
/// </summary>
public class ConnectedColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected ?
                new SolidColorBrush(Colors.Green) :
                new SolidColorBrush(Colors.Red);
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 状态文本转换器
/// </summary>
public class StatusTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UserStatus status)
        {
            return status switch
            {
                UserStatus.Online => "在线",
                UserStatus.Away => "离开",
                UserStatus.Busy => "忙碌",
                UserStatus.Invisible => "隐身",
                UserStatus.Offline => "离线",
                _ => "未知"
            };
        }
        return "未知";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 个性签名或状态转换器
/// </summary>
public class SignatureOrStatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var signature = value as string;
        var status = parameter as UserStatus?;

        if (!string.IsNullOrEmpty(signature))
        {
            return signature;
        }

        if (status.HasValue)
        {
            return status.Value switch
            {
                UserStatus.Online => "在线",
                UserStatus.Away => "离开",
                UserStatus.Busy => "忙碌",
                UserStatus.Invisible => "隐身",
                UserStatus.Offline => "离线",
                _ => "未知状态"
            };
        }

        return "未设置个性签名";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 离线状态可见性转换器
/// </summary>
public class OfflineVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UserStatus status)
        {
            return status == UserStatus.Offline;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 零值到可见性转换器
/// </summary>
public class ZeroToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0;
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
