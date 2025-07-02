using System.Collections.ObjectModel;

namespace AvaChat.Client.ViewModels;

public partial class AddFriendDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isSearching = false;

    [ObservableProperty]
    private ObservableCollection<UserInfo> _searchResults = [];

    public event EventHandler? CloseRequested;

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            SearchResults.Clear();
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SearchResults.Clear();
            return;
        }

        try
        {
            IsSearching = true;
            SearchResults.Clear();

            var currentUserId = App.CurrentUserId;
            var serverAddress = App.CurrentServerAddress;
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(serverAddress))
            {
                return;
            }

            var api = new ApiService(serverAddress);
            var users = await api.SearchUsersAsync(SearchText, currentUserId);

            if (users != null)
            {
                foreach (var user in users)
                {
                    SearchResults.Add(user);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SearchAsync] Exception: {ex.Message}");
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task AddFriendAsync(UserInfo user)
    {
        try
        {
            var currentUserId = App.CurrentUserId;
            var serverAddress = App.CurrentServerAddress;
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(serverAddress))
            {
                return;
            }

            var api = new ApiService(serverAddress);
            var result = await api.SendFriendRequestAsync(currentUserId, user.UserId, $"我是 {currentUserId}，想要添加您为好友");

            if (result?.Success == true)
            {
                // TODO: 显示成功消息
                Console.WriteLine($"好友申请已发送给 {user.UserName}");

                // 从搜索结果中移除
                SearchResults.Remove(user);
            }
            else
            {
                // TODO: 显示错误消息
                Console.WriteLine($"发送好友申请失败: {result?.Error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AddFriendAsync] Exception: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
