using AvaChat.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AvaChat.Server.Models;

[ApiController]
[Route("api/[controller]")]
public class ChatController(ServerDbContext db) : ControllerBase
{
    private readonly ServerDbContext _db = db;

    // 发送消息（包括系统消息和普通消息）
    [HttpPost("send")]
    public async Task<ActionResult<SendMessageResponse>> Send([FromBody] SendMessageRequest req)
    {
        // 检查用户和好友关系
        var fromUser = await _db.Users.FindAsync(req.FromUserId);
        var toUser = await _db.Users.FindAsync(req.ToUserId);
        if (fromUser == null || toUser == null)
        {
            return Ok(new SendMessageResponse { Success = false, Error = "用户不存在" });
        }
        // 系统消息无需好友关系校验
        if (req.FromUserId != "00000000")
        {
            var isFriend = await _db.Friendships.AnyAsync(f => f.UserId == req.FromUserId && f.FriendUserId == req.ToUserId);
            if (!isFriend)
                return Ok(new SendMessageResponse { Success = false, Error = "不是好友关系" });
        }
        var msg = new Message
        {
            SenderId = req.FromUserId,
            ReceiverId = req.ToUserId,
            Content = req.Content,
            Timestamp = DateTime.Now,
            MessageType = req.FromUserId == "00000000" ? MessageType.System : MessageType.Text,
            Status = MessageStatus.Delivered
        };
        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();
        return Ok(new SendMessageResponse { Success = true });
    }

    // 获取与某用户的聊天记录（含系统消息）
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
}
