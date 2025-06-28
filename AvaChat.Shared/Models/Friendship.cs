namespace AvaChat.Shared.Models;

/// <summary>
/// 好友关系实体模型
/// </summary>
[Table("Friendships")]
public class Friendship
{
    /// <summary>
    /// 好友关系ID（主键）
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 发起者用户ID
    /// </summary>
    [Required]
    public int InitiatorId { get; set; }

    /// <summary>
    /// 接受者用户ID
    /// </summary>
    [Required]
    public int AcceptorId { get; set; }

    /// <summary>
    /// 好友关系状态
    /// </summary>
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

    /// <summary>
    /// 建立时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 接受时间
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// 发起者备注名
    /// </summary>
    [StringLength(50)]
    public string? InitiatorRemark { get; set; }

    /// <summary>
    /// 接受者备注名
    /// </summary>
    [StringLength(50)]
    public string? AcceptorRemark { get; set; }

    // 导航属性
    /// <summary>
    /// 发起者
    /// </summary>
    [ForeignKey(nameof(InitiatorId))]
    public virtual User Initiator { get; set; } = null!;

    /// <summary>
    /// 接受者
    /// </summary>
    [ForeignKey(nameof(AcceptorId))]
    public virtual User Acceptor { get; set; } = null!;
}

/// <summary>
/// 好友关系状态枚举
/// </summary>
public enum FriendshipStatus
{
    /// <summary>
    /// 待接受
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 已接受
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// 已拒绝
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// 已删除
    /// </summary>
    Deleted = 3,

    /// <summary>
    /// 已屏蔽
    /// </summary>
    Blocked = 4
}
