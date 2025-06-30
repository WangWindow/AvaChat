namespace AvaChat.Server.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
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

    // 发送系统消息命令
    [RelayCommand]
    private void SendSystemMessage()
    {
        if (!string.IsNullOrWhiteSpace(MessageToSend))
        {
            // 追加到系统消息区
            SystemMessages += $"[系统消息] {MessageToSend}\n";
            // TODO: 广播到所有客户端
            MessageToSend = string.Empty;
        }
    }

    public MainWindowViewModel()
    {
        // 订阅在线用户变更事件
        AuthController.OnlineUsersChanged += ReloadOnlineUsers;

        // 初始化在线用户列表
        ReloadOnlineUsers();
    }

    public void ReloadOnlineUsers()
    {
        try
        {
            using var db = new ServerDbContext(new DbContextOptions<ServerDbContext>());
            var online = db.Users
                .Where(u => u.Status == UserStatus.Online)
                .Select(u => u.UserId)
                .ToList();
            OnlineUsers = new ObservableCollection<string>(online);
        }
        catch (Exception ex)
        {
            SystemMessages += $"[错误] 加载在线用户失败: {ex.Message}\n";
        }
    }
}
