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

    // 获取用户所有好友关系（含好友User信息）
    [HttpGet("friends")] // /api/sync/friends?userId=xxxx
    public async Task<ActionResult<List<Friendship>>> GetFriends([FromQuery] string userId)
    {
        var friends = await _db.Friendships
            .Where(f => f.UserId == userId)
            .ToListAsync();
        var result = friends.Select(f => new Friendship
        {
            FriendshipId = f.FriendshipId,
            UserId = f.UserId,
            FriendUserId = f.FriendUserId,
            CreatedAt = f.CreatedAt,
            AlterName = f.AlterName,
        }).ToList();
        return Ok(result);
    }
}