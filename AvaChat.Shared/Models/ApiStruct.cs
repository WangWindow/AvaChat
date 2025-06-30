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
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class LoginRequest
{
    [Required]
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
    [Required]
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
    [Required]
    public string FromUserId { get; set; } = string.Empty;
    [Required]
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