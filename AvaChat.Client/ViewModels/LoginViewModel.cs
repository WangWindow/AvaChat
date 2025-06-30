
using AvaChat.Client.Models;
namespace AvaChat.Client.ViewModels;


public partial class LoginViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _userId = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberCredentials = false;

    [ObservableProperty]
    private string _serverAddress = "localhost:5000";

    [ObservableProperty]
    private bool _isPasswordVisible = false;

    [ObservableProperty]
    private char _passwordChar = '●';

    [ObservableProperty]
    private string _passwordVisibilityIcon = "👁";

    [ObservableProperty]
    private string _passwordVisibilityTooltip = "显示密码";

    /// <summary>
    /// 所有已保存账号Id列表（用于下拉选择）
    /// </summary>
    [ObservableProperty]
    private List<string> _userIdList = new();

    /// <summary>
    /// 控制账号下拉列表显示
    /// </summary>
    [ObservableProperty]
    private bool _isUserIdListOpen = false;

    public LoginViewModel()
    {
        LoadSavedCredentials();
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(Password))
        {
            ShowErrorDialog("登录失败", "请输入用户Id和密码");
            return;
        }

        if (UserId.Length != 8)
        {
            ShowErrorDialog("登录失败", "用户Id必须是8位数字");
            return;
        }

        // 显示登录状态窗口
        var statusWindow = new LoginStatusWindow();
        var statusViewModel = new LoginStatusViewModel();
        statusWindow.DataContext = statusViewModel;
        statusViewModel.ServerAddress = ServerAddress;

        // 获取当前登录窗口作为父窗口
        var currentWindow = GetCurrentLoginWindow();
        if (currentWindow != null)
        {
            statusWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        statusWindow.Show();

        // 订阅事件
        statusViewModel.WindowCloseRequested += (s, e) => statusWindow.Close();
        statusViewModel.RetryRequested += async (s, e) =>
        {
            statusWindow.Close();
            await Task.Delay(100);
            await LoginAsync();
        };

        try
        {
            // 显示连接状态
            statusViewModel.ShowConnecting();

            // 调用API
            var api = new ApiService(ServerAddress);
            var resp = await api.LoginAsync(UserId, Password) ?? throw new Exception("无法连接服务器");
            if (resp.Success)
            {
                await statusViewModel.ShowSuccessAsync();

                // 保存登录信息
                SaveCredentials(RememberCredentials);

                // 登录成功后进行数据同步
                bool syncSuccess = true;
                try
                {
                    // 用户信息同步
                    var userData = await api.GetUserDataAsync(UserId);
                    // 好友关系同步
                    var friends = await api.GetFriendshipsAsync(UserId);
                }
                catch
                {
                    // 同步失败，记录但不阻断登录
                    syncSuccess = false;
                }

                // 设置全局登录状态，刷新托盘菜单
                App.OnUserLogin(UserId, ServerAddress);

                // 打开主窗口
                var mainWindow = new MainWindow();
                mainWindow.Show();

                // 关闭登录窗口
                CloseCurrentLoginWindow();

                // 如同步失败可弹窗提示（可选）
                if (!syncSuccess)
                {
                    statusViewModel.ShowError("同步失败", "部分数据未能从云端同步，已使用本地缓存。");
                }
            }
            else
            {
                statusViewModel.ShowError("登录失败", resp.Error ?? "登录失败");
            }
        }
        catch (Exception ex)
        {
            statusViewModel.ShowError("登录失败", ex.Message);
        }
    }

    [RelayCommand]
    private void Register()
    {
        var statusWindow = new LoginStatusWindow();
        var statusViewModel = new LoginStatusViewModel();
        statusWindow.DataContext = statusViewModel;
        statusWindow.Title = "注册新账号";
        statusViewModel.ServerAddress = ServerAddress;
        statusViewModel.ShowRegisterPanelView();

        // 注册成功后自动填充UserId
        statusViewModel.RegisterSuccess += (s, userId) =>
        {
            UserId = userId;
        };

        // 订阅关闭事件
        statusViewModel.WindowCloseRequested += (s, e) => statusWindow.Close();

        statusWindow.Show();
    }

    [RelayCommand]
    private void ServerSettings()
    {
        var statusWindow = new LoginStatusWindow();
        var statusViewModel = new LoginStatusViewModel();
        statusWindow.DataContext = statusViewModel;
        statusWindow.Title = "服务器设置";
        statusViewModel.ShowServerSettingsPanelView();

        // 订阅关闭事件
        statusViewModel.WindowCloseRequested += (s, e) => statusWindow.Close();

        statusWindow.Show();
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
        PasswordChar = IsPasswordVisible ? '\0' : '●';
        PasswordVisibilityIcon = IsPasswordVisible ? "🙈" : "👁";
        PasswordVisibilityTooltip = IsPasswordVisible ? "隐藏密码" : "显示密码";
    }

    private void ShowErrorDialog(string title, string message)
    {
        var statusWindow = new LoginStatusWindow();
        var statusViewModel = new LoginStatusViewModel();
        statusWindow.DataContext = statusViewModel;
        statusWindow.Title = title;

        // 订阅关闭事件
        statusViewModel.WindowCloseRequested += (s, e) => statusWindow.Close();

        statusWindow.Show();
        statusViewModel.ShowError(title, message);
    }

    private LoginWindow? GetCurrentLoginWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.Windows.OfType<LoginWindow>().FirstOrDefault();
        }
        return null;
    }

    private void CloseCurrentLoginWindow()
    {
        var loginWindow = GetCurrentLoginWindow();
        loginWindow?.Close();
    }


    private void LoadSavedCredentials()
    {
        try
        {
            var factory = new ClientDbContextFactory();
            using var db = factory.CreateDbContext([]);
            // 获取所有已保存账号Id，按Id升序
            UserIdList = db.LoginInfos
                .Select(x => x.UserId)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // 获取最近一次登录的账号
            var info = db.LoginInfos.OrderByDescending(x => x.LoginTime).FirstOrDefault();
            if (info != null)
            {
                UserId = info.UserId;
                Password = info.Password ?? string.Empty;
            }
        }
        catch { /* 忽略异常 */ }
    }

    private void SaveCredentials(bool isSavePassword)
    {
        try
        {
            var factory = new ClientDbContextFactory();
            using var db = factory.CreateDbContext([]);
            // 先删除已有相同UserId的记录
            var old = db.LoginInfos.Where(x => x.UserId == UserId).ToList();
            if (old.Count > 0)
            {
                db.LoginInfos.RemoveRange(old);
            }
            var info = new LoginInfo
            {
                UserId = UserId,
                Password = isSavePassword ? Password : null,
                LoginTime = DateTime.Now
            };
            db.LoginInfos.Add(info);
            db.SaveChanges();
        }
        catch { /* 忽略异常 */ }
    }
}
