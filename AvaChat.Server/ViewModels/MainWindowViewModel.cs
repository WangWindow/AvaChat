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

    // 示例：初始化一些在线用户
    public MainWindowViewModel()
    {
        // TODO: 实际应从服务器获取在线用户
        OnlineUsers.Add("10000001");
        OnlineUsers.Add("10000002");
        OnlineUsers.Add("System");
    }
}
