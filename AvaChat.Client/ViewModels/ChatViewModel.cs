namespace AvaChat.Client.ViewModels;

using AvaChat.Shared.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;

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

    // 滚动到底部事件
    public event EventHandler? ScrollToBottomRequested;

    [ObservableProperty]
    private bool _autoScrollToBottom = true;

    public ChatViewModel()
    {
        // 设置SignalR消息处理
        SetupSignalRHandlers();
    }

    /// <summary>
    /// 设置SignalR消息处理器
    /// </summary>
    private void SetupSignalRHandlers()
    {
        // 应用启动后，如果SignalRClient已创建，就注册处理器
        RegisterSignalRHandlers(App.SignalRClient);

        // 订阅SignalR客户端变化事件
        WeakReferenceMessenger.Default.Register<SignalRClient>(this, (r, client) =>
        {
            RegisterSignalRHandlers(client);
        });
    }

    /// <summary>
    /// 注册SignalR消息处理器
    /// </summary>
    private void RegisterSignalRHandlers(SignalRClient? signalRClient)
    {
        if (signalRClient == null) return;

        // 接收消息处理器
        signalRClient.OnMessageReceived += (sender, message) =>
        {
            // 如果是当前聊天的消息，就添加到消息列表
            if (CurrentFriend != null &&
                (message.SenderId == CurrentFriend.FriendUserId || message.ReceiverId == CurrentFriend.FriendUserId))
            {
                // 需要在UI线程上操作集合
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // 检查消息是否已存在，避免重复添加
                    var exists = Messages.Any(m => m.MessageId == message.MessageId);
                    if (!exists)
                    {
                        Messages.Add(message);
                        Console.WriteLine($"[ChatViewModel] 添加收到的消息到UI: {message.MessageId}, 发送者: {message.SenderId}");

                        // 触发滚动到底部
                        if (AutoScrollToBottom)
                        {
                            ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[ChatViewModel] 消息已存在，跳过: {message.MessageId}");
                    }

                    // 自动滚动到底部
                    if (AutoScrollToBottom)
                    {
                        ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
                    }
                });
            }
        };

        // 消息发送成功回调 - 只处理自己发送的消息的立即显示
        signalRClient.OnMessageSent += (sender, message) =>
        {
            // 需要在UI线程上操作集合
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                // 检查是否是当前聊天的消息且是自己发送的
                if (CurrentFriend != null && message.ReceiverId == CurrentFriend.FriendUserId)
                {
                    // 检查消息是否已存在，避免重复添加
                    var exists = Messages.Any(m => m.MessageId == message.MessageId);
                    if (!exists)
                    {
                        Messages.Add(message);
                        Console.WriteLine($"[ChatViewModel] 立即显示发送的消息: {message.MessageId}");

                        // 触发滚动到底部
                        if (AutoScrollToBottom)
                        {
                            ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[ChatViewModel] 发送的消息已存在: {message.MessageId}");
                    }
                }
            });
        };

        // 接收错误信息
        signalRClient.OnErrorReceived += (sender, error) =>
        {
            Console.WriteLine($"[SignalR错误] {error}");
        };

        // 好友状态变更
        signalRClient.OnFriendStatusChanged += (sender, info) =>
        {
            if (CurrentFriend != null && CurrentFriend.FriendUserId == info.UserId)
            {
                // 更新当前聊天好友状态
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (CurrentFriend.FriendUser != null)
                    {
                        CurrentFriend.FriendUser.Status = info.Status;
                        UpdateStatusText();
                    }
                });
            }
        };
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
            var signalRClient = App.SignalRClient;

            if (string.IsNullOrEmpty(currentUserId) || signalRClient == null || !signalRClient.IsConnected)
            {
                Console.WriteLine("无法发送消息：SignalR未连接或用户未登录");
                return;
            }

            // 暂存消息文本，因为发送后会清空输入框
            var messageText = MessageText;
            MessageText = string.Empty;

            // 通过SignalR发送消息
            bool success = await signalRClient.SendPrivateMessageAsync(CurrentFriend.FriendUserId, messageText);

            if (!success)
            {
                Console.WriteLine("通过SignalR发送消息失败");
                MessageText = messageText; // 恢复消息文本
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SendMessageAsync] 异常: {ex.Message}");
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
        _ = LoadChatHistory();
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

                // 加载完历史消息后滚动到底部
                if (Messages.Count > 0)
                {
                    await Task.Delay(100); // 等待UI更新
                    ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
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

        StatusText = CurrentFriend.FriendUser?.Status switch
        {
            UserStatus.Online => "在线",
            UserStatus.Offline => "离线",
            null => "未知状态",
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
