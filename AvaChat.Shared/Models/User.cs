namespace AvaChat.Shared.Models;

/// <summary>
/// 用户实体模型
/// </summary>
[Table("Users")]
public class User
{
    /// <summary>
    /// 用户ID（主键）
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 用户号码（8位唯一标识）
    /// </summary>
    [Required]
    [StringLength(8, MinimumLength = 8)]
    [Column("UserNumber")]
    public string UserNumber { get; set; } = string.Empty;

    /// <summary>
    /// 用户密码（哈希存储）
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 用户昵称
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// 个性签名
    /// </summary>
    [StringLength(200)]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// 头像路径
    /// </summary>
    [StringLength(500)]
    public string? Avatar { get; set; }

    /// <summary>
    /// 在线状态
    /// </summary>
    public UserStatus Status { get; set; } = UserStatus.Offline;

    /// <summary>
    /// 最后在线时间
    /// </summary>
    public DateTime LastOnlineTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 注册时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // 导航属性
    /// <summary>
    /// 发送的消息
    /// </summary>
    public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();

    /// <summary>
    /// 接收的消息
    /// </summary>
    public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();

    /// <summary>
    /// 作为发起者的好友关系
    /// </summary>
    public virtual ICollection<Friendship> InitiatedFriendships { get; set; } = new List<Friendship>();

    /// <summary>
    /// 作为接受者的好友关系
    /// </summary>
    public virtual ICollection<Friendship> ReceivedFriendships { get; set; } = new List<Friendship>();
}

/// <summary>
/// 用户在线状态枚举
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// 离线
    /// </summary>
    Offline = 0,

    /// <summary>
    /// 在线
    /// </summary>
    Online = 1,

    /// <summary>
    /// 离开
    /// </summary>
    Away = 2,

    /// <summary>
    /// 忙碌
    /// </summary>
    Busy = 3,

    /// <summary>
    /// 隐身
    /// </summary>
    Invisible = 4
}
