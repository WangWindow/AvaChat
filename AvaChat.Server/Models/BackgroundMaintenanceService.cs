using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AvaChat.Server.Models;

/// <summary>
/// 后台维护服务 - 定期执行数据库维护任务
/// </summary>
public class BackgroundMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundMaintenanceService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(24); // 每24小时执行一次

    public BackgroundMaintenanceService(IServiceProvider serviceProvider, ILogger<BackgroundMaintenanceService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("后台维护服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoMaintenanceWork();
                await Task.Delay(_period, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("后台维护服务正在停止");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "后台维护任务执行失败");
                // 等待较短时间后重试
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }

    private async Task DoMaintenanceWork()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var maintenanceService = scope.ServiceProvider.GetRequiredService<DatabaseMaintenanceService>();

            _logger.LogInformation("开始执行定期维护任务");
            await maintenanceService.RunPeriodicMaintenanceAsync();
            _logger.LogInformation("定期维护任务完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "定期维护任务失败");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("后台维护服务正在停止...");
        await base.StopAsync(stoppingToken);
    }
}
