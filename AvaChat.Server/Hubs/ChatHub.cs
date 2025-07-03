using AvaChat.Shared.Models;
using Microsoft.AspNetCore.SignalR;

namespace AvaChat.Server.Hubs;

/// <summary>
/// SignalR ChatHub - 处理实时消息通信
/// </summary>
public class ChatHub(ServerDbContext dbContext) : Hub
{
    private readonly ServerDbContext _dbContext = dbContext;

    /// <summary>
    /// 存储用户连接信息的静态字典
    /// </summary>
    private static readonly Dictionary<string, string> UserConnections = [];    /// <summary>
                                                                                /// 用户上线时调用，将用户ID与连接ID关联
                                                                                /// </summary>
    public async Task UserConnected(string userId)
    {
        // 存储用户连接
        UserConnections[userId] = Context.ConnectionId;

        // 将连接添加到用户组
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);

        // 更新用户状态为在线
        var user = await _dbContext.Users.FindAsync(userId);
        if (user != null)
        {
            user.Status = UserStatus.Online;
            user.LastLoginTime = DateTime.Now;
            await _dbContext.SaveChangesAsync();

            // 通知该用户的所有好友其已上线
            await NotifyFriendsStatusChange(userId, UserStatus.Online);
        }
    }    /// <summary>
         /// 用户断开连接时自动调用
         /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // 找到断开连接的用户
        var userId = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;

        if (!string.IsNullOrEmpty(userId))
        {
            // 更新用户状态为离线
            var user = await _dbContext.Users.FindAsync(userId);
            if (user != null)
            {
                user.Status = UserStatus.Offline;
                await _dbContext.SaveChangesAsync();

                // 从连接字典中移除
                UserConnections.Remove(userId);

                // 从用户组中移除
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);

                // 通知该用户的所有好友其已下线
                await NotifyFriendsStatusChange(userId, UserStatus.Offline);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 发送私人消息
    /// </summary>
    public async Task SendPrivateMessage(string senderId, string receiverId, string content)
    {
        // 验证发送者和接收者是否存在
        var sender = await _dbContext.Users.FindAsync(senderId);
        var receiver = await _dbContext.Users.FindAsync(receiverId);

        if (sender == null || receiver == null)
        {
            await Clients.Caller.SendAsync("ReceiveError", "用户不存在");
            return;
        }

        // 验证好友关系
        var areFriends = await _dbContext.Friendships.AnyAsync(f =>
            f.UserId == senderId && f.FriendUserId == receiverId);

        if (!areFriends)
        {
            await Clients.Caller.SendAsync("ReceiveError", "不是好友关系");
            return;
        }

        // 创建消息对象
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            Timestamp = DateTime.Now,
            MessageType = MessageType.Text,
            Status = MessageStatus.Delivered
        };

        // 保存消息到数据库
        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        // 立即更新消息ID
        message.MessageId = _dbContext.Messages
            .OrderByDescending(m => m.MessageId)
            .First(m => m.SenderId == senderId && m.ReceiverId == receiverId && m.Content == content)
            .MessageId;

        // 向发送者确认消息已发送
        await Clients.Caller.SendAsync("MessageSent", message);

        // 如果接收者在线，直接发送消息
        if (UserConnections.TryGetValue(receiverId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveMessage", message);
        }
    }

    /// <summary>
    /// 发送私人系统消息（只有指定用户能看到）
    /// </summary>
    public async Task SendPrivateSystemMessage(string receiverId, string content)
    {
        var receiver = await _dbContext.Users.FindAsync(receiverId);
        if (receiver == null)
        {
            return;
        }

        // 确保系统用户存在
        await EnsureSystemUser();

        // 创建私人系统消息
        var message = new Message
        {
            SenderId = "system",
            ReceiverId = receiverId,
            Content = content,
            Timestamp = DateTime.Now,
            MessageType = MessageType.System,
            Status = MessageStatus.Delivered
        };

        // 保存消息到数据库
        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        // 只发送给指定用户
        if (UserConnections.TryGetValue(receiverId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveSystemMessage", message);
        }
    }

    /// <summary>
    /// 广播公共系统消息给所有在线用户（如系统维护通知等）
    /// </summary>
    public async Task BroadcastPublicSystemMessage(string content)
    {
        var timestamp = DateTime.Now;

        // 确保系统用户存在
        await EnsureSystemUser();

        // 创建系统消息列表，每个在线用户一条
        var messages = new List<Message>();

        foreach (var userId in UserConnections.Keys)
        {
            var message = new Message
            {
                SenderId = "system",
                ReceiverId = userId,
                Content = content,
                Timestamp = timestamp,
                MessageType = MessageType.System,
                Status = MessageStatus.Delivered
            };

            messages.Add(message);
        }

        // 保存消息到数据库
        _dbContext.Messages.AddRange(messages);
        await _dbContext.SaveChangesAsync();

        // 向所有连接的用户发送系统消息
        await Clients.All.SendAsync("ReceiveSystemMessage", new Message
        {
            SenderId = "system",
            Content = content,
            Timestamp = timestamp,
            MessageType = MessageType.System,
            Status = MessageStatus.Delivered
        });

        // 记录系统广播消息
        Console.WriteLine($"[{timestamp:yyyy-MM-dd HH:mm:ss}] 公共系统广播: {content}");
    }

    /// <summary>
    /// 为服务器UI调用提供广播公共系统消息的静态方法
    /// </summary>
    public static async Task SendServerBroadcast(IHubContext<ChatHub> hubContext, ServerDbContext dbContext, string content)
    {
        try
        {
            var timestamp = DateTime.Now;

            // 确保系统用户存在
            var systemUser = await dbContext.Users.FindAsync("system");
            if (systemUser == null)
            {
                systemUser = new User
                {
                    UserId = "system",
                    UserName = "系统",
                    Password = "",
                    Status = UserStatus.Online,
                    LastLoginTime = DateTime.UtcNow
                };
                dbContext.Users.Add(systemUser);
                await dbContext.SaveChangesAsync();
            }

            // 创建系统消息列表，每个在线用户一条
            var messages = new List<Message>();
            var onlineUsers = await dbContext.Users.Where(u => u.Status == UserStatus.Online).ToListAsync();

            foreach (var user in onlineUsers)
            {
                var message = new Message
                {
                    SenderId = "system",
                    ReceiverId = user.UserId,
                    Content = content,
                    Timestamp = timestamp,
                    MessageType = MessageType.System,
                    Status = MessageStatus.Delivered
                };

                messages.Add(message);
            }

            // 保存消息到数据库
            dbContext.Messages.AddRange(messages);
            await dbContext.SaveChangesAsync();

            // 向所有客户端广播
            await hubContext.Clients.All.SendAsync("ReceiveSystemMessage", new Message
            {
                SenderId = "system",
                Content = content,
                Timestamp = timestamp,
                MessageType = MessageType.System,
                Status = MessageStatus.Delivered
            });

            Console.WriteLine($"[{timestamp:yyyy-MM-dd HH:mm:ss}] 服务器公共广播: {content}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] 发送系统广播失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 为其他控制器提供发送私人系统消息的静态方法
    /// </summary>
    public static async Task SendPrivateSystemMessage(IHubContext<ChatHub> hubContext, ServerDbContext dbContext, string receiverId, string content)
    {
        try
        {
            var receiver = await dbContext.Users.FindAsync(receiverId);
            if (receiver == null)
            {
                Console.WriteLine($"[SendPrivateSystemMessage] 用户不存在: {receiverId}");
                return;
            }

            // 确保系统用户存在
            var systemUser = await dbContext.Users.FindAsync("system");
            if (systemUser == null)
            {
                systemUser = new User
                {
                    UserId = "system",
                    UserName = "系统",
                    Password = "",
                    Status = UserStatus.Online,
                    LastLoginTime = DateTime.UtcNow
                };
                dbContext.Users.Add(systemUser);
                await dbContext.SaveChangesAsync();
            }

            // 创建私人系统消息
            var message = new Message
            {
                SenderId = "system",
                ReceiverId = receiverId,
                Content = content,
                Timestamp = DateTime.UtcNow,
                MessageType = MessageType.System,
                Status = MessageStatus.Delivered
            };

            // 保存消息到数据库
            dbContext.Messages.Add(message);
            await dbContext.SaveChangesAsync();

            // 只发送给指定用户（如果在线）
            await hubContext.Clients.Group(receiverId).SendAsync("ReceiveSystemMessage", message);

            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] 私人系统消息发送给 {receiverId}: {content}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] 发送私人系统消息失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 通知好友状态变化
    /// </summary>
    private async Task NotifyFriendsStatusChange(string userId, UserStatus status)
    {
        // 获取所有该用户的好友
        var friendships = await _dbContext.Friendships
            .Where(f => f.FriendUserId == userId)
            .ToListAsync();

        foreach (var friendship in friendships)
        {
            if (UserConnections.TryGetValue(friendship.UserId, out var connectionId))
            {
                // 发送好友状态更新通知
                await Clients.Client(connectionId).SendAsync("FriendStatusChanged", userId, status);
            }
        }
    }

    /// <summary>
    /// 确保系统用户存在
    /// </summary>
    private async Task EnsureSystemUser()
    {
        var systemUser = await _dbContext.Users.FindAsync("system");
        if (systemUser == null)
        {
            systemUser = new User
            {
                UserId = "system",
                UserName = "系统",
                Password = "", // 系统用户无需密码
                Status = UserStatus.Online,
                LastLoginTime = DateTime.UtcNow
            };

            _dbContext.Users.Add(systemUser);
            await _dbContext.SaveChangesAsync();
        }
    }
}
