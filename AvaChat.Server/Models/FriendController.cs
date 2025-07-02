using AvaChat.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AvaChat.Server.Models;

[ApiController]
[Route("api/[controller]")]
public class FriendController(ServerDbContext db) : ControllerBase
{
    private readonly ServerDbContext _db = db;

    // 发送好友申请
    [HttpPost("request")]
    public async Task<ActionResult<AddFriendResponse>> SendFriendRequest([FromBody] AddFriendRequest req)
    {
        // 检查用户是否存在
        var fromUser = await _db.Users.FindAsync(req.FromUserId);
        var toUser = await _db.Users.FindAsync(req.ToUserId);
        if (fromUser == null || toUser == null)
        {
            return Ok(new AddFriendResponse { Success = false, Error = "用户不存在" });
        }

        // 不能加自己为好友
        if (req.FromUserId == req.ToUserId)
        {
            return Ok(new AddFriendResponse { Success = false, Error = "不能添加自己为好友" });
        }

        // 检查是否已经是好友
        var existingFriendship = await _db.Friendships.AnyAsync(f =>
            f.UserId == req.FromUserId && f.FriendUserId == req.ToUserId);
        if (existingFriendship)
        {
            return Ok(new AddFriendResponse { Success = false, Error = "已经是好友关系" });
        }

        // 检查是否已有待处理的申请
        var existingRequest = await _db.FriendRequests.AnyAsync(r =>
            r.FromUserId == req.FromUserId && r.ToUserId == req.ToUserId && r.Status == FriendRequestStatus.Pending);
        if (existingRequest)
        {
            return Ok(new AddFriendResponse { Success = false, Error = "已发送好友申请，请等待对方回应" });
        }

        // 创建好友申请
        var friendRequest = new FriendRequestEntity
        {
            FromUserId = req.FromUserId,
            ToUserId = req.ToUserId,
            Message = req.Message,
            CreatedAt = DateTime.UtcNow,
            Status = FriendRequestStatus.Pending
        };

        _db.FriendRequests.Add(friendRequest);
        await _db.SaveChangesAsync();

        return Ok(new AddFriendResponse { Success = true });
    }

    // 处理好友申请（接受或拒绝）
    [HttpPost("handle")]
    public async Task<ActionResult<HandleFriendRequestResponse>> HandleFriendRequest([FromBody] HandleFriendRequestRequest req)
    {
        // 查找待处理的好友申请
        var friendRequest = await _db.FriendRequests
            .Include(r => r.FromUser)
            .FirstOrDefaultAsync(r => r.FromUserId == req.FromUserId && r.ToUserId == req.UserId && r.Status == FriendRequestStatus.Pending);

        if (friendRequest == null)
        {
            return Ok(new HandleFriendRequestResponse { Success = false, Error = "未找到待处理的好友申请" });
        }

        if (req.Accept)
        {
            // 接受好友申请 - 建立双向好友关系
            var friendship1 = new Friendship
            {
                UserId = req.UserId,
                FriendUserId = req.FromUserId,
                AlterName = friendRequest.FromUser.UserName,
                CreatedAt = DateTime.UtcNow
            };

            var friendship2 = new Friendship
            {
                UserId = req.FromUserId,
                FriendUserId = req.UserId,
                AlterName = (await _db.Users.FindAsync(req.UserId))?.UserName ?? "",
                CreatedAt = DateTime.UtcNow
            };

            _db.Friendships.AddRange(friendship1, friendship2);
            friendRequest.Status = FriendRequestStatus.Accepted;
        }
        else
        {
            // 拒绝好友申请
            friendRequest.Status = FriendRequestStatus.Rejected;
        }

        await _db.SaveChangesAsync();
        return Ok(new HandleFriendRequestResponse { Success = true });
    }

    // 获取待处理的好友申请列表
    [HttpGet("pending")]
    public async Task<ActionResult<GetPendingFriendRequestsResponse>> GetPendingRequests([FromQuery] string userId)
    {
        var requests = await _db.FriendRequests
            .Include(r => r.FromUser)
            .Where(r => r.ToUserId == userId && r.Status == FriendRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var result = requests.Select(r => new FriendRequest
        {
            FromUserId = r.FromUserId,
            FromUserName = r.FromUser.UserName,
            Message = r.Message,
            CreatedAt = r.CreatedAt
        }).ToList();

        return Ok(new GetPendingFriendRequestsResponse { Success = true, Requests = result });
    }

    // 搜索用户（用于添加好友时搜索）
    [HttpGet("search")]
    public async Task<ActionResult<List<UserInfo>>> SearchUsers([FromQuery] string query, [FromQuery] string currentUserId)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Ok(new List<UserInfo>());
        }

        var users = await _db.Users
            .Where(u => u.UserId != currentUserId && u.UserId != "00000000" && // 排除自己和系统账号
                       (u.UserName.Contains(query) || u.UserId.Contains(query)))
            .Take(20) // 限制返回数量
            .ToListAsync();

        var result = users.Select(u => new UserInfo
        {
            UserId = u.UserId,
            UserName = u.UserName,
            Status = u.Status,
            LastLoginTime = u.LastLoginTime
        }).ToList();

        return Ok(result);
    }
}
