namespace AvaChat.Client.ViewModels;

public partial class FriendListViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Friendship> _friends = [];

    [ObservableProperty]
    private ObservableCollection<Friendship> _filteredFriends = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private Friendship? _selectedFriend;

    [ObservableProperty]
    private bool _isLoading = false;

    public FriendListViewModel()
    {
        FilteredFriends = new ObservableCollection<Friendship>(Friends);

        // 只有当用户已登录时才加载好友列表
        if (!string.IsNullOrEmpty(App.CurrentUserId))
        {
            _ = LoadFriendsAsync();
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterFriends();
    }

    partial void OnSelectedFriendChanged(Friendship? value)
    {
        // TODO
    }

    [RelayCommand]
    private async Task AddFriend()
    {
        try
        {
            var dialog = new AddFriendDialog();
            var viewModel = (AddFriendDialogViewModel)dialog.DataContext!;

            // 订阅关闭事件
            viewModel.CloseRequested += (s, e) => dialog.Close();

            // 获取当前主窗口作为父窗口
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                await dialog.ShowDialog<bool?>(desktop.MainWindow);
            }
            else
            {
                dialog.Show();
            }

            // 对话框关闭后刷新好友列表
            await LoadFriendsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AddFriend] Exception: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RemoveFriend(Friendship? friend)
    {
        if (friend == null)
            return;

        // TODO: 实现删除好友功能
    }

    [RelayCommand]
    private void SendMessage(Friendship? friend)
    {
        if (friend == null)
            return;

        SelectedFriend = friend;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await LoadFriendsAsync();
    }

    private async Task LoadFriendsAsync()
    {
        try
        {
            IsLoading = true;
            var userId = App.CurrentUserId;
            var serverAddress = App.CurrentServerAddress;

            if (string.IsNullOrEmpty(userId))
            {
                Friends = [];
                FilteredFriends = [];
                IsLoading = false;
                return;
            }

            // 先尝试从服务器同步好友数据
            if (!string.IsNullOrEmpty(serverAddress))
            {
                try
                {
                    var api = new ApiService(serverAddress);
                    var serverFriends = await api.GetFriendshipsAsync(userId);
                    if (serverFriends != null)
                    {
                        // 数据已在ApiService中同步到本地数据库
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LoadFriendsAsync] Failed to sync from server: {ex.Message}");
                }
            }

            // 从本地数据库加载好友列表
            var factory = new ClientDbContextFactory();
            using var db = factory.CreateDbContext([]);
            var friends = await Task.Run(() => db.Friendships
                .Include(f => f.FriendUser)
                .Where(f => f.UserId == userId)
                .OrderBy(f => f.FriendUserId)
                .ToList());

            Friends = new ObservableCollection<Friendship>(friends);
            FilterFriends();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoadFriendsAsync] Exception: {ex.Message}");
            Friends = [];
            FilteredFriends = [];
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void FilterFriends()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredFriends = new ObservableCollection<Friendship>(Friends);
        }
        else
        {
            var lower = SearchText.ToLowerInvariant();
            var filtered = Friends.Where(f =>
                (f.FriendUser.UserName?.ToLowerInvariant().Contains(lower, StringComparison.InvariantCultureIgnoreCase) ?? false)
                || (f.FriendUserId?.Contains(lower) ?? false)
            ).ToList();
            FilteredFriends = new ObservableCollection<Friendship>(filtered);
        }
    }

    /// <summary>
    /// 更新好友在线状态
    /// </summary>
    public void UpdateFriendStatus(string userId, UserStatus status)
    {
        var friend = Friends.FirstOrDefault(f => f.UserId == userId);
        if (friend != null)
        {
            friend.FriendUser.Status = status;
            FilterFriends();
        }
    }

    /// <summary>
    /// 刷新指定好友状态
    /// </summary>
    public async Task RefreshFriendStatusAsync(string userId, UserStatus status)
    {
        // 查找好友
        var friend = Friends.FirstOrDefault(f => f.FriendUserId == userId);
        if (friend != null)
        {
            // 更新状态
            friend.FriendUser.Status = status;

            // 如果是上线状态，更新最后登录时间
            if (status == UserStatus.Online)
            {
                friend.FriendUser.LastLoginTime = DateTime.Now;
            }

            // 刷新过滤列表
            FilterFriends();
        }
        else
        {
            // 如果找不到好友（可能是新添加的），刷新整个列表
            await LoadFriendsAsync();
        }
    }

    /// <summary>
    /// 初始化好友列表（用于登录后调用）
    /// </summary>
    public async Task InitializeAsync()
    {
        if (!string.IsNullOrEmpty(App.CurrentUserId))
        {
            await LoadFriendsAsync();
        }
    }
}
