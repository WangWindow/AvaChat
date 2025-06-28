namespace AvaChat.Client.ViewModels;

public partial class NavigationViewModel : ViewModelBase
{
    [ObservableProperty]
    private NavigationType _currentNavigation = NavigationType.Chat;

    [ObservableProperty]
    private UserInfo? _currentUser;

    [ObservableProperty]
    private bool _hasNewMessages = false;

    [ObservableProperty]
    private int _unreadCount = 0;

    [ObservableProperty]
    private bool _isConnected = false;

    private readonly ILogger<NavigationViewModel>? _logger;

    public NavigationViewModel()
    {
        // 设计时构造函数
        CurrentUser = new UserInfo
        {
            UserNumber = "10000001",
            Nickname = "演示用户",
            Signature = "这是一个演示签名",
            Status = UserStatus.Online
        };
    }

    public NavigationViewModel(ILogger<NavigationViewModel> logger)
    {
        _logger = logger;
    }

    [RelayCommand]
    private void SwitchToChat()
    {
        CurrentNavigation = NavigationType.Chat;
        WeakReferenceMessenger.Default.Send(new NavigationChangedMessage(NavigationType.Chat));
    }

    [RelayCommand]
    private void SwitchToFriends()
    {
        CurrentNavigation = NavigationType.Friends;
        WeakReferenceMessenger.Default.Send(new NavigationChangedMessage(NavigationType.Friends));
    }

    [RelayCommand]
    private void SwitchToSettings()
    {
        CurrentNavigation = NavigationType.Settings;
        WeakReferenceMessenger.Default.Send(new NavigationChangedMessage(NavigationType.Settings));
    }

    [RelayCommand]
    private void ChangeStatus()
    {
        // TODO: 实现状态切换功能
        _logger?.LogInformation("用户请求切换状态");
    }

    [RelayCommand]
    private void ShowProfile()
    {
        // TODO: 打开个人资料窗口
        _logger?.LogInformation("用户请求查看个人资料");
    }

    /// <summary>
    /// 设置当前用户信息
    /// </summary>
    public void SetCurrentUser(UserInfo user)
    {
        CurrentUser = user;
    }

    /// <summary>
    /// 更新未读消息数量
    /// </summary>
    public void UpdateUnreadCount(int count)
    {
        UnreadCount = count;
        HasNewMessages = count > 0;
    }
}
