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
/// 判断是否为当前用户的转换器
/// </summary>
public class IsCurrentUserConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string senderNumber)
        {
            return senderNumber == App.CurrentUserId;
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
            return senderNumber != App.CurrentUserId;
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
                MessageStatus.Sending => "M3 12l18 0M9 19H7v-4.5c0-.83-.67-1.5-1.5-1.5S4 13.67 4 14.5V19h2m10-6v4.5c0 .83.67 1.5 1.5 1.5s1.5-.67 1.5-1.5V13h2",
                MessageStatus.Failed => "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm3.59-13L12 10.59 8.41 7 7 8.41 10.59 12 7 15.59 8.41 17 12 13.41 15.59 17 17 15.59 13.41 12 17 8.41z",
                MessageStatus.Delivered => "M20 2H4c-1.1 0-1.99.9-1.99 2L2 22l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm-7 9V9l-5 4 5 4v-3h5V9l-5 4z",
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

/// <summary>
/// 零值到布尔值转换器
/// </summary>
public class ZeroToBoolConverter : IValueConverter
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

/// <summary>
/// 显示名称转换器（优先显示备注名）
/// </summary>
public class DisplayNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var alterName = value as string;
        var userName = parameter as string;

        return !string.IsNullOrEmpty(alterName) ? alterName : userName ?? "未知用户";
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 日期时间转换器
/// </summary>
public class DateTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            var now = DateTime.Now;
            var diff = now - dateTime;

            if (diff.TotalMinutes < 1)
                return "刚刚";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes}分钟前";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours}小时前";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays}天前";

            return dateTime.ToString("MM/dd");
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 用户头像颜色转换器
/// </summary>
public class AvatarColorConverter : IValueConverter
{
    private readonly List<(string Start, string End)> _colorPairs = new()
    {
        ("#FF4A90E2", "#FF7B68EE"),
        ("#FF42A5F5", "#FF26C6DA"),
        ("#FF66BB6A", "#FF26A69A"),
        ("#FFFF7043", "#FFFF5722"),
        ("#FFAB47BC", "#FF8E24AA"),
        ("#FF29B6F6", "#FF039BE5"),
        ("#FF26C6DA", "#FF00ACC1"),
        ("#FF66BB6A", "#FF43A047")
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string userName && !string.IsNullOrEmpty(userName))
        {
            var hash = userName.GetHashCode();
            var index = Math.Abs(hash) % _colorPairs.Count;
            var colorPair = _colorPairs[index];

            var brush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative)
            };

            brush.GradientStops.Add(new GradientStop(Color.Parse(colorPair.Start), 0));
            brush.GradientStops.Add(new GradientStop(Color.Parse(colorPair.End), 1));

            return brush;
        }

        // 默认渐变色
        var defaultBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative)
        };
        defaultBrush.GradientStops.Add(new GradientStop(Color.Parse("#FF4A90E2"), 0));
        defaultBrush.GradientStops.Add(new GradientStop(Color.Parse("#FF7B68EE"), 1));

        return defaultBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}