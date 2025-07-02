namespace AvaChat.Shared.Models;

public class Friendship
{
    [Key]
    public int FriendshipId { get; set; }

    [Required, StringLength(8)]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(8)]
    public string FriendUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public UserInfo FriendUser { get; set; } = new UserInfo();
}
