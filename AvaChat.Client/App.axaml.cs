namespace AvaChat.Client;

public partial class App : Application
{
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
        // TODO
        var options = new DbContextOptionsBuilder<ClientDbContext>()
            .UseSqlite("Data Source=AvaChat.Client.db")
            .Options;
        using var db = new ClientDbContext(options);
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
    private void Exit_Click(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    #endregion
}
