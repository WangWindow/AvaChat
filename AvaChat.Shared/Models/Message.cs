namespace AvaChat.Shared.Models;

public class Message
{
    [Key]
    public int MessageId { get; set; }

    [Required, StringLength(8)]
    public string SenderId { get; set; } = string.Empty;

    [ForeignKey(nameof(SenderId))]
    public User Sender { get; set; } = null!;

    [Required, StringLength(8)]
    public string ReceiverId { get; set; } = string.Empty;

    [ForeignKey(nameof(ReceiverId))]
    public User Receiver { get; set; } = null!;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public MessageStatus Status { get; set; } = MessageStatus.Sending;
    public MessageType MessageType { get; set; } = MessageType.Text;
}

public enum MessageStatus
{
    Sending = 0, // 发送中
    Delivered = 1, // 已送达
    Failed = 2, // 发送失败
}

public enum NewMessageStatus
{
    Unread = 0, // 未读
    Read = 1, // 已读
    Deleted = 2 // 已删除
}

public enum MessageType
{
    Text = 0, // 文本消息
    Image = 1, // 图片消息
    File = 2,  // 文件消息
    System = 3, // 系统消息
}
