using AvaChat.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AvaChat.Server.Models;

public class ServerDbContext(DbContextOptions<ServerDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Friendship> Friendships { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<FriendRequest> FriendRequests { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbDir = Path.Combine(appData, "AvaChat");
            Directory.CreateDirectory(dbDir);
            var dbPath = Path.Combine(dbDir, "AvaChat.Server.db");

            // 配置SQLite连接字符串
            var connectionString = $"Data Source={dbPath};Cache=Shared;Pooling=true";
            optionsBuilder.UseSqlite(connectionString, options =>
            {
                options.CommandTimeout(30);
            });

            // 在连接后设置WAL模式
            optionsBuilder.UseSqlite(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Users表配置
        modelBuilder
            .Entity<User>()
            .HasIndex(u => u.UserId)
            .IsUnique();

        modelBuilder
            .Entity<User>()
            .HasIndex(u => u.UserName)
            .IsUnique();

        modelBuilder
            .Entity<User>()
            .HasIndex(u => new { u.Status, u.LastLoginTime });

        // Friendships表配置
        modelBuilder
            .Entity<Friendship>()
            .HasKey(f => new { f.UserId, f.FriendUserId });

        // 添加外键约束
        modelBuilder
            .Entity<Friendship>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<Friendship>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.FriendUserId)
            .OnDelete(DeleteBehavior.Cascade);

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

        // 添加外键约束
        modelBuilder
            .Entity<Message>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<Message>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        // FriendRequests表配置
        modelBuilder
            .Entity<FriendRequest>()
            .HasIndex(f => new { f.FromUserId, f.ToUserId })
            .IsUnique()
            .HasFilter("Status = 0");

        modelBuilder
            .Entity<FriendRequest>()
            .HasIndex(f => new { f.ToUserId, f.Status });

        modelBuilder
            .Entity<FriendRequest>()
            .HasIndex(f => f.CreatedAt);

        // 添加外键约束
        modelBuilder
            .Entity<FriendRequest>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.FromUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<FriendRequest>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.ToUserId)
            .OnDelete(DeleteBehavior.Cascade);
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
