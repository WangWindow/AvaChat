using AvaChat.Shared.Models;

namespace AvaChat.Shared.Protocol;

/// <summary>
/// 聊天消息
/// </summary>
public class ChatMessage : NetworkMessage
{
    public override string Type => "ChatMessage";

    /// <summary>
    /// 发送者用户号码
    /// </summary>
    [JsonPropertyName("senderNumber")]
    public string SenderNumber { get; set; } = string.Empty;

    /// <summary>
    /// 接收者用户号码
    /// </summary>
    [JsonPropertyName("receiverNumber")]
    public string ReceiverNumber { get; set; } = string.Empty;

    /// <summary>
    /// 消息内容
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 消息类型
    /// </summary>
    [JsonPropertyName("messageType")]
    public MessageType MessageType { get; set; } = MessageType.Text;
}

/// <summary>
/// 消息送达确认
/// </summary>
public class MessageDelivered : NetworkMessage
{
    public override string Type => "MessageDelivered";

    /// <summary>
    /// 原消息ID
    /// </summary>
    [JsonPropertyName("originalMessageId")]
    public string OriginalMessageId { get; set; } = string.Empty;

    /// <summary>
    /// 接收者用户号码
    /// </summary>
    [JsonPropertyName("receiverNumber")]
    public string ReceiverNumber { get; set; } = string.Empty;
}

/// <summary>
/// 消息已读确认
/// </summary>
public class MessageRead : NetworkMessage
{
    public override string Type => "MessageRead";

    /// <summary>
    /// 原消息ID
    /// </summary>
    [JsonPropertyName("originalMessageId")]
    public string OriginalMessageId { get; set; } = string.Empty;

    /// <summary>
    /// 读取者用户号码
    /// </summary>
    [JsonPropertyName("readerNumber")]
    public string ReaderNumber { get; set; } = string.Empty;
}

/// <summary>
/// 用户状态变更通知
/// </summary>
public class UserStatusChanged : NetworkMessage
{
    public override string Type => "UserStatusChanged";

    /// <summary>
    /// 用户号码
    /// </summary>
    [JsonPropertyName("userNumber")]
    public string UserNumber { get; set; } = string.Empty;

    /// <summary>
    /// 新状态
    /// </summary>
    [JsonPropertyName("status")]
    public UserStatus Status { get; set; }

    /// <summary>
    /// 最后在线时间
    /// </summary>
    [JsonPropertyName("lastOnlineTime")]
    public DateTime LastOnlineTime { get; set; }
}
