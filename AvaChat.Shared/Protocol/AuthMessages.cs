namespace AvaChat.Shared.Protocol;

/// <summary>
/// 登录请求消息
/// </summary>
public class LoginRequest : NetworkMessage
{
    public override string Type => "LoginRequest";

    /// <summary>
    /// 用户号码
    /// </summary>
    [JsonPropertyName("userNumber")]
    public string UserNumber { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 登录响应消息
/// </summary>
public class LoginResponse : NetworkMessage
{
    public override string Type => "LoginResponse";

    /// <summary>
    /// 是否成功
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 用户信息
    /// </summary>
    [JsonPropertyName("userInfo")]
    public UserInfo? UserInfo { get; set; }
}

/// <summary>
/// 注册请求消息
/// </summary>
public class RegisterRequest : NetworkMessage
{
    public override string Type => "RegisterRequest";

    /// <summary>
    /// 昵称
    /// </summary>
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 注册响应消息
/// </summary>
public class RegisterResponse : NetworkMessage
{
    public override string Type => "RegisterResponse";

    /// <summary>
    /// 是否成功
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 生成的用户号码
    /// </summary>
    [JsonPropertyName("userNumber")]
    public string? UserNumber { get; set; }
}
