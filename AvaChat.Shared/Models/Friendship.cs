namespace AvaChat.Shared.Models;

public class Friendship
{
    [Required, StringLength(8)]
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(8)]
    [JsonPropertyName("friendUserId")]
    public string FriendUserId { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 临时属性，用于在客户端显示好友信息
    [NotMapped]
    [JsonPropertyName("friendUser")]
    public UserInfo? FriendUser { get; set; }
}
