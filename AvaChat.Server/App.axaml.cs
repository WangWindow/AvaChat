using System.Threading.Tasks;
using AvaChat.Server.Hubs;
using AvaChat.Server.Models;
using AvaChat.Shared.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace AvaChat.Server;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 启动Web API服务（后台线程）
        Task.Run(Start);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void Start()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddDbContext<ServerDbContext>();
        builder.Services.AddControllers();

        // 添加SignalR服务
        builder.Services.AddSignalR();

        // 添加自定义服务
        builder.Services.AddScoped<DataService>();
        builder.Services.AddSingleton<DatabaseBackupService>(provider =>
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbPath = Path.Combine(appData, "AvaChat", "AvaChat.Server.db");
            return new DatabaseBackupService(dbPath);
        });
        builder.Services.AddScoped<DatabaseMaintenanceService>();

        // 添加后台维护服务
        builder.Services.AddHostedService<BackgroundMaintenanceService>();

        // 添加日志服务
        builder.Services.AddLogging();

        var app = builder.Build();

        // 存储服务提供者，以便在应用程序其他地方访问
        Program.SetServiceProvider(app.Services);

        // 初始化数据库并将所有用户状态设为离线
        InitializeDatabaseAsync(app.Services).Wait();

        // 配置SignalR Hub路由
        app.MapHub<ChatHub>("/chatHub");

        app.MapControllers();
        app.Run("http://localhost:5000");
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

    /// <summary>
    /// 初始化数据库，将所有用户状态设为离线
    /// </summary>
    private static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

            // 确保数据库已创建
            await dbContext.Database.EnsureCreatedAsync();

            // 配置WAL模式以支持并发访问
            try
            {
                await dbContext.ConfigureWalModeAsync();
                Console.WriteLine("[服务器启动] WAL模式已启用");
            }
            catch (Exception walEx)
            {
                Console.WriteLine($"[服务器启动警告] 配置WAL模式失败: {walEx.Message}");
            }

            // 升级现有的哈希密码为明文密码（为兼容新的加密传输方式）
            await UpgradePasswordsAsync(dbContext);

            // 将所有用户状态设为离线
            var onlineUsers = await dbContext.Users
                .Where(u => u.Status == UserStatus.Online)
                .ToListAsync();

            if (onlineUsers.Count > 0)
            {
                foreach (var user in onlineUsers)
                {
                    user.Status = UserStatus.Offline;
                }

                await dbContext.SaveChangesAsync();
                Console.WriteLine($"[服务器启动] 已将 {onlineUsers.Count} 个在线用户状态设为离线");
            }
            else
            {
                Console.WriteLine("[服务器启动] 没有在线用户需要设为离线状态");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[服务器启动错误] 初始化数据库失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 升级现有的哈希密码为明文密码（为兼容新的加密传输方式）
    /// </summary>
    private static async Task UpgradePasswordsAsync(ServerDbContext dbContext)
    {
        try
        {
            // 获取所有用户
            var users = await dbContext.Users.ToListAsync();
            int upgradedCount = 0;
            const string defaultPassword = "123456"; // 默认密码，用户需要重新设置

            foreach (var user in users)
            {
                // 检查密码是否是哈希格式（Base64，44字符）
                if (!string.IsNullOrEmpty(user.Password) && IsHashedPassword(user.Password))
                {
                    // 由于无法从哈希还原明文，设置为默认密码
                    user.Password = defaultPassword;
                    upgradedCount++;
                    Console.WriteLine($"[密码升级] 用户 {user.UserName} 的密码已重置为默认密码，请提醒用户重新设置");
                }
            }

            if (upgradedCount > 0)
            {
                await dbContext.SaveChangesAsync();
                Console.WriteLine($"[密码升级] 共重置了 {upgradedCount} 个用户的密码为默认密码");
                Console.WriteLine($"[密码升级] 警告：这些用户需要使用默认密码 '{defaultPassword}' 登录并重新设置密码");
            }
            else
            {
                Console.WriteLine("[密码升级] 所有用户密码已经是明文格式");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[密码升级错误] {ex.Message}");
        }
    }

    /// <summary>
    /// 检查密码是否已经是哈希格式
    /// </summary>
    private static bool IsHashedPassword(string password)
    {
        // 哈希密码特征：
        // 1. 长度通常为44字符（SHA256 + Base64）
        // 2. 包含Base64字符
        if (password.Length != 44)
            return false;

        try
        {
            // 尝试Base64解码，如果成功且长度为32字节（SHA256），则可能是哈希
            var bytes = Convert.FromBase64String(password);
            return bytes.Length == 32;
        }
        catch
        {
            return false;
        }
    }
}