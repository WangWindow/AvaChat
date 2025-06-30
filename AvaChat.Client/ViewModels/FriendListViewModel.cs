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
        _ = LoadFriendsAsync();
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
    private void AddFriend()
    {
        // TODO: 打开添加好友对话框
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
    private async Task RefreshAsync()
    {
        await LoadFriendsAsync();
    }

    private async Task LoadFriendsAsync()
    {
        try
        {
            IsLoading = true;
            var factory = new ClientDbContextFactory();
            using var db = factory.CreateDbContext([]);
            // 获取当前用户Id
            var userId = App.CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                Friends = [];
                FilteredFriends = [];
                return;
            }
            // 加载所有好友关系（含System）
            var friends = db.Friendships
                .Where(f => f.UserId == userId)
                .OrderBy(f => f.FriendUserId)
                .ToList();
            Friends = new ObservableCollection<Friendship>(friends);
            FilterFriends();
        }
        catch (Exception)
        {
            // 可根据需要弹窗或日志
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
                || (f.AlterName?.ToLowerInvariant().Contains(lower, StringComparison.InvariantCultureIgnoreCase) ?? false)
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
}
