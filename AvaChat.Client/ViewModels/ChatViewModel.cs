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
            _ = LoadChatHistory();
            UpdateStatusText();
        }
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (CurrentFriend == null || string.IsNullOrWhiteSpace(MessageText)) return;

        try
        {
            IsSending = true;
            var currentUserId = App.CurrentUserId;
            var serverAddress = App.CurrentServerAddress;

            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(serverAddress))
                return;

            var api = new ApiService(serverAddress);
            var result = await api.SendMessageAsync(currentUserId, CurrentFriend.FriendUserId, MessageText);

            if (result?.Success == true)
            {
                // 创建本地消息记录
                var message = new Message
                {
                    SenderId = currentUserId,
                    ReceiverId = CurrentFriend.FriendUserId,
                    Content = MessageText,
                    Timestamp = DateTime.Now,
                    MessageType = MessageType.Text,
                    Status = MessageStatus.Delivered
                };

                Messages.Add(message);
                MessageText = string.Empty;

                // 保存到本地数据库
                var factory = new ClientDbContextFactory();
                using var db = factory.CreateDbContext([]);
                db.Messages.Add(message);
                await db.SaveChangesAsync();
            }
            else
            {
                Console.WriteLine($"发送消息失败: {result?.Error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SendMessageAsync] Exception: {ex.Message}");
        }
        finally
        {
            IsSending = false;
        }
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

        try
        {
            var currentUserId = App.CurrentUserId;
            var serverAddress = App.CurrentServerAddress;

            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(serverAddress))
                return;

            var api = new ApiService(serverAddress);
            var messages = await api.GetChatHistoryAsync(currentUserId, CurrentFriend.FriendUserId);

            if (messages != null)
            {
                Messages.Clear();
                foreach (var msg in messages)
                {
                    Messages.Add(msg);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoadChatHistory] Exception: {ex.Message}");
        }
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
