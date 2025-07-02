
using System.Threading.Tasks;
using AvaChat.Server.Hubs;
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

        var app = builder.Build();

        // 存储服务提供者，以便在应用程序其他地方访问
        Program.SetServiceProvider(app.Services);

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
}