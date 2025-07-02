using AvaChat.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AvaChat.Server.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly ServerDbContext _db;
    private readonly Timer _statsTimer;
    private readonly IHubContext<ChatHub>? _hubContext;

    // 在线用户列表
    [ObservableProperty]
    private ObservableCollection<string> _onlineUsers = [];

    // 当前选中的用户
    [ObservableProperty]
    private string? _selectedUser;

    // 系统消息内容（多行文本）
    [ObservableProperty]
    private string _systemMessages = string.Empty;

    // 待发送的系统消息
    [ObservableProperty]
    private string _messageToSend = string.Empty;

    // 服务器状态信息
    [ObservableProperty]
    private string _serverStats = "消息: 0 | 用户: 0";

    // 发送系统消息命令
    [RelayCommand]
    private async Task SendSystemMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageToSend))
            return;

        try
        {
            var timestamp = DateTime.Now;
            var formattedTime = timestamp.ToString("yyyy-MM-dd HH:mm:ss");

            // 追加到系统消息区
            SystemMessages += $"[{formattedTime}] 广播: {MessageToSend}\n";

            // 使用ChatHub的静态方法发送系统广播
            if (_hubContext != null)
            {
                await ChatHub.SendServerBroadcast(_hubContext, _db, MessageToSend);
                MessageToSend = string.Empty;

                // 更新统计信息
                UpdateServerStats();
            }
            else
            {
                SystemMessages += "[错误] 无法获取SignalR上下文，消息未发送\n";
            }
        }
        catch (Exception ex)
        {
            SystemMessages += $"[错误] 发送系统消息失败: {ex.Message}\n";
        }
    }

    public MainWindowViewModel()
    {
        // 创建数据库上下文
        _db = new ServerDbContext(new DbContextOptions<ServerDbContext>());

        // 尝试获取SignalR Hub上下文
        try
        {
            // 在实际应用中，应通过依赖注入获取
            var serviceProvider = Program.GetServiceProvider();
            if (serviceProvider != null)
            {
                _hubContext = serviceProvider.GetService<IHubContext<ChatHub>>();
            }
        }
        catch (Exception ex)
        {
            SystemMessages += $"[错误] 获取Hub上下文失败: {ex.Message}\n";
        }

        // 订阅在线用户变更事件
        AuthController.OnlineUsersChanged += ReloadOnlineUsers;

        // 添加启动消息
        var startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        SystemMessages += $"[{startTime}] 服务器已启动\n";

        // 初始化在线用户列表
        ReloadOnlineUsers();

        // 设置定时更新服务器统计信息（每30秒更新一次）
        _statsTimer = new Timer(_ =>
        {
            UpdateServerStats();
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    public void ReloadOnlineUsers()
    {
        try
        {
            var online = _db.Users
                .Where(u => u.Status == UserStatus.Online)
                .Select(u => $"{u.UserName} ({u.UserId})")
                .ToList();

            OnlineUsers = new ObservableCollection<string>(online);

            // 更新统计信息
            UpdateServerStats();
        }
        catch (Exception ex)
        {
            SystemMessages += $"[错误] 加载在线用户失败: {ex.Message}\n";
        }
    }

    private void UpdateServerStats()
    {
        try
        {
            var messageCount = _db.Messages.Count();
            var userCount = _db.Users.Count();
            var onlineCount = _db.Users.Count(u => u.Status == UserStatus.Online);

            ServerStats = $"消息: {messageCount} | 用户: {userCount} | 在线: {onlineCount}";
        }
        catch (Exception ex)
        {
            SystemMessages += $"[错误] 更新统计信息失败: {ex.Message}\n";
        }
    }

    public void Dispose()
    {
        _statsTimer?.Dispose();
        _db?.Dispose();
    }
}
