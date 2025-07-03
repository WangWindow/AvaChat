using System.IO.Compression;
using Microsoft.Data.Sqlite;

namespace AvaChat.Server.Models;

/// <summary>
/// 数据库备份与恢复服务
/// </summary>
public class DatabaseBackupService
{
    private readonly string _dbPath;
    private readonly string _backupPath;

    public DatabaseBackupService(string dbPath)
    {
        _dbPath = dbPath;
        _backupPath = Path.Combine(Path.GetDirectoryName(dbPath)!, "Backups");
        Directory.CreateDirectory(_backupPath);
    }

    /// <summary>
    /// 创建数据库备份
    /// </summary>
    public async Task<string> CreateBackupAsync()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"AvaChat_Backup_{timestamp}.db";
        var backupFilePath = Path.Combine(_backupPath, backupFileName);

        // 添加重试机制
        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // 如果不是第一次尝试，添加延迟
                if (attempt > 1)
                {
                    await Task.Delay(1000 * attempt);
                }

                // 使用正确的WAL模式配置进行在线备份
                using var source = new SqliteConnection($"Data Source={_dbPath}");
                await source.OpenAsync();

                // 启用WAL模式（如果尚未启用）
                using var walCommand = source.CreateCommand();
                walCommand.CommandText = "PRAGMA journal_mode=WAL";
                await walCommand.ExecuteNonQueryAsync();

                // 检查数据库状态
                using var statusCommand = source.CreateCommand();
                statusCommand.CommandText = "PRAGMA integrity_check";
                var integrityResult = await statusCommand.ExecuteScalarAsync();
                if (integrityResult?.ToString() != "ok")
                {
                    throw new InvalidOperationException($"数据库完整性检查失败: {integrityResult}");
                }

                // 执行VACUUM INTO命令进行在线备份
                using var command = source.CreateCommand();
                command.CommandText = $"VACUUM INTO '{backupFilePath.Replace("'", "''")}'";
                await command.ExecuteNonQueryAsync();

                // 验证备份文件是否创建成功
                if (!File.Exists(backupFilePath))
                    throw new InvalidOperationException("备份文件创建失败");

                // 压缩备份文件
                var zipPath = backupFilePath + ".zip";

                // 如果zip文件已存在，先删除
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                zip.CreateEntryFromFile(backupFilePath, backupFileName);

                // 删除临时的未压缩文件
                File.Delete(backupFilePath);

                return zipPath;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                // 清理可能创建的文件
                if (File.Exists(backupFilePath))
                {
                    try { File.Delete(backupFilePath); } catch { }
                }

                // 如果不是最后一次尝试，继续重试
                if (attempt == maxRetries)
                    throw new InvalidOperationException($"创建备份失败 (尝试 {attempt}/{maxRetries}): {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // 清理可能创建的文件
                if (File.Exists(backupFilePath))
                {
                    try { File.Delete(backupFilePath); } catch { }
                }

                throw new InvalidOperationException($"创建备份失败 (尝试 {attempt}/{maxRetries}): {ex.Message}", ex);
            }
        }

        throw new InvalidOperationException("备份创建失败：已达到最大重试次数");
    }

    /// <summary>
    /// 恢复数据库
    /// </summary>
    public async Task RestoreBackupAsync(string backupZipPath)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // 解压备份文件
            await Task.Run(() => ZipFile.ExtractToDirectory(backupZipPath, tempDir));
            var extractedDb = Directory.GetFiles(tempDir, "*.db").FirstOrDefault();

            if (extractedDb == null)
                throw new InvalidOperationException("备份文件中未找到数据库文件");

            // 停止当前连接并替换数据库文件
            GC.Collect();
            GC.WaitForPendingFinalizers();

            await Task.Run(() => File.Copy(extractedDb, _dbPath, true));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// 自动备份调度
    /// </summary>
    public void StartAutoBackup(TimeSpan interval)
    {
        var timer = new Timer(async _ => await CreateBackupAsync(), null, TimeSpan.Zero, interval);
    }

    /// <summary>
    /// 清理过期备份
    /// </summary>
    public void CleanupOldBackups(int keepDays = 30)
    {
        var cutoffDate = DateTime.Now.AddDays(-keepDays);
        var oldBackups = Directory.GetFiles(_backupPath, "*.zip")
            .Where(f => File.GetCreationTime(f) < cutoffDate);

        foreach (var backup in oldBackups)
        {
            File.Delete(backup);
        }
    }
}
