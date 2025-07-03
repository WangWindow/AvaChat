using AvaChat.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AvaChat.Client.Models;

public class ClientDbContext(DbContextOptions<ClientDbContext> options) : DbContext(options)
{
    public DbSet<UserInfo> Users { get; set; } = null!;
    public DbSet<Friendship> Friendships { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<ClientSetting> ClientSettings { get; set; } = null!;
    public DbSet<LoginInfo> LoginInfos { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbDir = Path.Combine(appData, "AvaChat");
            Directory.CreateDirectory(dbDir);
            var dbPath = Path.Combine(dbDir, "AvaChat.Client.db");

            // 配置SQLite连接字符串
            var connectionString = $"Data Source={dbPath};Cache=Shared;Pooling=true";
            optionsBuilder.UseSqlite(connectionString, options =>
            {
                options.CommandTimeout(30);
            });
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Users表配置
        modelBuilder
            .Entity<UserInfo>()
            .HasIndex(u => u.UserId)
            .IsUnique();

        modelBuilder
            .Entity<UserInfo>()
            .HasIndex(u => u.UserName);

        modelBuilder
            .Entity<UserInfo>()
            .HasIndex(u => new { u.Status, u.LastLoginTime });

        // Friendships表配置
        modelBuilder
            .Entity<Friendship>()
            .HasKey(f => new { f.UserId, f.FriendUserId });

        // Messages表配置
        modelBuilder
            .Entity<Message>()
            .HasIndex(m => new { m.SenderId, m.ReceiverId, m.Timestamp });

        modelBuilder
            .Entity<Message>()
            .HasIndex(m => m.Timestamp);

        modelBuilder
            .Entity<Message>()
            .HasIndex(m => new { m.Status, m.MessageType });

        modelBuilder
            .Entity<Message>()
            .HasIndex(m => new { m.ReceiverId, m.Status });

        // ClientSettings表配置
        modelBuilder
            .Entity<ClientSetting>()
            .HasIndex(c => c.Key)
            .IsUnique();

        // LoginInfos表配置
        modelBuilder
            .Entity<LoginInfo>()
            .HasIndex(l => l.LoginTime);

        modelBuilder
            .Entity<LoginInfo>()
            .HasIndex(l => l.UserName);
    }

    /// <summary>
    /// 配置数据库连接以启用WAL模式
    /// </summary>
    public async Task ConfigureWalModeAsync()
    {
        await Database.OpenConnectionAsync();

        // 启用WAL模式
        await Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL");

        // 启用外键约束
        await Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON");

        // 设置同步模式为NORMAL以提高性能
        await Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL");
    }
}
