using System.ComponentModel;
using AvaChat.Client.Models;
using AvaChat.Shared.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;

namespace AvaChat.Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// 好友列表视图模型
    /// </summary>
    public FriendListViewModel FriendListViewModel { get; }

    /// <summary>
    /// 聊天视图模型
    /// </summary>
    public ChatViewModel ChatViewModel { get; }

    [ObservableProperty]
    private string _currentUserName = string.Empty;

    [ObservableProperty]
    private string _userStatus = "离线";

    [ObservableProperty]
    private bool _hasNewNotifications = false;

    [ObservableProperty]
    private NotificationViewModel? _notificationViewModel;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public MainWindowViewModel()
    {
        FriendListViewModel = new FriendListViewModel();
        ChatViewModel = new ChatViewModel();

        // 设置好友选择事件
        SetupMessaging();

        // 初始化
        _ = InitializeAsync();
    }

    public MainWindowViewModel(
        FriendListViewModel friendListViewModel,
        ChatViewModel chatViewModel)
    {
        FriendListViewModel = friendListViewModel;
        ChatViewModel = chatViewModel;

        // 设置消息传递
        SetupMessaging();

        // 初始化
        _ = InitializeAsync();
    }

    private void SetupMessaging()
    {
        // 监听好友选择事件
        FriendListViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(FriendListViewModel.SelectedFriend) &&
                FriendListViewModel.SelectedFriend != null)
            {
                ChatViewModel.LoadChatWithFriend(FriendListViewModel.SelectedFriend);
            }
        };

        // 监听SignalR客户端事件
        SetupSignalRHandlers();

        // 订阅SignalR客户端变化事件
        WeakReferenceMessenger.Default.Register<SignalRClient>(this, (r, client) =>
        {
            SetupSignalRHandlers(client);
        });
    }

    /// <summary>
    /// 设置SignalR消息处理器
    /// </summary>
    private void SetupSignalRHandlers()
    {
        // 获取当前SignalR客户端
        var signalRClient = App.SignalRClient;
        if (signalRClient != null)
        {
            SetupSignalRHandlers(signalRClient);
        }
    }

    /// <summary>
    /// 设置指定SignalR客户端的消息处理器
    /// </summary>
    private void SetupSignalRHandlers(SignalRClient signalRClient)
    {
        // 处理系统消息
        signalRClient.OnSystemMessageReceived += (sender, message) =>
        {
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                // 显示系统消息通知
                HasNewNotifications = true;

                // 如果当前显示的是系统消息对话，则刷新消息列表
                if (NotificationViewModel != null)
                {
                    await NotificationViewModel.RefreshSystemMessagesAsync();
                }
            });
        };

        // 好友状态变更
        signalRClient.OnFriendStatusChanged += (sender, info) =>
        {
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                // 刷新好友列表中的好友状态
                await FriendListViewModel.RefreshFriendStatusAsync(info.UserId, info.Status);
            });
        };

        // 监听通知更新 - 使用可取消的后台任务
        _ = Task.Run(async () =>
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await CheckNotificationsAsync();
                    await Task.Delay(30000, _cancellationTokenSource.Token); // 每30秒检查一次
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，不需要处理
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SetupMessaging] Background task exception: {ex.Message}");
            }
        }, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// 初始化主窗口（加载好友列表等）
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            // 设置当前用户信息
            CurrentUserName = App.CurrentUserName ?? App.CurrentUserId ?? "未登录";
            UserStatus = string.IsNullOrEmpty(App.CurrentUserId) ? "离线" : "在线";

            // 初始化好友列表
            await FriendListViewModel.InitializeAsync();

            // 初始化通知
            await CheckNotificationsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainWindowViewModel.InitializeAsync] Exception: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ShowNotificationsAsync()
    {
        try
        {
            var window = new NotificationWindow();
            var viewModel = new NotificationViewModel();
            window.DataContext = viewModel;

            // 订阅关闭事件
            viewModel.CloseRequested += (s, e) => window.Close();

            // 获取当前主窗口作为父窗口
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                await window.ShowDialog<bool?>(desktop.MainWindow);
            }
            else
            {
                window.Show();
            }

            // 刷新通知状态
            await CheckNotificationsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShowNotificationsAsync] Exception: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowSettings()
    {
        try
        {
            // TODO: 实现设置窗口
            Console.WriteLine("显示设置窗口");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShowSettings] Exception: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查是否有新通知
    /// </summary>
    private async Task CheckNotificationsAsync()
    {
        try
        {
            var currentUserId = App.CurrentUserId;
            var serverAddress = App.CurrentServerAddress;

            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(serverAddress))
            {
                HasNewNotifications = false;
                return;
            }

            var api = new ApiService(serverAddress);
            var response = await api.GetPendingFriendRequestsAsync(currentUserId);

            HasNewNotifications = response?.Success == true && response.Requests?.Count > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CheckNotificationsAsync] Exception: {ex.Message}");
            HasNewNotifications = false;
        }
    }

    /// <summary>
    /// 手动刷新通知状态
    /// </summary>
    public async Task RefreshNotificationsAsync()
    {
        await CheckNotificationsAsync();
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}
