using System.Collections.ObjectModel;

namespace AvaChat.Client.ViewModels;

public partial class NotificationViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<FriendRequest> _pendingFriendRequests = [];

    [ObservableProperty]
    private ObservableCollection<Message> _systemMessages = [];

    [ObservableProperty]
    private DateTime _lastUpdateTime = DateTime.Now;

    [ObservableProperty]
    private bool _isLoading = false;

    public event EventHandler? CloseRequested;

    public NotificationViewModel()
    {
        _ = LoadNotificationsAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadNotificationsAsync();
    }

    [RelayCommand]
    private async Task AcceptFriendRequestAsync(FriendRequest request)
    {
        if (request == null) return;

        try
        {
            IsLoading = true;
            var currentUserId = App.CurrentUserId;
            var serverAddress = App.CurrentServerAddress;

            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(serverAddress))
                return;

            var api = new ApiService(serverAddress);
            var result = await api.HandleFriendRequestAsync(currentUserId, request.FromUserId, true);

            if (result?.Success == true)
            {
                // 从待处理列表中移除
                PendingFriendRequests.Remove(request);

                // 添加系统消息
                SystemMessages.Insert(0, new Message
                {
                    SenderId = "system",
                    ReceiverId = currentUserId,
                    Content = $"已接受 {request.FromUserName} 的好友申请",
                    Timestamp = DateTime.Now,
                    MessageType = MessageType.System,
                    Status = MessageStatus.Delivered
                });

                // 通知主窗口刷新好友列表
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = desktop.Windows.FirstOrDefault(w => w is MainWindow);
                    if (mainWindow?.DataContext is MainWindowViewModel mainViewModel)
                    {
                        await mainViewModel.FriendListViewModel.RefreshAsync();
                    }
                }
            }
            else
            {
                // 添加错误消息
                SystemMessages.Insert(0, new Message
                {
                    SenderId = "system",
                    ReceiverId = currentUserId,
                    Content = $"处理好友申请失败: {result?.Error}",
                    Timestamp = DateTime.Now,
                    MessageType = MessageType.System,
                    Status = MessageStatus.Delivered
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AcceptFriendRequestAsync] Exception: {ex.Message}");
            SystemMessages.Insert(0, new Message
            {
                SenderId = "system",
                ReceiverId = App.CurrentUserId ?? "unknown",
                Content = $"处理好友申请时发生错误: {ex.Message}",
                Timestamp = DateTime.Now,
                MessageType = MessageType.System,
                Status = MessageStatus.Delivered
            });
        }
        finally
        {
            IsLoading = false;
            LastUpdateTime = DateTime.Now;
        }
    }

    [RelayCommand]
    private async Task RejectFriendRequestAsync(FriendRequest request)
    {
        if (request == null) return;

        try
        {
            IsLoading = true;
            var currentUserId = App.CurrentUserId;
            var serverAddress = App.CurrentServerAddress;

            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(serverAddress))
                return;

            var api = new ApiService(serverAddress);
            var result = await api.HandleFriendRequestAsync(currentUserId, request.FromUserId, false);

            if (result?.Success == true)
            {
                // 从待处理列表中移除
                PendingFriendRequests.Remove(request);

                // 添加系统消息
                SystemMessages.Insert(0, new Message
                {
                    SenderId = "system",
                    ReceiverId = currentUserId,
                    Content = $"已拒绝 {request.FromUserName} 的好友申请",
                    Timestamp = DateTime.Now,
                    MessageType = MessageType.System,
                    Status = MessageStatus.Delivered
                });
            }
            else
            {
                // 添加错误消息
                SystemMessages.Insert(0, new Message
                {
                    SenderId = "system",
                    ReceiverId = currentUserId,
                    Content = $"处理好友申请失败: {result?.Error}",
                    Timestamp = DateTime.Now,
                    MessageType = MessageType.System,
                    Status = MessageStatus.Delivered
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RejectFriendRequestAsync] Exception: {ex.Message}");
            SystemMessages.Insert(0, new Message
            {
                SenderId = "system",
                ReceiverId = App.CurrentUserId ?? "unknown",
                Content = $"处理好友申请时发生错误: {ex.Message}",
                Timestamp = DateTime.Now,
                MessageType = MessageType.System,
                Status = MessageStatus.Delivered
            });
        }
        finally
        {
            IsLoading = false;
            LastUpdateTime = DateTime.Now;
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task LoadNotificationsAsync()
    {
        try
        {
            IsLoading = true;
            var currentUserId = App.CurrentUserId;
            var serverAddress = App.CurrentServerAddress;

            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(serverAddress))
                return;

            var api = new ApiService(serverAddress);

            // 加载待处理的好友申请
            var friendRequestsResponse = await api.GetPendingFriendRequestsAsync(currentUserId);
            if (friendRequestsResponse?.Success == true && friendRequestsResponse.Requests != null)
            {
                PendingFriendRequests.Clear();
                foreach (var request in friendRequestsResponse.Requests)
                {
                    PendingFriendRequests.Add(request);
                }
            }

            // 加载系统消息
            await LoadSystemMessagesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoadNotificationsAsync] Exception: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            LastUpdateTime = DateTime.Now;
        }
    }

    private async Task LoadSystemMessagesAsync()
    {
        try
        {
            var currentUserId = App.CurrentUserId;
            if (string.IsNullOrEmpty(currentUserId))
                return;

            // 先清理重复的系统消息
            await CleanupDuplicateSystemMessagesAsync();

            // 从本地数据库加载系统消息
            var factory = new ClientDbContextFactory();
            using var db = factory.CreateDbContext([]);

            // 查询只属于当前用户的系统消息
            var messages = await Task.Run(() => db.Messages
                .Where(m => (m.SenderId == "system" || m.MessageType == MessageType.System) &&
                           m.ReceiverId == currentUserId) // 只获取发给当前用户的系统消息
                .OrderByDescending(m => m.Timestamp)
                .Take(50)
                .ToList());

            SystemMessages.Clear();
            foreach (var message in messages)
            {
                SystemMessages.Add(message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoadSystemMessagesAsync] Exception: {ex.Message}");
        }
    }

    /// <summary>
    /// 添加系统消息
    /// </summary>
    public void AddSystemMessage(string content)
    {
        var message = new Message
        {
            SenderId = "system",
            ReceiverId = App.CurrentUserId ?? "unknown",
            Content = content,
            Timestamp = DateTime.Now,
            MessageType = MessageType.System,
            Status = MessageStatus.Delivered
        };

        SystemMessages.Insert(0, message);
        LastUpdateTime = DateTime.Now;

        // 保存到本地数据库
        Task.Run(() =>
        {
            try
            {
                var factory = new ClientDbContextFactory();
                using var db = factory.CreateDbContext([]);
                db.Messages.Add(message);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddSystemMessage] 保存消息失败: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// 检查是否有新通知
    /// </summary>
    public bool HasNewNotifications => PendingFriendRequests.Count > 0;

    /// <summary>
    /// 刷新系统消息
    /// </summary>
    public async Task RefreshSystemMessagesAsync()
    {
        await LoadSystemMessagesAsync();
    }

    /// <summary>
    /// 清理重复的系统消息（基于内容和时间的相似性）
    /// </summary>
    private async Task CleanupDuplicateSystemMessagesAsync()
    {
        try
        {
            var currentUserId = App.CurrentUserId;
            if (string.IsNullOrEmpty(currentUserId))
                return;

            var factory = new ClientDbContextFactory();
            using var db = factory.CreateDbContext([]);

            // 获取当前用户的所有系统消息
            var allMessages = await Task.Run(() => db.Messages
                .Where(m => (m.SenderId == "system" || m.MessageType == MessageType.System) &&
                           m.ReceiverId == currentUserId)
                .OrderByDescending(m => m.Timestamp)
                .ToList());

            var messagesToDelete = new List<Message>();
            var seenMessages = new HashSet<string>();

            foreach (var message in allMessages)
            {
                // 创建消息的唯一标识（基于内容和大致时间）
                var messageKey = $"{message.Content}_{message.Timestamp:yyyy-MM-dd-HH-mm}";

                if (seenMessages.Contains(messageKey))
                {
                    // 发现重复消息，标记为删除
                    messagesToDelete.Add(message);
                }
                else
                {
                    seenMessages.Add(messageKey);
                }
            }

            // 删除重复的消息
            if (messagesToDelete.Count > 0)
            {
                await Task.Run(() =>
                {
                    db.Messages.RemoveRange(messagesToDelete);
                    db.SaveChanges();
                });

                Console.WriteLine($"[CleanupDuplicateSystemMessagesAsync] 删除了 {messagesToDelete.Count} 条重复的系统消息");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CleanupDuplicateSystemMessagesAsync] Exception: {ex.Message}");
        }
    }
}
