using System.Collections.ObjectModel;

namespace AvaChat.Client.ViewModels;

public partial class NotificationViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<FriendRequest> _pendingFriendRequests = [];

    [ObservableProperty]
    private ObservableCollection<SystemMessage> _systemMessages = [];

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
                SystemMessages.Insert(0, new SystemMessage
                {
                    Content = $"已接受 {request.FromUserName} 的好友申请",
                    Timestamp = DateTime.Now
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
                SystemMessages.Insert(0, new SystemMessage
                {
                    Content = $"处理好友申请失败: {result?.Error}",
                    Timestamp = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AcceptFriendRequestAsync] Exception: {ex.Message}");
            SystemMessages.Insert(0, new SystemMessage
            {
                Content = $"处理好友申请时发生错误: {ex.Message}",
                Timestamp = DateTime.Now
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
                SystemMessages.Insert(0, new SystemMessage
                {
                    Content = $"已拒绝 {request.FromUserName} 的好友申请",
                    Timestamp = DateTime.Now
                });
            }
            else
            {
                // 添加错误消息
                SystemMessages.Insert(0, new SystemMessage
                {
                    Content = $"处理好友申请失败: {result?.Error}",
                    Timestamp = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RejectFriendRequestAsync] Exception: {ex.Message}");
            SystemMessages.Insert(0, new SystemMessage
            {
                Content = $"处理好友申请时发生错误: {ex.Message}",
                Timestamp = DateTime.Now
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

            // TODO: 加载系统消息（从本地数据库或服务器）
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
            // 从本地数据库加载系统消息
            var factory = new ClientDbContextFactory();
            using var db = factory.CreateDbContext([]);

            var systemUserId = "00000000"; // System用户的固定ID
            var messages = await Task.Run(() => db.Messages
                .Where(m => m.SenderId == systemUserId || m.ReceiverId == systemUserId)
                .OrderByDescending(m => m.Timestamp)
                .Take(50)
                .ToList());

            SystemMessages.Clear();
            foreach (var message in messages)
            {
                SystemMessages.Add(new SystemMessage
                {
                    Content = message.Content,
                    Timestamp = message.Timestamp
                });
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
        SystemMessages.Insert(0, new SystemMessage
        {
            Content = content,
            Timestamp = DateTime.Now
        });
        LastUpdateTime = DateTime.Now;
    }

    /// <summary>
    /// 检查是否有新通知
    /// </summary>
    public bool HasNewNotifications => PendingFriendRequests.Count > 0;
}

/// <summary>
/// 系统消息模型
/// </summary>
public class SystemMessage
{
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
