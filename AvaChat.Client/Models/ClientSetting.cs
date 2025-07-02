using System.ComponentModel.DataAnnotations;

namespace AvaChat.Client.Models;

public class ClientSetting
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string Value { get; set; } = string.Empty;
}
