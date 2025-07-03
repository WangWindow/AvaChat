namespace AvaChat.Shared.Models;

public class RegisterRequest
{
    [Required]
    public string UserName { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("userId"), Required, StringLength(8)]
    public string? UserId { get; set; }
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class LoginRequest
{
    [Required, StringLength(8)]
    public string UserId { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("error")]
    public string? Error { get; set; }
    [JsonPropertyName("userName")]
    public string? UserName { get; set; }
}


public class LogoutRequest
{
    [Required, StringLength(8)]
    public string UserId { get; set; } = string.Empty;
}

public class LogoutResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    public string? Error { get; set; }
    [JsonPropertyName("userName")]
    public string? UserName { get; set; }
}


public class SendMessageRequest
{
    [Required, StringLength(8)]
    public string FromUserId { get; set; } = string.Empty;
    [Required, StringLength(8)]
    public string ToUserId { get; set; } = string.Empty;
    [Required]
    public string Content { get; set; } = string.Empty;
}
public class SendMessageResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

// 好友申请相关API结构
public class AddFriendRequest
{
    [Required, StringLength(8)]
    public string FromUserId { get; set; } = string.Empty;
    [Required, StringLength(8)]
    public string ToUserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty; // 申请消息
}

public class AddFriendResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class HandleFriendRequestRequest
{
    [Required, StringLength(8)]
    public string UserId { get; set; } = string.Empty;
    [Required, StringLength(8)]
    public string FromUserId { get; set; } = string.Empty;
    [Required]
    public bool Accept { get; set; } // true=接受，false=拒绝
}

public class HandleFriendRequestResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class GetPendingFriendRequestsResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("requests")]
    public List<FriendRequest> Requests { get; set; } = new();
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class FriendRequest
{
    [Key]
    public int RequestId { get; set; }

    [JsonPropertyName("fromUserId"), Required, StringLength(8)]
    public string FromUserId { get; set; } = string.Empty;

    [JsonPropertyName("toUserId"), Required, StringLength(8)]
    public string ToUserId { get; set; } = string.Empty;

    [JsonPropertyName("fromUserName")]
    public string FromUserName { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; [JsonPropertyName("status")]
    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;
}

public enum FriendRequestStatus
{
    Pending = 0,   // 待处理
    Accepted = 1,  // 已接受
    Rejected = 2   // 已拒绝
}
