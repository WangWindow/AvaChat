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
    public bool Success { get; set; }
    public string? UserId { get; set; }
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
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? UserName { get; set; }
}
