using AvaChat.Client.Models;
using Avalonia.LogicalTree;
using CommunityToolkit.Mvvm.Messaging;
namespace AvaChat.Client;

public partial class App : Application
{
    public static TrayIcon? MainTrayIcon;
    public static string? CurrentUserId { get; set; }
    public static string? CurrentServerAddress { get; set; }
    public static string? CurrentUserName { get; set; }

    // SignalR客户端实例
    public static SignalRClient? SignalRClient { get; private set; }

    public static async void OnUserLogin(string userId, string serverAddress)
    {
        CurrentUserId = userId;
        CurrentServerAddress = serverAddress;
        UpdateTrayMenu();

        // 创建并连接SignalR客户端
        try
        {
            SignalRClient?.Dispose();
            SignalRClient = new SignalRClient(serverAddress, userId);
            await SignalRClient.ConnectAsync();

            // 通知其他组件SignalR客户端已创建
            WeakReferenceMessenger.Default.Send(SignalRClient);

            Console.WriteLine("[App] SignalR连接成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[App] SignalR连接失败: {ex.Message}");
        }
    }

    public static async Task OnUserLogout()
    {
        // 断开SignalR连接
        if (SignalRClient != null)
        {
            await SignalRClient.DisconnectAsync();
            SignalRClient.Dispose();
            SignalRClient = null;
        }

        CurrentUserId = null;
        UpdateTrayMenu();
    }

    public static void UpdateTrayMenu()
    {
        if (Current is App app)
        {
            var menu = new NativeMenu();

            var showItem = new NativeMenuItem("显示主窗口");
            showItem.Click += app.ShowWindow_Click;
            menu.Add(showItem);

            if (!string.IsNullOrEmpty(CurrentUserId))
            {
                var setMenu = new NativeMenuItem("设置");
                var sub = new NativeMenu();

                var profileItem = new NativeMenuItem("个人资料");
                profileItem.Click += app.Profile_Click;
                sub.Add(profileItem);

                var msgItem = new NativeMenuItem("消息设置");
                msgItem.Click += app.MessageSettings_Click;
                sub.Add(msgItem);

                sub.Add(new NativeMenuItemSeparator());

                var themeItem = new NativeMenuItem("主题切换");
                themeItem.Click += app.ThemeSwitch_Click;
                sub.Add(themeItem);

                setMenu.Menu = sub;
                menu.Add(setMenu);
            }
            menu.Add(new NativeMenuItemSeparator());

            var exitItem = new NativeMenuItem("退出");
            exitItem.Click += app.Exit_Click;
            menu.Add(exitItem);

#pragma warning disable CS8602 // 解引用可能出现空引用。
            MainTrayIcon.Menu = menu;
#pragma warning restore CS8602 // 解引用可能出现空引用。
        }
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Task.Run(Start);


        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // 启动时显示登录窗口而不是主窗口
            desktop.MainWindow = new LoginWindow { DataContext = new LoginViewModel() };
            // desktop.MainWindow = new MainWindow
            // {
            //     DataContext = new MainWindowViewModel()
            // };

            // 优先通过XAML注册的WindowIcon资源获取托盘图标
            MainTrayIcon = new TrayIcon
            {
                Icon = Current!.Resources["AppTrayIcon"] as WindowIcon ?? throw new NullReferenceException("AppTrayIcon 资源未找到或类型错误"),
                ToolTipText = "AvaChat",
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove = BindingPlugins
            .DataValidators.OfType<DataAnnotationsValidationPlugin>()
            .ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private static void Start()
    {
        var db = new ClientDbContextFactory().CreateDbContext([]);
        db.Database.EnsureCreated();
    }

    #region 托盘图标事件处理

    /// <summary>
    /// 显示主窗口
    /// </summary>
    private void ShowWindow_Click(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.Windows.FirstOrDefault(w => w is MainWindow);
            if (mainWindow != null)
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            }
            else
            {
                // 如果主窗口不存在，可能用户还未登录，显示登录窗口
                var loginWindow = desktop.Windows.FirstOrDefault(w => w is LoginWindow);
                if (loginWindow != null)
                {
                    loginWindow.Show();
                    loginWindow.WindowState = WindowState.Normal;
                    loginWindow.Activate();
                }
            }
        }
    }

    /// <summary>
    /// 个人资料设置
    /// </summary>
    private void Profile_Click(object? sender, EventArgs e)
    {
        // TODO: 实现个人资料窗口
        // 可以创建一个 ProfileWindow 或者在主窗口中导航到设置页面
    }

    /// <summary>
    /// 消息设置
    /// </summary>
    private void MessageSettings_Click(object? sender, EventArgs e)
    {
        // TODO: 实现消息设置窗口
        // 可以创建一个 MessageSettingsWindow 或者在主窗口中导航到消息设置页面
    }

    /// <summary>
    /// 主题切换
    /// </summary>
    private void ThemeSwitch_Click(object? sender, EventArgs e)
    {
        // 切换主题
        RequestedThemeVariant =
            RequestedThemeVariant == ThemeVariant.Light ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    /// <summary>
    /// 退出应用程序
    /// </summary>
    private async void Exit_Click(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(CurrentUserId) && !string.IsNullOrEmpty(CurrentServerAddress))
        {
            try
            {
                var api = new ApiService(CurrentServerAddress);
                await api.LogoutAsync(CurrentUserId);
            }
            catch { /* 忽略异常 */ }
        }
        await OnUserLogout();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    #endregion
}
