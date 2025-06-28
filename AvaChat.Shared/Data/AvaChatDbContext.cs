using AvaChat.Shared.Models;

namespace AvaChat.Shared.Data;

/// <summary>
/// AvaChat 数据库上下文
/// </summary>
public class AvaChatDbContext : DbContext
{
    /// <summary>
    /// 用户表
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// 消息表
    /// </summary>
    public DbSet<Message> Messages { get; set; } = null!;

    /// <summary>
    /// 好友关系表
    /// </summary>
    public DbSet<Friendship> Friendships { get; set; } = null!;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AvaChatDbContext(DbContextOptions<AvaChatDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// 配置模型
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置用户表
        modelBuilder.Entity<User>(entity =>
        {
            // 用户号码唯一索引
            entity.HasIndex(u => u.UserNumber)
                  .IsUnique()
                  .HasDatabaseName("IX_Users_UserNumber");

            // 昵称索引
            entity.HasIndex(u => u.Nickname)
                  .HasDatabaseName("IX_Users_Nickname");

            // 在线状态索引
            entity.HasIndex(u => u.Status)
                  .HasDatabaseName("IX_Users_Status");

            // 最后在线时间索引
            entity.HasIndex(u => u.LastOnlineTime)
                  .HasDatabaseName("IX_Users_LastOnlineTime");
        });

        // 配置消息表
        modelBuilder.Entity<Message>(entity =>
        {
            // 发送者索引
            entity.HasIndex(m => m.SenderId)
                  .HasDatabaseName("IX_Messages_SenderId");

            // 接收者索引
            entity.HasIndex(m => m.ReceiverId)
                  .HasDatabaseName("IX_Messages_ReceiverId");

            // 发送时间索引
            entity.HasIndex(m => m.SentTime)
                  .HasDatabaseName("IX_Messages_SentTime");

            // 消息状态索引
            entity.HasIndex(m => m.Status)
                  .HasDatabaseName("IX_Messages_Status");

            // 组合索引：发送者-接收者-时间
            entity.HasIndex(m => new { m.SenderId, m.ReceiverId, m.SentTime })
                  .HasDatabaseName("IX_Messages_Sender_Receiver_Time");

            // 配置外键关系
            entity.HasOne(m => m.Sender)
                  .WithMany(u => u.SentMessages)
                  .HasForeignKey(m => m.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Receiver)
                  .WithMany(u => u.ReceivedMessages)
                  .HasForeignKey(m => m.ReceiverId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 配置好友关系表
        modelBuilder.Entity<Friendship>(entity =>
        {
            // 发起者索引
            entity.HasIndex(f => f.InitiatorId)
                  .HasDatabaseName("IX_Friendships_InitiatorId");

            // 接受者索引
            entity.HasIndex(f => f.AcceptorId)
                  .HasDatabaseName("IX_Friendships_AcceptorId");

            // 状态索引
            entity.HasIndex(f => f.Status)
                  .HasDatabaseName("IX_Friendships_Status");

            // 唯一约束：防止重复好友关系
            entity.HasIndex(f => new { f.InitiatorId, f.AcceptorId })
                  .IsUnique()
                  .HasDatabaseName("IX_Friendships_Unique");

            // 配置外键关系
            entity.HasOne(f => f.Initiator)
                  .WithMany(u => u.InitiatedFriendships)
                  .HasForeignKey(f => f.InitiatorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Acceptor)
                  .WithMany(u => u.ReceivedFriendships)
                  .HasForeignKey(f => f.AcceptorId)
                  .OnDelete(DeleteBehavior.Restrict);

            // 检查约束：确保发起者和接受者不是同一人
            entity.ToTable(t => t.HasCheckConstraint("CK_Friendship_DifferentUsers",
                                                    "[InitiatorId] != [AcceptorId]"));
        });

        // 种子数据
        SeedData(modelBuilder);
    }

    /// <summary>
    /// 种子数据
    /// </summary>
    private void SeedData(ModelBuilder modelBuilder)
    {
        // 创建默认系统用户
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                UserNumber = "10000000",
                Password = HashPassword("admin123"), // 实际应用中应该使用安全的哈希算法
                Nickname = "系统管理员",
                Signature = "欢迎使用 AvaChat 聊天系统！",
                Status = UserStatus.Online,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastOnlineTime = DateTime.UtcNow
            }
        );
    }

    /// <summary>
    /// 简单的密码哈希函数（实际应用中应使用更安全的方法）
    /// </summary>
    private string HashPassword(string password)
    {
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password + "AvaChat_Salt"));
    }
}
