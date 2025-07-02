using System;
using System.ComponentModel.DataAnnotations;

namespace AvaChat.Client.Models;

public class LoginInfo
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(32)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? Password { get; set; }

    public DateTime LoginTime { get; set; }

    public string DisplayText => $"{UserId} ({LoginTime:MM-dd HH:mm})";

    public bool HasSavedPassword => !string.IsNullOrEmpty(Password);
}
