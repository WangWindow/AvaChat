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
    private string _serverAddress = string.Empty;

    [ObservableProperty]
    private bool _isPasswordVisible = false;

    [ObservableProperty]
    private char _passwordChar = '●';

    [ObservableProperty]
    private string _passwordVisibilityIcon = "👁";

    [ObservableProperty]
    private string _passwordVisibilityTooltip = "显示密码";

    /// <summary>
    /// 所有已保存账号信息列表（用于下拉选择）
    /// </summary>
    [ObservableProperty]
    private List<LoginInfo> _accountList = new();

    /// <summary>
    /// 当前选中的账号信息
    /// </summary>
    [ObservableProperty]
    private LoginInfo? _selectedAccount;

    /// <summary>
    /// 是否使用自动填充的密码（影响密码显示按钮的可用性）
    /// </summary>
    [ObservableProperty]
    private bool _isUsingAutoFilledPassword = false;

    /// <summary>
    /// 密码显示按钮是否可用
    /// </summary>
    public bool IsPasswordVisibilityEnabled => !IsUsingAutoFilledPassword;

    /// <summary>
    /// 控制账号列表下拉框是否打开
    /// </summary>
    [ObservableProperty]
    private bool _isAccountListOpen = false;

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

        // 在登录前重新加载服务器地址设置
        ReloadServerAddress();

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
                // 保存登录信息（包括用户名）
                SaveCredentials(RememberCredentials, resp.UserName);

                // 登录成功后进行数据同步
                bool syncSuccess = true;
                try
                {
                    // 使用新的批量同步API
                    syncSuccess = await api.BatchSyncAsync(UserId);
                }
                catch
                {
                    // 同步失败，记录但不阻断登录
                    syncSuccess = false;
                }

                // 设置全局登录状态，刷新托盘菜单
                App.OnUserLogin(UserId, ServerAddress, resp.UserName);

                // 显示成功状态并等待完成
                await statusViewModel.ShowSuccessAsync();

                // 在UI线程上执行窗口切换操作
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    try
                    {
                        Console.WriteLine("[LoginViewModel] 开始窗口切换操作");

                        // 关闭状态窗口
                        statusWindow.Close();
                        Console.WriteLine("[LoginViewModel] 状态窗口已关闭");

                        // 检查当前窗口状态
                        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                        {
                            Console.WriteLine($"[LoginViewModel] 当前窗口数量: {desktop.Windows.Count}");
                            foreach (var window in desktop.Windows)
                            {
                                Console.WriteLine($"[LoginViewModel] 窗口类型: {window.GetType().Name}, 标题: {window.Title}");
                            }
                        }

                        // 创建主窗口（在关闭登录窗口之前创建）
                        var mainWindow = new MainWindow
                        {
                            DataContext = new MainWindowViewModel()
                        };
                        Console.WriteLine("[LoginViewModel] 主窗口已创建");

                        // 设置为应用程序的主窗口
                        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop2)
                        {
                            desktop2.MainWindow = mainWindow;
                            Console.WriteLine("[LoginViewModel] 主窗口已设置为应用主窗口");
                        }

                        // 显示主窗口
                        mainWindow.Show();
                        Console.WriteLine("[LoginViewModel] 主窗口已调用Show()");

                        // 现在关闭登录窗口
                        CloseCurrentLoginWindow();

                        // 等待一小段时间确保窗口切换完成
                        await Task.Delay(200);

                        // 确保窗口可见并激活
                        mainWindow.WindowState = WindowState.Normal;
                        mainWindow.Activate();
                        Console.WriteLine("[LoginViewModel] 主窗口已激活");

                        // 临时注释掉可能引起问题的置顶操作
                        // mainWindow.Topmost = true;
                        // await Task.Delay(100);
                        // mainWindow.Topmost = false;

                        Console.WriteLine("[LoginViewModel] 主窗口切换完成");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LoginViewModel] 窗口切换过程中出错: {ex.Message}");
                        // 如果出错，至少确保登录窗口不会关闭
                    }
                });

                // 如同步失败可弹窗提示（可选）
                if (!syncSuccess)
                {
                    // 在主窗口显示后再显示错误提示
                    await Task.Delay(500);
                    ShowErrorDialog("同步失败", "部分数据未能从云端同步，已使用本地缓存。");
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
        ReloadServerAddress();
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
        // 重新加载服务器地址以显示当前设置
        ReloadServerAddress();

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
        // 如果使用自动填充密码，禁用密码显示功能
        if (IsUsingAutoFilledPassword)
        {
            return;
        }

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
        try
        {
            var loginWindow = GetCurrentLoginWindow();
            if (loginWindow != null)
            {
                Console.WriteLine("[LoginViewModel] 关闭登录窗口");
                loginWindow.Close();
            }
            else
            {
                Console.WriteLine("[LoginViewModel] 未找到登录窗口");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoginViewModel] 关闭登录窗口时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 重新从数据库加载服务器地址
    /// </summary>
    private void ReloadServerAddress()
    {
        try
        {
            var factory = new ClientDbContextFactory();
            using var db = factory.CreateDbContext([]);

            // 重新加载服务器地址设置
            var serverSetting = db.ClientSettings.FirstOrDefault(s => s.Key == "ServerAddress");
            if (serverSetting != null && !string.IsNullOrEmpty(serverSetting.Value))
            {
                ServerAddress = serverSetting.Value;
                Console.WriteLine($"[LoginViewModel] 重新加载服务器地址: {ServerAddress}");
            }
            else
            {
                ServerAddress = "localhost:5000";
                Console.WriteLine($"[LoginViewModel] 使用默认服务器地址: {ServerAddress}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoginViewModel] 加载服务器地址失败: {ex.Message}");
            ServerAddress = "localhost:5000";
        }
    }


    private void LoadSavedCredentials()
    {
        try
        {
            var factory = new ClientDbContextFactory();
            using var db = factory.CreateDbContext([]);

            // 加载服务器地址设置
            var serverSetting = db.ClientSettings.FirstOrDefault(s => s.Key == "ServerAddress");
            if (serverSetting != null && !string.IsNullOrEmpty(serverSetting.Value))
            {
                ServerAddress = serverSetting.Value;
            }
            else
            {
                ServerAddress = "localhost:5000";
            }

            // 加载"记住密码"勾选框状态
            var rememberSetting = db.ClientSettings.FirstOrDefault(s => s.Key == "RememberCredentials");
            if (rememberSetting != null && bool.TryParse(rememberSetting.Value, out bool rememberValue))
            {
                RememberCredentials = rememberValue;
            }

            // 获取所有已保存账号信息，按最后登录时间降序排列
            var loginInfos = db.LoginInfos
                .OrderByDescending(x => x.LoginTime)
                .ToList();

            AccountList = loginInfos.Select(info => new LoginInfo
            {
                UserId = info.UserId,
                Password = info.Password,
                UserName = info.UserName,
                LoginTime = info.LoginTime
            }).ToList();

            // 自动选择最近登录的账号
            if (AccountList.Count > 0)
            {
                SelectedAccount = AccountList.First();
            }
        }
        catch { /* 忽略异常 */ }
    }

    private void SaveCredentials(bool isSavePassword, string? userName = null)
    {
        try
        {
            var factory = new ClientDbContextFactory();
            using var db = factory.CreateDbContext([]);

            // 保存当前的服务器地址设置
            var serverSetting = db.ClientSettings.FirstOrDefault(s => s.Key == "ServerAddress");
            if (serverSetting != null)
            {
                serverSetting.Value = ServerAddress;
                db.ClientSettings.Update(serverSetting);
            }
            else
            {
                db.ClientSettings.Add(new ClientSetting
                {
                    Key = "ServerAddress",
                    Value = ServerAddress
                });
            }

            // 保存"记住密码"勾选框状态
            var rememberSetting = db.ClientSettings.FirstOrDefault(s => s.Key == "RememberCredentials");
            if (rememberSetting != null)
            {
                rememberSetting.Value = RememberCredentials.ToString();
                db.ClientSettings.Update(rememberSetting);
            }
            else
            {
                db.ClientSettings.Add(new ClientSetting
                {
                    Key = "RememberCredentials",
                    Value = RememberCredentials.ToString()
                });
            }

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
                UserName = userName,
                LoginTime = DateTime.Now
            };
            db.LoginInfos.Add(info);
            db.SaveChanges();
        }
        catch { /* 忽略异常 */ }
    }

    partial void OnSelectedAccountChanged(LoginInfo? value)
    {
        if (value != null)
        {
            UserId = value.UserId;

            if (value.HasSavedPassword)
            {
                Password = value.Password!;
                IsUsingAutoFilledPassword = true;
                IsPasswordVisible = false;
                PasswordChar = '●';
                PasswordVisibilityIcon = "👁";
                PasswordVisibilityTooltip = "自动填充密码不可显示";
            }
            else
            {
                Password = string.Empty;
                IsUsingAutoFilledPassword = false;
                PasswordVisibilityTooltip = "显示密码";
            }

            OnPropertyChanged(nameof(IsPasswordVisibilityEnabled));
        }
    }

    partial void OnPasswordChanged(string value)
    {
        if (IsUsingAutoFilledPassword && SelectedAccount?.Password != value)
        {
            IsUsingAutoFilledPassword = false;
            PasswordVisibilityTooltip = "显示密码";
            OnPropertyChanged(nameof(IsPasswordVisibilityEnabled));
        }
    }

    [RelayCommand]
    private void ShowAccountList()
    {
        // 切换账号列表的打开状态
        IsAccountListOpen = !IsAccountListOpen;
    }
}
