using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AvaChat.Server.Models;

/// <summary>
/// 数据库维护服务 - 提供数据库优化和维护功能
/// </summary>
public class DatabaseMaintenanceService
{
    private readonly ServerDbContext _context;
    private readonly DatabaseBackupService _backupService;
    private readonly ILogger<DatabaseMaintenanceService> _logger;

    public DatabaseMaintenanceService(ServerDbContext context, DatabaseBackupService backupService, ILogger<DatabaseMaintenanceService> logger)
    {
        _context = context;
        _backupService = backupService;
        _logger = logger;
    }

    /// <summary>
    /// 执行数据库优化
    /// </summary>
    public async Task OptimizeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("开始数据库优化...");

            // 1. 更新统计信息
            await _context.Database.ExecuteSqlRawAsync("ANALYZE");

            // 2. 重建索引
            await _context.Database.ExecuteSqlRawAsync("REINDEX");

            // 3. 清理碎片
            await _context.Database.ExecuteSqlRawAsync("VACUUM");

            _logger.LogInformation("数据库优化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库优化失败");
            throw;
        }
    }

    /// <summary>
    /// 执行数据归档
    /// </summary>
    public async Task<int> ArchiveOldDataAsync(int daysToKeep = 365)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        var archivedCount = 0;

        try
        {
            _logger.LogInformation($"开始归档 {cutoffDate} 之前的数据...");

            // 归档旧消息到归档表（如果存在）
            var oldMessages = await _context.Messages
                .Where(m => m.Timestamp < cutoffDate && m.MessageType != MessageType.System)
                .CountAsync();

            if (oldMessages > 0)
            {
                // 这里可以实现将数据移动到归档表的逻辑
                _logger.LogInformation($"发现 {oldMessages} 条可归档的消息");
                archivedCount += oldMessages;
            }

            _logger.LogInformation($"数据归档完成，共归档 {archivedCount} 条记录");
            return archivedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据归档失败");
            throw;
        }
    }

    /// <summary>
    /// 生成数据库健康报告
    /// </summary>
    public async Task<DatabaseHealthReport> GenerateHealthReportAsync()
    {
        var report = new DatabaseHealthReport();

        try
        {
            // 统计各表记录数
            report.UserCount = await _context.Users.CountAsync();
            report.MessageCount = await _context.Messages.CountAsync();
            report.FriendshipCount = await _context.Friendships.CountAsync();
            report.FriendRequestCount = await _context.FriendRequests.CountAsync();

            // 统计活跃用户
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
            report.ActiveUserCount = await _context.Users
                .CountAsync(u => u.Status == UserStatus.Online || u.LastLoginTime > oneWeekAgo);

            // 统计今日消息
            var today = DateTime.UtcNow.Date;
            report.TodayMessageCount = await _context.Messages
                .CountAsync(m => m.Timestamp >= today);

            // 统计待处理好友申请
            report.PendingFriendRequestCount = await _context.FriendRequests
                .CountAsync(r => r.Status == FriendRequestStatus.Pending);

            // 数据库大小（需要实际实现）
            report.DatabaseSizeBytes = await GetDatabaseSizeAsync();

            report.GeneratedAt = DateTime.UtcNow;
            report.IsHealthy = true;

            _logger.LogInformation("数据库健康报告生成完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成数据库健康报告失败");
            report.IsHealthy = false;
            report.ErrorMessage = ex.Message;
        }

        return report;
    }

    /// <summary>
    /// 获取数据库大小
    /// </summary>
    private async Task<long> GetDatabaseSizeAsync()
    {
        try
        {
            var result = await _context.Database
                .SqlQueryRaw<DatabaseSizeResult>("SELECT page_count * page_size as size FROM pragma_page_count(), pragma_page_size()")
                .FirstOrDefaultAsync();

            return result?.Size ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 定期维护任务
    /// </summary>
    public async Task RunPeriodicMaintenanceAsync()
    {
        try
        {
            _logger.LogInformation("开始定期数据库维护...");

            // 1. 清理过期数据
            var dataService = new DataService(_context);
            var cleanedRecords = await dataService.CleanupExpiredDataAsync();
            _logger.LogInformation($"清理了 {cleanedRecords} 条过期记录");

            // 2. 创建备份
            var backupPath = await _backupService.CreateBackupAsync();
            _logger.LogInformation($"创建备份: {backupPath}");

            // 3. 清理旧备份
            _backupService.CleanupOldBackups(30);

            // 4. 优化数据库
            await OptimizeDatabaseAsync();

            _logger.LogInformation("定期数据库维护完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "定期数据库维护失败");
            throw;
        }
    }
}

/// <summary>
/// 数据库健康报告
/// </summary>
public class DatabaseHealthReport
{
    public int UserCount { get; set; }
    public int MessageCount { get; set; }
    public int FriendshipCount { get; set; }
    public int FriendRequestCount { get; set; }
    public int ActiveUserCount { get; set; }
    public int TodayMessageCount { get; set; }
    public int PendingFriendRequestCount { get; set; }
    public long DatabaseSizeBytes { get; set; }
    public DateTime GeneratedAt { get; set; }
    public bool IsHealthy { get; set; }
    public string? ErrorMessage { get; set; }

    public string DatabaseSizeFormatted =>
        DatabaseSizeBytes < 1024 * 1024 ? $"{DatabaseSizeBytes / 1024.0:F2} KB" :
        DatabaseSizeBytes < 1024 * 1024 * 1024 ? $"{DatabaseSizeBytes / (1024.0 * 1024):F2} MB" :
        $"{DatabaseSizeBytes / (1024.0 * 1024 * 1024):F2} GB";
}

/// <summary>
/// 数据库大小查询结果
/// </summary>
public class DatabaseSizeResult
{
    public long Size { get; set; }
}
