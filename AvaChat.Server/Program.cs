namespace AvaChat.Server;

sealed class Program
{
    // 全局服务提供器，用于访问依赖注入容器
    private static IServiceProvider? _serviceProvider;

    // 获取服务提供器的公共方法
    public static IServiceProvider? GetServiceProvider() => _serviceProvider;

    // 设置服务提供器的内部方法
    internal static void SetServiceProvider(IServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
