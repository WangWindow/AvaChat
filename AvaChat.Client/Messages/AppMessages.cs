namespace AvaChat.Client.Messages;

/// <summary>
/// 好友选择消息
/// </summary>
public class FriendSelectedMessage
{
    public FriendInfo Friend { get; }

    public FriendSelectedMessage(FriendInfo friend)
    {
        Friend = friend;
    }
}

/// <summary>
/// 导航切换消息
/// </summary>
public class NavigationChangedMessage
{
    public NavigationType NavigationType { get; }

    public NavigationChangedMessage(NavigationType navigationType)
    {
        NavigationType = navigationType;
    }
}

/// <summary>
/// 导航类型枚举
/// </summary>
public enum NavigationType
{
    /// <summary>
    /// 聊天
    /// </summary>
    Chat,

    /// <summary>
    /// 好友
    /// </summary>
    Friends,

    /// <summary>
    /// 设置
    /// </summary>
    Settings
}
