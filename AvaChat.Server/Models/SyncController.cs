using AvaChat.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AvaChat.Server.Models;

[ApiController]
[Route("api/[controller]")]
public class SyncController(ServerDbContext db) : ControllerBase
{
    private readonly ServerDbContext _db = db;

    // 获取用户基本信息
    [HttpGet("user")] // /api/sync/user?userId=xxxx
    public async Task<ActionResult<UserInfo>> GetUser([FromQuery] string userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return NotFound();
        // 只返回非敏感信息
        var info = new UserInfo
        {
            UserId = user.UserId,
            UserName = user.UserName,
            Status = user.Status,
            LastLoginTime = user.LastLoginTime
        };
        return Ok(info);
    }

    // 获取用户所有好友关系（含好友详细信息）
    [HttpGet("friends")] // /api/sync/friends?userId=xxxx
    public async Task<ActionResult<List<Friendship>>> GetFriends([FromQuery] string userId)
    {
        var friends = await _db.Friendships
            .Include(f => f.FriendUser)
            .Where(f => f.UserId == userId)
            .ToListAsync();

        var result = friends.Select(f => new Friendship
        {
            FriendshipId = f.FriendshipId,
            UserId = f.UserId,
            FriendUserId = f.FriendUserId,
            CreatedAt = f.CreatedAt,
            AlterName = f.AlterName,
            FriendUser = new UserInfo
            {
                UserId = f.FriendUser.UserId,
                UserName = f.FriendUser.UserName,
                Status = f.FriendUser.Status,
                LastLoginTime = f.FriendUser.LastLoginTime
            }
        }).ToList();
        return Ok(result);
    }

    // 批量同步接口 - 登录时一次性获取所有需要的数据
    [HttpPost("batch")]
    public async Task<ActionResult<BatchSyncResponse>> BatchSync([FromBody] BatchSyncRequest req)
    {
        var response = new BatchSyncResponse { Success = true };

        try
        {
            // 1. 同步用户基本信息
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == req.UserId);
            if (user != null)
            {
                response.UserInfo = new UserInfo
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    Status = user.Status,
                    LastLoginTime = user.LastLoginTime
                };
            }

            // 2. 同步好友关系列表
            var friends = await _db.Friendships
                .Include(f => f.FriendUser)
                .Where(f => f.UserId == req.UserId)
                .ToListAsync();

            response.Friendships = friends.Select(f => new Friendship
            {
                FriendshipId = f.FriendshipId,
                UserId = f.UserId,
                FriendUserId = f.FriendUserId,
                CreatedAt = f.CreatedAt,
                AlterName = f.AlterName,
                FriendUser = new UserInfo
                {
                    UserId = f.FriendUser.UserId,
                    UserName = f.FriendUser.UserName,
                    Status = f.FriendUser.Status,
                    LastLoginTime = f.FriendUser.LastLoginTime
                }
            }).ToList();

            // 3. 同步聊天记录（根据时间戳增量同步）
            var messageQuery = _db.Messages
                .Where(m => m.SenderId == req.UserId || m.ReceiverId == req.UserId);

            if (req.LastSyncTime.HasValue)
            {
                messageQuery = messageQuery.Where(m => m.Timestamp > req.LastSyncTime.Value);
            }

            var messages = await messageQuery
                .OrderBy(m => m.Timestamp)
                .Take(1000) // 限制单次同步消息数量
                .ToListAsync();

            response.Messages = messages.Select(m => new Message
            {
                MessageId = m.MessageId,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Content = m.Content,
                Timestamp = m.Timestamp,
                MessageType = m.MessageType,
                Status = m.Status
            }).ToList();

            // 4. 同步好友申请
            var pendingRequests = await _db.FriendRequests
                .Include(r => r.FromUser)
                .Where(r => r.ToUserId == req.UserId && r.Status == FriendRequestStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            response.PendingFriendRequests = pendingRequests.Select(r => new FriendRequest
            {
                FromUserId = r.FromUserId,
                FromUserName = r.FromUser.UserName,
                Message = r.Message,
                CreatedAt = r.CreatedAt
            }).ToList();

            response.LastSyncTime = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Error = ex.Message;
        }

        return Ok(response);
    }
}