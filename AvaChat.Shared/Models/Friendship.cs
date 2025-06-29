namespace AvaChat.Shared.Models;

public class Friendship
{
    [Key]
    public int FriendshipId { get; set; }

    [Required, StringLength(8)]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required, StringLength(8)]
    public string FriendUserId { get; set; } = string.Empty;

    [ForeignKey(nameof(FriendUserId))]
    public User FriendUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string AlterName { get; set; } = string.Empty;
}
