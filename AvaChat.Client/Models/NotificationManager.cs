using System.Diagnostics;
using System.Media;
using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace AvaChat.Client.Models;

/// <summary>
/// 通知管理器 - 处理托盘图标闪烁和提示音
/// </summary>
public static class NotificationManager
{
    private static Timer? _blinkTimer;
    private static bool _isBlinking = false;
    private static WindowIcon? _originalIcon;
    private static WindowIcon? _alertIcon;

    // Windows API for playing sounds
    [DllImport("winmm.dll", SetLastError = true)]
    private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

    private const uint SND_FILENAME = 0x00020000;
    private const uint SND_ASYNC = 0x00000001;

    static NotificationManager()
    {
        // 创建提示图标（可以是不同颜色的图标）
        InitializeIcons();
    }

    private static void InitializeIcons()
    {
        try
        {
            if (App.Current?.Resources["AppTrayIcon"] is WindowIcon icon)
            {
                _originalIcon = icon;
                // 如果有警告图标资源，可以使用；否则使用原图标
                _alertIcon = App.Current?.Resources["AppTrayIconAlert"] as WindowIcon ?? icon;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationManager] 初始化图标失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示新消息通知
    /// </summary>
    /// <param name="isSystemMessage">是否为系统消息</param>
    /// <param name="senderName">发送者名称</param>
    /// <param name="messageContent">消息内容</param>
    public static void ShowMessageNotification(bool isSystemMessage, string senderName, string messageContent)
    {
        try
        {
            // 播放提示音
            PlayNotificationSound(isSystemMessage);

            // 开始托盘图标闪烁
            StartTrayIconBlink();

            // 显示系统通知（如果支持）
            ShowSystemNotification(isSystemMessage, senderName, messageContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationManager] 显示通知失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 播放通知声音
    /// </summary>
    private static void PlayNotificationSound(bool isSystemMessage)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows 平台使用系统声音
                if (isSystemMessage)
                {
                    // 系统消息使用系统提示音
                    Console.Beep(800, 200); // 高音短促
                }
                else
                {
                    // 用户消息使用默认通知音
                    Console.Beep(600, 300); // 中音较长
                }
            }
            else
            {
                // 其他平台使用控制台蜂鸣
                Console.Beep();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationManager] 播放声音失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 开始托盘图标闪烁
    /// </summary>
    private static void StartTrayIconBlink()
    {
        try
        {
            if (_isBlinking || App.MainTrayIcon == null) return;

            _isBlinking = true;
            var blinkCount = 0;
            const int maxBlinks = 10; // 闪烁5次（每次闪烁包含显示和隐藏）

            _blinkTimer = new Timer(_ =>
            {
                try
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (App.MainTrayIcon != null)
                        {
                            // 交替显示原图标和警告图标
                            App.MainTrayIcon.Icon = (blinkCount % 2 == 0) ? _alertIcon : _originalIcon;
                        }
                    });

                    blinkCount++;

                    if (blinkCount >= maxBlinks)
                    {
                        StopTrayIconBlink();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NotificationManager] 托盘图标闪烁失败: {ex.Message}");
                    StopTrayIconBlink();
                }
            }, null, 0, 300); // 每300毫秒切换一次
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationManager] 启动托盘图标闪烁失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 停止托盘图标闪烁
    /// </summary>
    public static void StopTrayIconBlink()
    {
        try
        {
            _isBlinking = false;
            _blinkTimer?.Dispose();
            _blinkTimer = null;

            // 恢复原始图标
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (App.MainTrayIcon != null && _originalIcon != null)
                {
                    App.MainTrayIcon.Icon = _originalIcon;
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationManager] 停止托盘图标闪烁失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示系统通知
    /// </summary>
    private static void ShowSystemNotification(bool isSystemMessage, string senderName, string messageContent)
    {
        try
        {
            // 限制消息内容长度
            var content = messageContent.Length > 50 ? messageContent[..50] + "..." : messageContent;
            var title = isSystemMessage ? "系统消息" : $"来自 {senderName} 的消息";

            // ShowWindowsNotification(title, content);

            // 兜底方案：控制台输出
            Console.WriteLine($"[通知] {title}: {content}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationManager] 显示系统通知失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 当用户查看消息时停止通知
    /// </summary>
    public static void OnMessageViewed()
    {
        StopTrayIconBlink();
    }
}
