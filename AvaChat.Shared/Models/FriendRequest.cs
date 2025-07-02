namespace AvaChat.Shared.Models;

public class FriendRequestEntity
{
    [Key]
    public int RequestId { get; set; }

    [Required, StringLength(8)]
    public string FromUserId { get; set; } = string.Empty;

    [Required, StringLength(8)]
    public string ToUserId { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

    // 导航属性
    public User FromUser { get; set; } = new();
    public User ToUser { get; set; } = new();
}

public enum FriendRequestStatus
{
    Pending = 0,   // 待处理
    Accepted = 1,  // 已接受
    Rejected = 2   // 已拒绝
}
