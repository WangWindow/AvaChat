namespace AvaChat.Shared.Models;

public class User
{
    [Key, StringLength(8), RegularExpression(@"^[0-9]+$", ErrorMessage = "用户ID为8位且只能包含数字。")]
    public string UserId { get; set; } = string.Empty;

    [Required, RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "用户名只能包含字母、数字和下划线。"),
     MinLength(3, ErrorMessage = "用户名长度必须在3到50个字符之间。"), MaxLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public UserStatus Status { get; set; } = UserStatus.Offline;

    public DateTime? LastLoginTime { get; set; }
}

public enum UserStatus
{
    Offline = 0, // 离线
    Online = 1,  // 在线
}

public class UserInfo
{
    [Key]
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public UserStatus Status { get; set; } = UserStatus.Offline;

    [JsonPropertyName("lastLoginTime")]
    public DateTime? LastLoginTime { get; set; }
}