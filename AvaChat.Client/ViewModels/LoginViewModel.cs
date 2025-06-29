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

            // 登录成功
            await statusViewModel.ShowSuccessAsync();

            // 保存登录信息
            if (RememberCredentials)
            {
                SaveCredentials();
            }

            // 打开主窗口
            var mainWindow = new MainWindow();
            mainWindow.Show();

            // 关闭登录窗口
            CloseCurrentLoginWindow();
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
        statusViewModel.ShowRegisterPanelView();

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
        // TODO: 将从本地数据库加载保存的用户Id和密码
    }

    private void SaveCredentials()
    {
        // TODO: 将用户Id和密码保存到本地数据库
    }
}
