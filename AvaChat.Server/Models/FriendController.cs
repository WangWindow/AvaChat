using AvaChat.Server.Hubs;
using AvaChat.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AvaChat.Server.Models;

[ApiController]
[Route("api/[controller]")]
public class FriendController(ServerDbContext db, IHubContext<ChatHub> hubContext) : ControllerBase
{
    private readonly ServerDbContext _db = db;
    private readonly IHubContext<ChatHub> _hubContext = hubContext;

    // 发送好友申请
    [HttpPost("request")]
    public async Task<ActionResult<AddFriendResponse>> SendFriendRequest([FromBody] AddFriendRequest req)
    {
        try
        {
            Console.WriteLine($"[SendFriendRequest] 收到请求: FromUserId={req.FromUserId}, ToUserId={req.ToUserId}, Message={req.Message}");

            // 检查用户是否存在
            var fromUser = await _db.Users.FindAsync(req.FromUserId);
            var toUser = await _db.Users.FindAsync(req.ToUserId);

            if (fromUser == null || toUser == null)
            {
                Console.WriteLine($"[SendFriendRequest] 用户不存在: fromUser={fromUser != null}, toUser={toUser != null}");
                return Ok(new AddFriendResponse { Success = false, Error = "用户不存在" });
            }

            // 不能加自己为好友
            if (req.FromUserId == req.ToUserId)
            {
                Console.WriteLine($"[SendFriendRequest] 不能添加自己为好友: FromUserId={req.FromUserId}");
                return Ok(new AddFriendResponse { Success = false, Error = "不能添加自己为好友" });
            }

            // 检查是否已经是好友
            var existingFriendship = await _db.Friendships.AnyAsync(f =>
                f.UserId == req.FromUserId && f.FriendUserId == req.ToUserId);

            if (existingFriendship)
            {
                Console.WriteLine($"[SendFriendRequest] 已经是好友关系: FromUserId={req.FromUserId}, ToUserId={req.ToUserId}");
                return Ok(new AddFriendResponse { Success = false, Error = "已经是好友关系" });
            }

            // 检查是否已有待处理的申请
            var existingRequest = await _db.FriendRequests.AnyAsync(r =>
                r.FromUserId == req.FromUserId && r.ToUserId == req.ToUserId && r.Status == FriendRequestStatus.Pending);

            if (existingRequest)
            {
                Console.WriteLine($"[SendFriendRequest] 已发送好友申请: FromUserId={req.FromUserId}, ToUserId={req.ToUserId}");
                return Ok(new AddFriendResponse { Success = false, Error = "已发送好友申请，请等待对方回应" });
            }

            // 创建好友申请
            var friendRequest = new FriendRequest
            {
                FromUserId = req.FromUserId,
                ToUserId = req.ToUserId,
                FromUserName = fromUser.UserName,
                Message = req.Message,
                CreatedAt = DateTime.UtcNow,
                Status = FriendRequestStatus.Pending,
            };

            _db.FriendRequests.Add(friendRequest);
            Console.WriteLine($"[SendFriendRequest] 创建好友申请: FromUserId={req.FromUserId}, ToUserId={req.ToUserId}, FromUserName={fromUser.UserName}");

            Console.WriteLine($"[SendFriendRequest] 准备保存到数据库");
            await _db.SaveChangesAsync();
            Console.WriteLine($"[SendFriendRequest] 成功保存到数据库");

            // 使用私人系统消息方法发送通知
            try
            {
                await ChatHub.SendPrivateSystemMessage(_hubContext, _db, req.FromUserId, $"好友申请已发送给 {toUser.UserName}，请等待对方回应");
                Console.WriteLine($"[SendFriendRequest] 已通过SignalR发送私人系统消息");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SendFriendRequest] 发送SignalR消息失败: {ex.Message}");
            }

            return Ok(new AddFriendResponse { Success = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SendFriendRequest] 异常: {ex.Message}");
            Console.WriteLine($"[SendFriendRequest] 堆栈跟踪: {ex.StackTrace}");
            return StatusCode(500, new AddFriendResponse { Success = false, Error = $"服务器错误: {ex.Message}" });
        }
    }

