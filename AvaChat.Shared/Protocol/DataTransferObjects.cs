using AvaChat.Shared.Models;

namespace AvaChat.Shared.Protocol;

/// <summary>
/// 用户信息DTO
/// </summary>
public class UserInfo
{
    /// <summary>
    /// 用户号码
    /// </summary>
    [JsonPropertyName("userNumber")]
    public string UserNumber { get; set; } = string.Empty;

    /// <summary>
    /// 昵称
    /// </summary>
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// 个性签名
    /// </summary>
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// 头像路径
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    /// <summary>
    /// 在线状态
    /// </summary>
    [JsonPropertyName("status")]
    public UserStatus Status { get; set; }

    /// <summary>
    /// 最后在线时间
    /// </summary>
    [JsonPropertyName("lastOnlineTime")]
    public DateTime LastOnlineTime { get; set; }
}

/// <summary>
/// 好友信息DTO
/// </summary>
public class FriendInfo : UserInfo
{
    /// <summary>
    /// 备注名
    /// </summary>
    [JsonPropertyName("remark")]
    public string? Remark { get; set; }

    /// <summary>
    /// 好友关系状态
    /// </summary>
    [JsonPropertyName("friendshipStatus")]
    public FriendshipStatus FriendshipStatus { get; set; }
}

/// <summary>
/// 消息信息DTO
/// </summary>
public class MessageInfo
{
    /// <summary>
    /// 消息ID
    /// </summary>
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

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
    public MessageType MessageType { get; set; }

    /// <summary>
    /// 消息状态
    /// </summary>
    [JsonPropertyName("status")]
    public MessageStatus Status { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    [JsonPropertyName("sentTime")]
    public DateTime SentTime { get; set; }

    /// <summary>
    /// 送达时间
    /// </summary>
    [JsonPropertyName("deliveredTime")]
    public DateTime? DeliveredTime { get; set; }

    /// <summary>
    /// 已读时间
    /// </summary>
    [JsonPropertyName("readTime")]
    public DateTime? ReadTime { get; set; }
}
