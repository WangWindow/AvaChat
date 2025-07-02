using System.Collections.ObjectModel;
using AvaChat.Server.Hubs;
using AvaChat.Server.Models;
using AvaChat.Shared.Models;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AvaChat.Server.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly Timer _statsTimer;
    private readonly IServiceProvider? _serviceProvider;

    // 在线用户列表
    [ObservableProperty]
    private ObservableCollection<UserDisplayInfo> _onlineUsers = [];

    // 所有用户列表
    [ObservableProperty]
    private ObservableCollection<UserDisplayInfo> _allUsers = [];

    // 最近消息列表
    [ObservableProperty]
    private ObservableCollection<MessageDisplayInfo> _recentMessages = [];

    // 好友关系列表
    [ObservableProperty]
    private ObservableCollection<FriendshipDisplayInfo> _friendships = [];

    // 当前选中的用户
    [ObservableProperty]
    private UserDisplayInfo? _selectedUser;

    // 当前选中的监控页面
    [ObservableProperty]
    private string _selectedTab = "在线用户";

    // 搜索文本
    [ObservableProperty]
    private string _searchText = string.Empty;

    // 可见性属性
    public bool IsOnlineUsersTabSelected => SelectedTab == "在线用户";
    public bool IsAllUsersTabSelected => SelectedTab == "所有用户";
    public bool IsMessagesTabSelected => SelectedTab == "消息历史";
    public bool IsFriendshipsTabSelected => SelectedTab == "好友关系";

    // 系统消息内容（多行文本）
    [ObservableProperty]
    private string _systemMessages = string.Empty;

    // 待发送的系统消息
    [ObservableProperty]
    private string _messageToSend = string.Empty;

    // 服务器状态信息
    [ObservableProperty]
    private string _serverStats = "消息: 0 | 用户: 0 | 在线: 0";

    // 用户显示信息类
    public class UserDisplayInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public UserStatus Status { get; set; }
        public DateTime? LastLoginTime { get; set; }

        public string DisplayText => $"{UserName} ({UserId})";
        public string StatusText => Status switch
        {
            UserStatus.Online => "在线",
            UserStatus.Offline => "离线",
            _ => "未知"
        };
        public string LastLoginText => LastLoginTime?.ToString("MM-dd HH:mm") ?? "从未登录";
    }

    // 消息显示信息类
    public class MessageDisplayInfo
    {
        public int MessageId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public MessageType MessageType { get; set; }
        public MessageStatus Status { get; set; }

        public string MessageTypeText => MessageType switch
        {
            MessageType.Text => "文本",
            MessageType.System => "系统",
            _ => "未知"
        };

        public string StatusText => Status switch
        {
            MessageStatus.Sending => "发送中",
            MessageStatus.Delivered => "已送达",
            MessageStatus.Failed => "发送失败",
            _ => "未知"
        };

        public string TimestampText => Timestamp.ToString("MM-dd HH:mm:ss");
        public string DisplayText => $"{SenderName} → {ReceiverName}: {Content}";
    }

    // 好友关系显示信息类
    public class FriendshipDisplayInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FriendUserId { get; set; } = string.Empty;
        public string FriendUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string DisplayText => $"{UserName} ↔ {FriendUserName}";
        public string CreatedAtText => CreatedAt.ToString("MM-dd HH:mm");
    }

    // 发送系统消息命令
    [RelayCommand]
    private async Task SendSystemMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageToSend))
            return;

        try
        {
            var timestamp = DateTime.Now;
            var formattedTime = timestamp.ToString("yyyy-MM-dd HH:mm:ss");

            // 追加到系统消息区
            SystemMessages += $"[{formattedTime}] 系统广播: {MessageToSend}\n";

            // 通过SignalR发送系统广播
            if (_serviceProvider != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var hubContext = scope.ServiceProvider.GetService<IHubContext<ChatHub>>();
                var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                if (hubContext != null)
                {
                    // 创建系统消息
                    var systemMessage = new Message
                    {
                        SenderId = "System",
                        ReceiverId = "Broadcast",
                        Content = MessageToSend,
                        Timestamp = timestamp,
                        MessageType = MessageType.System,
                        Status = MessageStatus.Delivered
                    };

                    // 保存到数据库
                    db.Messages.Add(systemMessage);
                    await db.SaveChangesAsync();

                    // 广播给所有在线用户
                    await hubContext.Clients.All.SendAsync("ReceiveSystemMessage", systemMessage);

                    MessageToSend = string.Empty;
                    SystemMessages += $"[{formattedTime}] 广播已发送给所有在线用户\n";
                }
                else
                {
                    SystemMessages += $"[{formattedTime}] 错误: 无法获取SignalR上下文\n";
                }
            }
            else
            {
                SystemMessages += $"[{formattedTime}] 错误: 服务提供程序未初始化\n";
            }

            // 更新统计信息
            await UpdateServerStatsAsync();
        }
        catch (Exception ex)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SystemMessages += $"[{timestamp}] 错误: 发送系统消息失败 - {ex.Message}\n";
        }
    }

    // 踢出用户命令
    [RelayCommand]
    private async Task KickUserAsync()
    {
        if (SelectedUser == null) return;

        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SystemMessages += $"[{timestamp}] 正在踢出用户: {SelectedUser.DisplayText}\n";

            if (_serviceProvider != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var hubContext = scope.ServiceProvider.GetService<IHubContext<ChatHub>>();
                var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                if (hubContext != null)
                {
                    // 发送踢出通知
                    await hubContext.Clients.User(SelectedUser.UserId).SendAsync("ReceiveError", "您已被管理员踢出");

                    // 更新用户状态为离线
                    var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == SelectedUser.UserId);
                    if (user != null)
                    {
                        user.Status = UserStatus.Offline;
                        await db.SaveChangesAsync();
                    }

                    SystemMessages += $"[{timestamp}] 用户 {SelectedUser.DisplayText} 已被踢出\n";
                    await ReloadOnlineUsersAsync();
                }
            }
        }
        catch (Exception ex)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SystemMessages += $"[{timestamp}] 错误: 踢出用户失败 - {ex.Message}\n";
        }
    }

    // 清除日志命令
    [RelayCommand]
    private void ClearLogs()
    {
        SystemMessages = string.Empty;
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        SystemMessages += $"[{timestamp}] 日志已清除\n";
    }

    // 切换标签页命令
    [RelayCommand]
    private async Task SwitchTabAsync(string tabName)
    {
        SelectedTab = tabName;

        // 通知可见性属性变更
        OnPropertyChanged(nameof(IsOnlineUsersTabSelected));
        OnPropertyChanged(nameof(IsAllUsersTabSelected));
        OnPropertyChanged(nameof(IsMessagesTabSelected));
        OnPropertyChanged(nameof(IsFriendshipsTabSelected));

        await LoadTabDataAsync(tabName);
    }

    // 搜索命令
    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadTabDataAsync(SelectedTab);
    }

    // 删除用户命令
    [RelayCommand]
    private async Task DeleteUserAsync(UserDisplayInfo user)
    {
        if (user == null) return;

        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (_serviceProvider != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                // 删除用户相关的所有数据
                var userEntity = await db.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId);
                if (userEntity != null)
                {
                    // 删除用户的消息
                    var messages = db.Messages.Where(m => m.SenderId == user.UserId || m.ReceiverId == user.UserId);
                    db.Messages.RemoveRange(messages);

                    // 删除用户的好友关系
                    var friendships = db.Friendships.Where(f => f.UserId == user.UserId || f.FriendUserId == user.UserId);
                    db.Friendships.RemoveRange(friendships);

                    // 删除用户
                    db.Users.Remove(userEntity);

                    await db.SaveChangesAsync();

                    SystemMessages += $"[{timestamp}] 用户 {user.DisplayText} 及其相关数据已删除\n";

                    // 刷新当前标签页数据
                    await LoadTabDataAsync(SelectedTab);
                }
            }
        }
        catch (Exception ex)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SystemMessages += $"[{timestamp}] 错误: 删除用户失败 - {ex.Message}\n";
        }
    }

    public MainWindowViewModel()
    {
        // 获取服务提供程序
        _serviceProvider = Program.GetServiceProvider();

        // 添加启动消息
        var startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        SystemMessages += $"[{startTime}] AvaChat 服务器已启动\n";
        SystemMessages += $"[{startTime}] 服务地址: http://localhost:5000\n";
        SystemMessages += $"[{startTime}] 等待客户端连接...\n";

        // 初始化在线用户列表
        _ = ReloadOnlineUsersAsync();

        // 初始化默认标签页数据
        _ = LoadTabDataAsync(SelectedTab);

        // 设置定时更新服务器统计信息（每10秒更新一次）
        _statsTimer = new Timer(async _ =>
        {
            await UpdateServerStatsAsync();
            await ReloadOnlineUsersAsync();
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

    public async Task ReloadOnlineUsersAsync()
    {
        try
        {
            if (_serviceProvider == null) return;

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

            var users = await db.Users
                .Where(u => u.Status == UserStatus.Online)
                .OrderBy(u => u.UserName)
                .Select(u => new UserDisplayInfo
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    Status = u.Status,
                    LastLoginTime = u.LastLoginTime
                })
                .ToListAsync();

            // 在UI线程更新集合
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                OnlineUsers.Clear();
                foreach (var user in users)
                {
                    OnlineUsers.Add(user);
                }
            });
        }
        catch (Exception ex)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SystemMessages += $"[{timestamp}] 错误: 加载在线用户失败 - {ex.Message}\n";
        }
    }

    private async Task UpdateServerStatsAsync()
    {
        try
        {
            if (_serviceProvider == null) return;

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

            var messageCount = await db.Messages.CountAsync();
            var userCount = await db.Users.CountAsync();
            var onlineCount = await db.Users.CountAsync(u => u.Status == UserStatus.Online);
            var friendshipCount = await db.Friendships.CountAsync();

            ServerStats = $"消息: {messageCount} | 用户: {userCount} | 在线: {onlineCount} | 好友关系: {friendshipCount}";
        }
        catch (Exception ex)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SystemMessages += $"[{timestamp}] 错误: 更新统计信息失败 - {ex.Message}\n";
        }
    }

    /// <summary>
    /// 根据标签页加载对应数据
    /// </summary>
    private async Task LoadTabDataAsync(string tabName)
    {
        switch (tabName)
        {
            case "在线用户":
                await ReloadOnlineUsersAsync();
                break;
            case "所有用户":
                await LoadAllUsersAsync();
                break;
            case "消息历史":
                await LoadRecentMessagesAsync();
                break;
            case "好友关系":
                await LoadFriendshipsAsync();
                break;
        }
    }

    /// <summary>
    /// 加载所有用户
    /// </summary>
    private async Task LoadAllUsersAsync()
    {
        try
        {
            if (_serviceProvider == null) return;

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

            var query = db.Users.AsQueryable();

            // 搜索过滤
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                query = query.Where(u => u.UserName.ToLower().Contains(searchLower) ||
                                        u.UserId.Contains(searchLower));
            }

            var users = await query
                .OrderBy(u => u.UserName)
                .Select(u => new UserDisplayInfo
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    Status = u.Status,
                    LastLoginTime = u.LastLoginTime
                })
                .ToListAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AllUsers.Clear();
                foreach (var user in users)
                {
                    AllUsers.Add(user);
                }
            });
        }
        catch (Exception ex)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SystemMessages += $"[{timestamp}] 错误: 加载所有用户失败 - {ex.Message}\n";
        }
    }

    /// <summary>
    /// 加载最近消息
    /// </summary>
    private async Task LoadRecentMessagesAsync()
    {
        try
        {
            if (_serviceProvider == null) return;

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

            var query = from msg in db.Messages
                        join sender in db.Users on msg.SenderId equals sender.UserId into senderGroup
                        from senderUser in senderGroup.DefaultIfEmpty()
                        join receiver in db.Users on msg.ReceiverId equals receiver.UserId into receiverGroup
                        from receiverUser in receiverGroup.DefaultIfEmpty()
                        select new MessageDisplayInfo
                        {
                            MessageId = msg.MessageId,
                            SenderId = msg.SenderId,
                            SenderName = senderUser != null ? senderUser.UserName : (msg.SenderId == "system" ? "系统" : "未知用户"),
                            ReceiverId = msg.ReceiverId,
                            ReceiverName = receiverUser != null ? receiverUser.UserName : (msg.ReceiverId == "Broadcast" ? "全体用户" : "未知用户"),
                            Content = msg.Content,
                            Timestamp = msg.Timestamp,
                            MessageType = msg.MessageType,
                            Status = msg.Status
                        };

            // 搜索过滤
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                query = query.Where(m => m.Content.ToLower().Contains(searchLower) ||
                                        m.SenderName.ToLower().Contains(searchLower) ||
                                        m.ReceiverName.ToLower().Contains(searchLower));
            }

            var messages = await query
                .OrderByDescending(m => m.Timestamp)
                .Take(100) // 只显示最近100条消息
                .ToListAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                RecentMessages.Clear();
                foreach (var message in messages)
                {
                    RecentMessages.Add(message);
                }
            });
        }
        catch (Exception ex)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SystemMessages += $"[{timestamp}] 错误: 加载消息历史失败 - {ex.Message}\n";
        }
    }

    /// <summary>
    /// 加载好友关系
    /// </summary>
    private async Task LoadFriendshipsAsync()
    {
        try
        {
            if (_serviceProvider == null) return;

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

            var query = from friendship in db.Friendships
                        join user in db.Users on friendship.UserId equals user.UserId
                        join friendUser in db.Users on friendship.FriendUserId equals friendUser.UserId
                        select new FriendshipDisplayInfo
                        {
                            UserId = friendship.UserId,
                            UserName = user.UserName,
                            FriendUserId = friendship.FriendUserId,
                            FriendUserName = friendUser.UserName,
                            CreatedAt = friendship.CreatedAt
                        };

            // 搜索过滤
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                query = query.Where(f => f.UserName.ToLower().Contains(searchLower) ||
                                        f.FriendUserName.ToLower().Contains(searchLower) ||
                                        f.UserId.Contains(searchLower) ||
                                        f.FriendUserId.Contains(searchLower));
            }

            var friendships = await query
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Friendships.Clear();
                foreach (var friendship in friendships)
                {
                    Friendships.Add(friendship);
                }
            });
        }
        catch (Exception ex)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SystemMessages += $"[{timestamp}] 错误: 加载好友关系失败 - {ex.Message}\n";
        }
    }

    // 公共方法，供外部调用以记录日志
    public void LogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        SystemMessages += $"[{timestamp}] {message}\n";
    }

    public void Dispose()
    {
        _statsTimer?.Dispose();
    }
}
