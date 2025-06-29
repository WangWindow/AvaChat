namespace AvaChat.Client.ViewModels;

using AvaChat.Shared.Models;

public partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty]
    private Friendship? _currentFriend;

    [ObservableProperty]
    private ObservableCollection<Message> _messages = [];

    [ObservableProperty]
    private string _messageText = string.Empty;

    [ObservableProperty]
    private bool _isConnected = false;

    [ObservableProperty]
    private bool _isSending = false;

    [ObservableProperty]
    private string _statusText = "未连接";

    public ChatViewModel()
    {
    }

    partial void OnCurrentFriendChanged(Friendship? value)
    {
        if (value != null)
        {
            _ = LoadChatHistory(); // Fire and forget
            UpdateStatusText();
        }
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        // TODO
    }

    [RelayCommand]
    private void ClearHistory()
    {
        Messages.Clear();
    }

    /// <summary>
    /// 加载与指定好友的聊天
    /// </summary>
    public void LoadChatWithFriend(Friendship friend)
    {
        CurrentFriend = friend;
    }

    private async Task LoadChatHistory()
    {
        if (CurrentFriend == null) return;
        // TODO: 从数据库或服务器加载聊天历史

    }

    private void UpdateStatusText()
    {
        if (CurrentFriend == null)
        {
            StatusText = "未选择好友";
            return;
        }

        StatusText = CurrentFriend.FriendUser.Status switch
        {
            UserStatus.Online => "在线",
            UserStatus.Offline => "离线",
            _ => "未知状态"
        };
    }

    /// <summary>
    /// 接收新消息
    /// </summary>
    public void ReceiveNewMessage(Message message)
    {
        Messages.Add(message);

        // TODO
    }
}
