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
        // TODO
    }

    private void FilterFriends()
    {
        // TODO
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
