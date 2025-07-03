using Microsoft.AspNetCore.Mvc;

namespace AvaChat.Server.Models;

/// <summary>
/// 管理控制器 - 提供数据库管理和维护功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly DataService _dataService;
    private readonly DatabaseMaintenanceService _maintenanceService;
    private readonly DatabaseBackupService _backupService;

    public AdminController(DataService dataService, DatabaseMaintenanceService maintenanceService, DatabaseBackupService backupService)
    {
        _dataService = dataService;
        _maintenanceService = maintenanceService;
        _backupService = backupService;
    }

    /// <summary>
    /// 获取数据库健康报告
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<DatabaseHealthReport>> GetHealthReport()
    {
        var report = await _maintenanceService.GenerateHealthReportAsync();
        return Ok(report);
    }

    /// <summary>
    /// 获取用户统计信息
    /// </summary>
    [HttpGet("user-stats/{userId}")]
    public async Task<ActionResult<UserStatsInfo>> GetUserStats(string userId)
    {
        var stats = await _dataService.GetUserStatsAsync(userId);
        return Ok(stats);
    }

    /// <summary>
    /// 获取活跃用户列表
    /// </summary>
    [HttpGet("active-users")]
    public async Task<ActionResult<List<User>>> GetActiveUsers([FromQuery] int days = 7)
    {
        var users = await _dataService.GetActiveUsersAsync(days);
        return Ok(users);
    }

    /// <summary>
    /// 执行数据库优化
    /// </summary>
    [HttpPost("optimize")]
    public async Task<ActionResult> OptimizeDatabase()
    {
        try
        {
            await _maintenanceService.OptimizeDatabaseAsync();
            return Ok(new { Success = true, Message = "数据库优化完成" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Error = ex.Message });
        }
    }

    /// <summary>
    /// 创建数据库备份
    /// </summary>
    [HttpPost("backup")]
    public async Task<ActionResult> CreateBackup()
    {
        try
        {
            var backupPath = await _backupService.CreateBackupAsync();
            return Ok(new { Success = true, BackupPath = backupPath });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Error = ex.Message });
        }
    }

    /// <summary>
    /// 清理过期数据
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<ActionResult> CleanupExpiredData([FromQuery] int daysToKeep = 90)
    {
        try
        {
            var cleanedCount = await _dataService.CleanupExpiredDataAsync(daysToKeep);
            return Ok(new { Success = true, CleanedRecords = cleanedCount });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Error = ex.Message });
        }
    }

    /// <summary>
    /// 执行完整的数据库维护
    /// </summary>
    [HttpPost("maintenance")]
    public async Task<ActionResult> RunMaintenance()
    {
        try
        {
            await _maintenanceService.RunPeriodicMaintenanceAsync();
            return Ok(new { Success = true, Message = "数据库维护完成" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Error = ex.Message });
        }
    }

    /// <summary>
    /// 获取消息历史（分页）
    /// </summary>
    [HttpGet("messages/{userId1}/{userId2}")]
    public async Task<ActionResult<List<Message>>> GetMessageHistory(
        string userId1,
        string userId2,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 50)
    {
        var messages = await _dataService.GetMessageHistoryAsync(userId1, userId2, page, pageSize);
        return Ok(messages);
    }
}
