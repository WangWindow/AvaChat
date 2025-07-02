namespace AvaChat.Shared.Models;

/// <summary>
/// 批量同步请求
/// </summary>
public class BatchSyncRequest
{
    public string UserId { get; set; } = string.Empty;
    public DateTime? LastSyncTime { get; set; }
}

/// <summary>
/// 批量同步响应
/// </summary>
public class BatchSyncResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public UserInfo? UserInfo { get; set; }
    public List<Friendship> Friendships { get; set; } = new();
    public List<Message> Messages { get; set; } = new();
    public List<FriendRequest> PendingFriendRequests { get; set; } = new();
    public DateTime LastSyncTime { get; set; }
}
