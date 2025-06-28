namespace AvaChat.Client.ViewModels;

public partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty]
    private FriendInfo? _currentFriend;

    [ObservableProperty]
    private ObservableCollection<MessageInfo> _messages = new();

    [ObservableProperty]
    private string _messageText = string.Empty;

    [ObservableProperty]
    private bool _isConnected = false;

    [ObservableProperty]
    private bool _isSending = false;

    [ObservableProperty]
    private string _statusText = "未连接";

    private readonly ILogger<ChatViewModel>? _logger;

    public ChatViewModel()
    {
        // 设计时构造函数
        LoadSampleData();
    }

    public ChatViewModel(ILogger<ChatViewModel> logger)
    {
        _logger = logger;
    }

    partial void OnCurrentFriendChanged(FriendInfo? value)
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
        if (string.IsNullOrWhiteSpace(MessageText) || CurrentFriend == null)
            return;

        var content = MessageText.Trim();
        MessageText = string.Empty;

        IsSending = true;

        try
        {
            // 创建新消息
            var message = new MessageInfo
            {
                MessageId = Guid.NewGuid().ToString(),
                SenderNumber = "10000001", // 当前用户号码
                ReceiverNumber = CurrentFriend.UserNumber,
                Content = content,
                MessageType = MessageType.Text,
                Status = MessageStatus.Sending,
                SentTime = DateTime.Now
            };

            // 添加到消息列表
            Messages.Add(message);

            // TODO: 发送到服务器
            await SimulateSendMessage(message);

            _logger?.LogInformation("消息发送成功: {Content}", content);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "发送消息失败: {Content}", content);

            // 更新消息状态为失败
            var lastMessage = Messages.LastOrDefault();
            if (lastMessage != null)
            {
                lastMessage.Status = MessageStatus.Failed;
            }
        }
        finally
        {
            IsSending = false;
        }
    }

    [RelayCommand]
    private void SelectEmoji()
    {
        // TODO: 打开表情选择器
        _logger?.LogInformation("用户请求选择表情");
    }

    [RelayCommand]
    private void SendFile()
    {
        // TODO: 打开文件选择器
        _logger?.LogInformation("用户请求发送文件");
    }

    [RelayCommand]
    private void ClearHistory()
    {
        Messages.Clear();
        _logger?.LogInformation("聊天记录已清空");
    }

    [RelayCommand]
    private void ViewFriendProfile()
    {
        if (CurrentFriend == null) return;

        // TODO: 打开好友资料窗口
        _logger?.LogInformation("用户请求查看好友资料: {Nickname}", CurrentFriend.Nickname);
    }

    /// <summary>
    /// 加载与指定好友的聊天
    /// </summary>
    public void LoadChatWithFriend(FriendInfo friend)
    {
        CurrentFriend = friend;
    }

    private async Task LoadChatHistory()
    {
        if (CurrentFriend == null) return;

        try
        {
            Messages.Clear();

            // TODO: 从数据库或服务器加载聊天历史
            await Task.Delay(500); // 模拟加载延迟

            // 暂时使用示例数据
            if (CurrentFriend.UserNumber == "10000002")
            {
                LoadSampleData();
            }

            _logger?.LogInformation("聊天历史加载完成，共 {Count} 条消息", Messages.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "加载聊天历史失败");
        }
    }

    private async Task SimulateSendMessage(MessageInfo message)
    {
        // 模拟网络发送延迟
        await Task.Delay(1000);

        // 更新消息状态
        message.Status = MessageStatus.Sent;
        message.DeliveredTime = DateTime.Now;

        // 模拟对方回复
        await Task.Delay(2000);

        var reply = new MessageInfo
        {
            MessageId = Guid.NewGuid().ToString(),
            SenderNumber = CurrentFriend?.UserNumber ?? "10000002",
            ReceiverNumber = "10000001",
            Content = GetAutoReply(message.Content),
            MessageType = MessageType.Text,
            Status = MessageStatus.Delivered,
            SentTime = DateTime.Now,
            DeliveredTime = DateTime.Now
        };

        Messages.Add(reply);
    }

    private string GetAutoReply(string originalMessage)
    {
        var replies = new[]
        {
            "好的，我知道了",
            "哈哈，有意思",
            "嗯嗯，没问题",
            "收到！",
            "👍",
            "我也这么觉得",
            "稍等，我看看",
            "OK"
        };

        var random = new Random();
        return replies[random.Next(replies.Length)];
    }

    private void UpdateStatusText()
    {
        if (CurrentFriend == null)
        {
            StatusText = "未选择好友";
            return;
        }

        StatusText = CurrentFriend.Status switch
        {
            UserStatus.Online => "在线",
            UserStatus.Away => "离开",
            UserStatus.Busy => "忙碌",
            UserStatus.Invisible => "隐身",
            UserStatus.Offline => $"离线 - 最后上线: {CurrentFriend.LastOnlineTime:MM/dd HH:mm}",
            _ => "未知状态"
        };
    }

    private void LoadSampleData()
    {
        var sampleMessages = new[]
        {
            new MessageInfo
            {
                MessageId = "1",
                SenderNumber = "10000002",
                ReceiverNumber = "10000001",
                Content = "你好！最近怎么样？",
                MessageType = MessageType.Text,
                Status = MessageStatus.Read,
                SentTime = DateTime.Now.AddHours(-2),
                DeliveredTime = DateTime.Now.AddHours(-2),
                ReadTime = DateTime.Now.AddHours(-2)
            },
            new MessageInfo
            {
                MessageId = "2",
                SenderNumber = "10000001",
                ReceiverNumber = "10000002",
                Content = "还不错，工作比较忙。你呢？",
                MessageType = MessageType.Text,
                Status = MessageStatus.Read,
                SentTime = DateTime.Now.AddHours(-1.5),
                DeliveredTime = DateTime.Now.AddHours(-1.5),
                ReadTime = DateTime.Now.AddHours(-1.5)
            },
            new MessageInfo
            {
                MessageId = "3",
                SenderNumber = "10000002",
                ReceiverNumber = "10000001",
                Content = "我也是，最近项目很紧张。有空一起吃饭吧！",
                MessageType = MessageType.Text,
                Status = MessageStatus.Read,
                SentTime = DateTime.Now.AddHours(-1),
                DeliveredTime = DateTime.Now.AddHours(-1),
                ReadTime = DateTime.Now.AddMinutes(-50)
            }
        };

        foreach (var message in sampleMessages)
        {
            Messages.Add(message);
        }
    }

    /// <summary>
    /// 接收新消息
    /// </summary>
    public void ReceiveMessage(MessageInfo message)
    {
        Messages.Add(message);

        // 自动标记为已读（如果聊天窗口是当前好友）
        if (CurrentFriend?.UserNumber == message.SenderNumber)
        {
            message.Status = MessageStatus.Read;
            message.ReadTime = DateTime.Now;
        }
    }
}
