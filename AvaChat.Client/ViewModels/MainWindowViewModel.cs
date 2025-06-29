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

    public MainWindowViewModel()
    {
        FriendListViewModel = new FriendListViewModel();
        ChatViewModel = new ChatViewModel();
    }

    public MainWindowViewModel(
        FriendListViewModel friendListViewModel,
        ChatViewModel chatViewModel)
    {
        FriendListViewModel = friendListViewModel;
        ChatViewModel = chatViewModel;

        // 设置消息传递
        SetupMessaging();
    }

    private void SetupMessaging()
    {
        // TODO

    }
}
