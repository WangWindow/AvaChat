using System.ComponentModel.DataAnnotations;

namespace AvaChat.Client.Models;

public class ClientSetting
{
    [Key]
    public int Id { get; set; }

    [MaxLength(256)]
    public string ServerAddress { get; set; } = string.Empty;

    // 可扩展更多设置项
}
