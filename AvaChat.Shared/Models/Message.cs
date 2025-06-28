namespace AvaChat.Shared.Models;

/// <summary>
/// 消息实体模型
/// </summary>
[Table("Messages")]
public class Message
{
    /// <summary>
    /// 消息ID（主键）
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// 发送者ID
    /// </summary>
    [Required]
    public int SenderId { get; set; }

    /// <summary>
    /// 接收者ID
    /// </summary>
    [Required]
    public int ReceiverId { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    [Required]
    [StringLength(5000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 消息类型
    /// </summary>
    public MessageType Type { get; set; } = MessageType.Text;

    /// <summary>
    /// 消息状态
    /// </summary>
    public MessageStatus Status { get; set; } = MessageStatus.Sent;

    /// <summary>
    /// 发送时间
    /// </summary>
    public DateTime SentTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 送达时间
    /// </summary>
    public DateTime? DeliveredTime { get; set; }

    /// <summary>
    /// 已读时间
    /// </summary>
    public DateTime? ReadTime { get; set; }

    /// <summary>
    /// 是否已删除
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    // 导航属性
    /// <summary>
    /// 发送者
    /// </summary>
    [ForeignKey(nameof(SenderId))]
    public virtual User Sender { get; set; } = null!;

    /// <summary>
    /// 接收者
    /// </summary>
    [ForeignKey(nameof(ReceiverId))]
    public virtual User Receiver { get; set; } = null!;
}

/// <summary>
/// 消息类型枚举
/// </summary>
public enum MessageType
{
    /// <summary>
    /// 文本消息
    /// </summary>
    Text = 0,

    /// <summary>
    /// 表情符号
    /// </summary>
    Emoji = 1,

    /// <summary>
    /// 图片
    /// </summary>
    Image = 2,

    /// <summary>
    /// 文件
    /// </summary>
    File = 3,

    /// <summary>
    /// 系统通知
    /// </summary>
    System = 4
}

/// <summary>
/// 消息状态枚举
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// 发送中
    /// </summary>
    Sending = 0,

    /// <summary>
    /// 已发送
    /// </summary>
    Sent = 1,

    /// <summary>
    /// 已送达
    /// </summary>
    Delivered = 2,

    /// <summary>
    /// 已读
    /// </summary>
    Read = 3,

    /// <summary>
    /// 发送失败
    /// </summary>
    Failed = 4
}
