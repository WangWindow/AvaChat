using System;
using System.ComponentModel.DataAnnotations;

namespace AvaChat.Client.Models;

public class LoginInfo
{
    [Key, Required, MaxLength(8), MinLength(8)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? Password { get; set; }

    [MaxLength(64)]
    public string? UserName { get; set; }

    public DateTime LoginTime { get; set; }

    public bool HasSavedPassword => !string.IsNullOrEmpty(Password);
}
