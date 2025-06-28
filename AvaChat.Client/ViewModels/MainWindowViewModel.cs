namespace AvaChat.Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// 导航视图模型
    /// </summary>
    public NavigationViewModel NavigationViewModel { get; }

    /// <summary>
    /// 好友列表视图模型
    /// </summary>
    public FriendListViewModel FriendListViewModel { get; }

    /// <summary>
    /// 聊天视图模型
    /// </summary>
    public ChatViewModel ChatViewModel { get; }

    public MainWindowViewModel()
    {
        // 设计时构造函数
        NavigationViewModel = new NavigationViewModel();
        FriendListViewModel = new FriendListViewModel();
        ChatViewModel = new ChatViewModel();
    }

    public MainWindowViewModel(
        NavigationViewModel navigationViewModel,
        FriendListViewModel friendListViewModel,
        ChatViewModel chatViewModel)
    {
        NavigationViewModel = navigationViewModel;
        FriendListViewModel = friendListViewModel;
        ChatViewModel = chatViewModel;

        // 设置消息传递
        SetupMessaging();
    }

    private void SetupMessaging()
    {
        // 监听好友选择消息
        WeakReferenceMessenger.Default.Register<FriendSelectedMessage>(this, (r, m) =>
        {
            // 当选择好友时，切换聊天窗口
            ChatViewModel.LoadChatWithFriend(m.Friend);
        });

        // 监听导航切换消息
        WeakReferenceMessenger.Default.Register<NavigationChangedMessage>(this, (r, m) =>
        {
            // 根据导航状态显示不同的内容
            switch (m.NavigationType)
            {
                case NavigationType.Chat:
                    // 显示聊天界面
                    break;
                case NavigationType.Friends:
                    // 显示好友管理界面
                    break;
                case NavigationType.Settings:
                    // 显示设置界面
                    break;
            }
        });
    }
}
