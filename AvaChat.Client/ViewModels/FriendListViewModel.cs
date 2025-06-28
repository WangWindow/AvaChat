namespace AvaChat.Client.ViewModels;

public partial class FriendListViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<FriendInfo> _friends = new();

    [ObservableProperty]
    private ObservableCollection<FriendInfo> _filteredFriends = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private FriendInfo? _selectedFriend;

    [ObservableProperty]
    private bool _isLoading = false;

    private readonly ILogger<FriendListViewModel>? _logger;

    public FriendListViewModel()
    {
        // 设计时构造函数
        LoadSampleData();
        FilteredFriends = new ObservableCollection<FriendInfo>(Friends);
    }

    public FriendListViewModel(ILogger<FriendListViewModel> logger)
    {
        _logger = logger;
        _ = LoadFriendsAsync(); // Fire and forget
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterFriends();
    }

    partial void OnSelectedFriendChanged(FriendInfo? value)
    {
        if (value != null)
        {
            // 发送好友选择消息
            WeakReferenceMessenger.Default.Send(new FriendSelectedMessage(value));
        }
    }

    [RelayCommand]
    private void AddFriend()
    {
        // TODO: 打开添加好友对话框
        _logger?.LogInformation("用户请求添加好友");
    }

    [RelayCommand]
    private void RemoveFriend(FriendInfo? friend)
    {
        if (friend == null) return;

        // TODO: 实现删除好友功能
        _logger?.LogInformation("用户请求删除好友: {Nickname}", friend.Nickname);
    }

    [RelayCommand]
    private void SendMessage(FriendInfo? friend)
    {
        if (friend == null) return;

        SelectedFriend = friend;
    }

    [RelayCommand]
    private void ViewProfile(FriendInfo? friend)
    {
        if (friend == null) return;

        // TODO: 打开好友资料窗口
        _logger?.LogInformation("用户请求查看好友资料: {Nickname}", friend.Nickname);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadFriendsAsync();
    }

    private async Task LoadFriendsAsync()
    {
        IsLoading = true;

        try
        {
            // TODO: 从服务器加载好友列表
            await Task.Delay(1000); // 模拟网络请求

            // 暂时使用示例数据
            LoadSampleData();
            FilterFriends();

            _logger?.LogInformation("好友列表加载完成，共 {Count} 个好友", Friends.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "加载好友列表失败");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void FilterFriends()
    {
        var query = Friends.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLower();
            query = query.Where(f =>
                f.Nickname.ToLower().Contains(searchLower) ||
                f.UserNumber.Contains(SearchText) ||
                (!string.IsNullOrEmpty(f.Remark) && f.Remark.ToLower().Contains(searchLower)));
        }

        // 按在线状态和昵称排序
        query = query.OrderBy(f => f.Status == UserStatus.Offline ? 1 : 0)
                     .ThenBy(f => f.Nickname);

        FilteredFriends.Clear();
        foreach (var friend in query)
        {
            FilteredFriends.Add(friend);
        }
    }

    private void LoadSampleData()
    {
        Friends.Clear();

        var sampleFriends = new[]
        {
            new FriendInfo
            {
                UserNumber = "10000002",
                Nickname = "张三",
                Signature = "忙碌中，请稍后联系",
                Status = UserStatus.Busy,
                LastOnlineTime = DateTime.Now.AddMinutes(-30),
                FriendshipStatus = FriendshipStatus.Accepted
            },
            new FriendInfo
            {
                UserNumber = "10000003",
                Nickname = "李四",
                Signature = "今天天气真不错~",
                Status = UserStatus.Online,
                LastOnlineTime = DateTime.Now,
                FriendshipStatus = FriendshipStatus.Accepted,
                Remark = "同事"
            },
            new FriendInfo
            {
                UserNumber = "10000004",
                Nickname = "王五",
                Signature = "生活就像海洋，只有意志坚强的人才能到达彼岸",
                Status = UserStatus.Away,
                LastOnlineTime = DateTime.Now.AddHours(-2),
                FriendshipStatus = FriendshipStatus.Accepted
            },
            new FriendInfo
            {
                UserNumber = "10000005",
                Nickname = "赵六",
                Signature = "",
                Status = UserStatus.Offline,
                LastOnlineTime = DateTime.Now.AddDays(-1),
                FriendshipStatus = FriendshipStatus.Accepted,
                Remark = "老同学"
            }
        };

        foreach (var friend in sampleFriends)
        {
            Friends.Add(friend);
        }
    }

    /// <summary>
    /// 更新好友在线状态
    /// </summary>
    public void UpdateFriendStatus(string userNumber, UserStatus status, DateTime lastOnlineTime)
    {
        var friend = Friends.FirstOrDefault(f => f.UserNumber == userNumber);
        if (friend != null)
        {
            friend.Status = status;
            friend.LastOnlineTime = lastOnlineTime;
            FilterFriends(); // 重新排序
        }
    }
}
