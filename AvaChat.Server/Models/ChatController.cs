using AvaChat.Server.Hubs;
using AvaChat.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AvaChat.Server.Models;

[ApiController]
[Route("api/[controller]")]
public class ChatController(ServerDbContext db, IHubContext<ChatHub> hubContext) : ControllerBase
{
    private readonly ServerDbContext _db = db;
    private readonly IHubContext<ChatHub> _hubContext = hubContext;

    // 发送消息（用户间直接通信 - 已通过SignalR实现，此API仅作为备选）
    [HttpPost("send")]
    public async Task<ActionResult<SendMessageResponse>> Send([FromBody] SendMessageRequest req)
    {
        // 检查用户存在
        var fromUser = await _db.Users.FindAsync(req.FromUserId);
        var toUser = await _db.Users.FindAsync(req.ToUserId);
        if (fromUser == null || toUser == null)
        {
            return Ok(new SendMessageResponse { Success = false, Error = "用户不存在" });
        }

        // 检查好友关系
        var isFriend = await _db.Friendships.AnyAsync(f => f.UserId == req.FromUserId && f.FriendUserId == req.ToUserId);
        if (!isFriend)
        {
            return Ok(new SendMessageResponse { Success = false, Error = "不是好友关系" });
        }

        var msg = new Message
        {
            SenderId = req.FromUserId,
            ReceiverId = req.ToUserId,
            Content = req.Content,
            Timestamp = DateTime.Now,
            MessageType = MessageType.Text,
            Status = MessageStatus.Delivered
        };

        // 保存消息到数据库
        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();

        // 更新消息ID
        msg.MessageId = _db.Messages
            .OrderByDescending(m => m.MessageId)
            .First(m => m.SenderId == req.FromUserId && m.ReceiverId == req.ToUserId && m.Content == req.Content)
            .MessageId;

        return Ok(new SendMessageResponse { Success = true });
    }

    // 获取与某用户的聊天记录
    [HttpGet("history")]
    public async Task<ActionResult<List<Message>>> GetHistory([FromQuery] string userId, [FromQuery] string friendId, [FromQuery] int count = 50)
    {
        var msgs = await _db.Messages
            .Where(m => (m.SenderId == userId && m.ReceiverId == friendId) ||
                        (m.SenderId == friendId && m.ReceiverId == userId))
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
        return Ok(msgs.Select(m => new Message
        {
            SenderId = m.SenderId,
            ReceiverId = m.ReceiverId,
            Content = m.Content,
            Timestamp = m.Timestamp,
            MessageType = m.MessageType
        }).ToList());
    }

    // 获取用户的所有聊天记录（用于登录时同步）
    [HttpGet("sync")]
    public async Task<ActionResult<List<Message>>> SyncMessages([FromQuery] string userId, [FromQuery] DateTime? lastSyncTime = null)
    {
        var query = _db.Messages
            .Where(m => m.SenderId == userId || m.ReceiverId == userId);

        // 如果提供了上次同步时间，只返回该时间之后的消息
        if (lastSyncTime.HasValue)
        {
            query = query.Where(m => m.Timestamp > lastSyncTime.Value);
        }

        var msgs = await query
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        return Ok(msgs.Select(m => new Message
        {
            MessageId = m.MessageId,
            SenderId = m.SenderId,
            ReceiverId = m.ReceiverId,
            Content = m.Content,
            Timestamp = m.Timestamp,
            MessageType = m.MessageType,
            Status = m.Status
        }).ToList());
    }
}
