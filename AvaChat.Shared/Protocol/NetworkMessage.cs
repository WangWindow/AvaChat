namespace AvaChat.Shared.Protocol;

/// <summary>
/// 网络消息基类
/// </summary>
public abstract class NetworkMessage
{
    /// <summary>
    /// 消息类型
    /// </summary>
    [JsonPropertyName("type")]
    public abstract string Type { get; }

    /// <summary>
    /// 消息ID（用于追踪响应）
    /// </summary>
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 时间戳
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 序列化消息为 JSON
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, GetType(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// 从 JSON 反序列化消息
    /// </summary>
    public static T? FromJson<T>(string json) where T : NetworkMessage
    {
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
