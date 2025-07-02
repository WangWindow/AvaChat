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

        // 初始化好友列表
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
    }

    private void SetupMessaging()
    {
        // TODO

    }

    /// <summary>
    /// 初始化主窗口（加载好友列表等）
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            await FriendListViewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainWindowViewModel.InitializeAsync] Exception: {ex.Message}");
        }
    }
}
