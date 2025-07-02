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

    // 错误信息显示
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // 成功信息显示
    [ObservableProperty]
    private string _successMessage = string.Empty;

    [RelayCommand]
    private async Task AddFriendAsync(UserInfo user)
    {
        try
        {
            // 清空先前的消息
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            var currentUserId = App.CurrentUserId;
            var serverAddress = App.CurrentServerAddress;
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(serverAddress))
            {
                ErrorMessage = "未登录或服务器地址未设置";
                return;
            }

            var api = new ApiService(serverAddress);
            var result = await api.SendFriendRequestAsync(currentUserId, user.UserId, $"我是 {App.CurrentUserName}，想要添加您为好友");

            if (result?.Success == true)
            {
                SuccessMessage = $"好友申请已发送给 {user.UserName}";
                Console.WriteLine(SuccessMessage);

                // 从搜索结果中移除
                SearchResults.Remove(user);
            }
            else
            {
                ErrorMessage = $"发送好友申请失败: {result?.Error ?? "未知错误"}";
                Console.WriteLine(ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"发送好友申请时出错: {ex.Message}";
            Console.WriteLine($"[AddFriendAsync] Exception: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
