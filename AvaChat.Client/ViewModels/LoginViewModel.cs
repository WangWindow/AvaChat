using System;
using System.Linq;
using System.Threading.Tasks;
using AvaChat.Client.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AvaChat.Client.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _userNumber = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberCredentials = false;

    [ObservableProperty]
    private string _serverAddress = "localhost:8080";

    [ObservableProperty]
    private bool _isPasswordVisible = false;

    [ObservableProperty]
    private char _passwordChar = '●';

    [ObservableProperty]
    private string _passwordVisibilityIcon = "👁";

    [ObservableProperty]
    private string _passwordVisibilityTooltip = "显示密码";

    private readonly ILogger<LoginViewModel> _logger;

    public LoginViewModel()
    {
        // 设计时构造函数
        _logger = null!;
        LoadSavedCredentials();
    }

    public LoginViewModel(ILogger<LoginViewModel> logger)
    {
        _logger = logger;
        LoadSavedCredentials();
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(UserNumber) || string.IsNullOrWhiteSpace(Password))
        {
            ShowErrorDialog("登录失败", "请输入用户号码和密码");
            return;
        }

        if (UserNumber.Length != 8)
        {
            ShowErrorDialog("登录失败", "用户号码必须是8位数字");
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
            statusWindow.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner;
            // 注意：Avalonia 可能需要不同的方式设置父窗口
        }

        statusWindow.Show();

        // 订阅事件
        statusViewModel.WindowCloseRequested += (s, e) => statusWindow.Close();
        statusViewModel.RetryRequested += async (s, e) =>
        {
            statusWindow.Close();
            await Task.Delay(100); // 短暂延迟后重试
            await LoginAsync();
        };

        try
        {
            _logger?.LogInformation("开始登录，用户号码: {UserNumber}", UserNumber);

            // 显示连接状态
            statusViewModel.ShowConnecting();

            // 执行登录
            await SimulateLoginAsync();

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
            _logger?.LogError(ex, "登录失败");
            statusViewModel.ShowError("登录失败", ex.Message);
        }
    }

    [RelayCommand]
    private void Register()
    {
        ShowErrorDialog("提示", "注册功能暂未实现");
    }

    [RelayCommand]
    private void ServerSettings()
    {
        ShowErrorDialog("提示", "服务器设置功能暂未实现");
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
        PasswordChar = IsPasswordVisible ? '\0' : '●';
        PasswordVisibilityIcon = IsPasswordVisible ? "🙈" : "👁";
        PasswordVisibilityTooltip = IsPasswordVisible ? "隐藏密码" : "显示密码";
    }

    private async Task SimulateLoginAsync()
    {
        // 模拟网络请求延迟
        await Task.Delay(2000);

        // 模拟登录验证
        if (UserNumber == "10000001" && Password == "123456")
        {
            // 登录成功
            return;
        }

        throw new UnauthorizedAccessException("用户号码或密码错误");
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
            // TODO: 从配置文件或注册表加载保存的登录信息
            // 这里使用临时的硬编码值进行演示
            if (RememberCredentials)
            {
                UserNumber = "10000001";
                // 出于安全考虑，不保存密码
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "加载保存的登录信息失败");
        }
    }

    private void SaveCredentials()
    {
        try
        {
            // TODO: 保存登录信息到配置文件或注册表
            // 注意：不应该保存明文密码
            _logger?.LogInformation("保存登录信息");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "保存登录信息失败");
        }
    }
}