    // 处理好友申请（接受或拒绝）
    [HttpPost("handle")]
    public async Task<ActionResult<HandleFriendRequestResponse>> HandleFriendRequest([FromBody] HandleFriendRequestRequest req)
    {
        try
        {
            Console.WriteLine($"[HandleFriendRequest] 收到请求: UserId={req.UserId}, FromUserId={req.FromUserId}, Accept={req.Accept}");

            // 查找待处理的好友申请
            var friendRequest = await _db.FriendRequests
                .FirstOrDefaultAsync(r => r.FromUserId == req.FromUserId && r.ToUserId == req.UserId && r.Status == FriendRequestStatus.Pending);

            if (friendRequest == null)
            {
                Console.WriteLine($"[HandleFriendRequest] 未找到待处理的好友申请: FromUserId={req.FromUserId}, ToUserId={req.UserId}");
                return Ok(new HandleFriendRequestResponse { Success = false, Error = "未找到待处理的好友申请" });
            }

            // 获取发送者和接收者信息
            var fromUser = await _db.Users.FindAsync(req.FromUserId);
            var toUser = await _db.Users.FindAsync(req.UserId);

            if (fromUser == null || toUser == null)
            {
                Console.WriteLine($"[HandleFriendRequest] 用户信息不存在: fromUser={fromUser != null}, toUser={toUser != null}");
                return Ok(new HandleFriendRequestResponse { Success = false, Error = "用户信息不存在" });
            }

            if (req.Accept)
            {
                Console.WriteLine($"[HandleFriendRequest] 准备接受好友申请");

                // 检查是否已经是好友关系（防止重复添加）
                var existingFriendship = await _db.Friendships.AnyAsync(f =>
                    (f.UserId == req.UserId && f.FriendUserId == req.FromUserId) ||
                    (f.UserId == req.FromUserId && f.FriendUserId == req.UserId));

                if (existingFriendship)
                {
                    Console.WriteLine($"[HandleFriendRequest] 已经是好友关系");
                    friendRequest.Status = FriendRequestStatus.Accepted;
                    await _db.SaveChangesAsync();
                    return Ok(new HandleFriendRequestResponse { Success = true });
                }

                // 接受好友申请 - 建立双向好友关系
                var friendship1 = new Friendship
                {
                    UserId = req.UserId,
                    FriendUserId = req.FromUserId,
                    CreatedAt = DateTime.UtcNow,
                    FriendUser = new UserInfo
                    {
                        UserId = fromUser.UserId,
                        UserName = fromUser.UserName,
                        Status = fromUser.Status,
                        LastLoginTime = fromUser.LastLoginTime
                    }
                };

                var friendship2 = new Friendship
                {
                    UserId = req.FromUserId,
                    FriendUserId = req.UserId,
                    CreatedAt = DateTime.UtcNow,
                    FriendUser = new UserInfo
                    {
                        UserId = toUser.UserId,
                        UserName = toUser.UserName,
                        Status = toUser.Status,
                        LastLoginTime = toUser.LastLoginTime
                    }
                };

                _db.Friendships.AddRange(friendship1, friendship2);
                friendRequest.Status = FriendRequestStatus.Accepted;

                Console.WriteLine($"[HandleFriendRequest] 已添加好友关系到数据库");

                Console.WriteLine($"[HandleFriendRequest] 准备保存到数据库");
                await _db.SaveChangesAsync();
                Console.WriteLine($"[HandleFriendRequest] 成功保存到数据库");

                // 通过私人系统消息发送通知
                try
                {
                    // 通知申请接收者
                    await ChatHub.SendPrivateSystemMessage(_hubContext, _db, req.UserId, $"您已与 {friendRequest.FromUserName} 成为好友");

                    // 通知申请发送者
                    await ChatHub.SendPrivateSystemMessage(_hubContext, _db, req.FromUserId, $"{toUser.UserName} 已接受您的好友申请");

                    Console.WriteLine($"[HandleFriendRequest] 已通过SignalR发送私人系统消息");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HandleFriendRequest] 发送SignalR消息失败: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"[HandleFriendRequest] 准备拒绝好友申请");

                // 拒绝好友申请
                friendRequest.Status = FriendRequestStatus.Rejected;

                Console.WriteLine($"[HandleFriendRequest] 准备保存拒绝信息到数据库");
                await _db.SaveChangesAsync();
                Console.WriteLine($"[HandleFriendRequest] 成功保存拒绝信息到数据库");

                // 通过私人系统消息发送通知
                try
                {
                    await ChatHub.SendPrivateSystemMessage(_hubContext, _db, req.FromUserId, $"{toUser.UserName} 已拒绝您的好友申请");
                    Console.WriteLine($"[HandleFriendRequest] 已通过SignalR发送拒绝通知");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HandleFriendRequest] 发送SignalR消息失败: {ex.Message}");
                }
            }

            return Ok(new HandleFriendRequestResponse { Success = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HandleFriendRequest] 异常: {ex.Message}");
            Console.WriteLine($"[HandleFriendRequest] 堆栈跟踪: {ex.StackTrace}");
            return StatusCode(500, new HandleFriendRequestResponse { Success = false, Error = $"服务器错误: {ex.Message}" });
        }
    }

    // 获取待处理的好友申请列表
    [HttpGet("pending")]
    public async Task<ActionResult<GetPendingFriendRequestsResponse>> GetPendingRequests([FromQuery] string userId)
    {
        var requests = await _db.FriendRequests
            .Where(r => r.ToUserId == userId && r.Status == FriendRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        // 由于现在 FriendRequest 已经包含了所需信息，直接返回即可
        return Ok(new GetPendingFriendRequestsResponse { Success = true, Requests = requests });
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
            .Where(u => u.UserId != currentUserId && u.UserId != "system" && // 排除自己和系统账号
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
