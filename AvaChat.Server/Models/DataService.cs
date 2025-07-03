using Microsoft.EntityFrameworkCore;

namespace AvaChat.Server.Models;

/// <summary>
/// 数据访问服务 - 提供优化的数据查询方法
/// </summary>
public class DataService
{
    private readonly ServerDbContext _context;

    public DataService(ServerDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 分页获取消息历史
    /// </summary>
    public async Task<List<Message>> GetMessageHistoryAsync(string userId1, string userId2, int page = 0, int pageSize = 50)
    {
        return await _context.Messages
            .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                       (m.SenderId == userId2 && m.ReceiverId == userId1))
            .OrderByDescending(m => m.Timestamp)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// 获取用户的所有好友关系（优化查询）
    /// </summary>
    public async Task<List<Friendship>> GetUserFriendshipsAsync(string userId)
    {
        return await _context.Friendships
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.FriendUserId)
            .ToListAsync();
    }

    /// <summary>
    /// 获取活跃用户列表
    /// </summary>
    public async Task<List<User>> GetActiveUsersAsync(int days = 7)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        return await _context.Users
            .Where(u => u.Status == UserStatus.Online || u.LastLoginTime > cutoffDate)
            .OrderByDescending(u => u.LastLoginTime)
            .ToListAsync();
    }

    /// <summary>
    /// 获取用户未读消息数量
    /// </summary>
    public async Task<int> GetUnreadMessageCountAsync(string userId)
    {
        return await _context.Messages
            .CountAsync(m => m.ReceiverId == userId && m.Status == MessageStatus.Delivered);
    }

    /// <summary>
    /// 批量标记消息为已读
    /// </summary>
    public async Task<int> MarkMessagesAsReadAsync(string senderId, string receiverId)
    {
        var messages = await _context.Messages
            .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId && m.Status == MessageStatus.Delivered)
            .ToListAsync();

        foreach (var message in messages)
        {
            message.Status = MessageStatus.Delivered; // 暂时用Delivered表示已读，后续可扩展MessageStatus
        }

        return await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 搜索用户（支持用户名和用户ID）
    /// </summary>
    public async Task<List<User>> SearchUsersAsync(string query, string currentUserId, int maxResults = 20)
    {
        var lowerQuery = query.ToLowerInvariant();
        return await _context.Users
            .Where(u => u.UserId != currentUserId &&
                       u.UserId != "system" &&
                       (u.UserName.ToLower().Contains(lowerQuery) || u.UserId.Contains(lowerQuery)))
            .Take(maxResults)
            .ToListAsync();
    }

    /// <summary>
    /// 获取用户统计信息
    /// </summary>
    public async Task<UserStatsInfo> GetUserStatsAsync(string userId)
    {
        var sentCount = await _context.Messages
            .CountAsync(m => m.SenderId == userId && m.MessageType != MessageType.System);

        var receivedCount = await _context.Messages
            .CountAsync(m => m.ReceiverId == userId && m.MessageType != MessageType.System);

        var friendCount = await _context.Friendships
            .CountAsync(f => f.UserId == userId);

        var lastMessage = await _context.Messages
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync();

        return new UserStatsInfo
        {
            UserId = userId,
            MessagesSent = sentCount,
            MessagesReceived = receivedCount,
            FriendCount = friendCount,
            LastActivityTime = lastMessage?.Timestamp
        };
    }

    /// <summary>
    /// 清理过期数据
    /// </summary>
    public async Task<int> CleanupExpiredDataAsync(int daysToKeep = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

        // 删除过期的系统消息
        var expiredMessages = await _context.Messages
            .Where(m => m.MessageType == MessageType.System && m.Timestamp < cutoffDate)
            .ToListAsync();

        _context.Messages.RemoveRange(expiredMessages);

        // 删除过期的已处理好友申请
        var expiredRequests = await _context.FriendRequests
            .Where(r => r.Status != FriendRequestStatus.Pending && r.CreatedAt < cutoffDate)
            .ToListAsync();

        _context.FriendRequests.RemoveRange(expiredRequests);

        return await _context.SaveChangesAsync();
    }
}

/// <summary>
/// 用户统计信息
/// </summary>
public class UserStatsInfo
{
    public string UserId { get; set; } = string.Empty;
    public int MessagesSent { get; set; }
    public int MessagesReceived { get; set; }
    public int FriendCount { get; set; }
    public DateTime? LastActivityTime { get; set; }
}
