using Microsoft.AspNetCore.Mvc;

namespace AvaChat.Server.Models;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ServerDbContext db) : ControllerBase
{
    // 服务端初始化时自动创建System用户
    static AuthController()
    {
        using var db = new ServerDbContext(new DbContextOptions<ServerDbContext>());
        // 检查System用户是否存在
        var sys = db.Users.FirstOrDefault(u => u.UserId == "00000000");
        if (sys == null)
        {
            db.Users.Add(new User
            {
                UserId = "00000000",
                UserName = "System",
                Password = "",
                Status = UserStatus.Online,
                LastLoginTime = DateTime.UtcNow
            });
            db.SaveChanges();
        }
        // 为所有用户与System建立好友关系
        var allUserIds = db.Users.Select(u => u.UserId).ToList();
        foreach (var uid in allUserIds)
        {
            if (uid == "00000000") continue;
            if (!db.Friendships.Any(f => f.UserId == uid && f.FriendUserId == "00000000"))
            {
                db.Friendships.Add(new Friendship
                {
                    UserId = uid,
                    FriendUserId = "00000000",
                    CreatedAt = DateTime.UtcNow,
                    AlterName = "System"
                });
            }
            if (!db.Friendships.Any(f => f.UserId == "00000000" && f.FriendUserId == uid))
            {
                db.Friendships.Add(new Friendship
                {
                    UserId = "00000000",
                    FriendUserId = uid,
                    CreatedAt = DateTime.UtcNow,
                    AlterName = ""
                });
            }
        }
        db.SaveChanges();
    }

    // 在线用户变更信号
    public static event Action? OnlineUsersChanged;

    private readonly ServerDbContext _db = db;

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.UserName == req.UserName))
        {
            return Ok(new RegisterResponse { Success = false, Error = "用户名已存在" });
        }

        string userId;
        var rand = new Random();
        do
        {
            userId = rand.Next(0, 100000000).ToString("D8");
        } while (await _db.Users.AnyAsync(u => u.UserId == userId));

        var user = new User
        {
            UserId = userId,
            UserName = req.UserName,
            Password = req.Password,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new RegisterResponse
        {
            Success = true,
            UserId = userId
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {

        // 禁止登录System账号
        if (req.UserId == "00000000")
        {
            return Ok(new LoginResponse
            {
                Success = false,
                Error = "禁止登录系统账号"
            });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == req.UserId);
        if (user == null || user.Password != req.Password)
        {
            return Ok(new LoginResponse
            {
                Success = false,
                Error = "用户ID或密码错误"
            });
        }

        user.Status = UserStatus.Online;
        user.LastLoginTime = DateTime.Now;
        await _db.SaveChangesAsync();

        // 触发在线用户变更信号
        OnlineUsersChanged?.Invoke();

        return Ok(new LoginResponse
        {
            Success = true,
            UserName = user.UserName
        });
    }

    [HttpPost("logout")]
    public async Task<ActionResult<LogoutResponse>> Logout([FromBody] LogoutRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == req.UserId);
        if (user == null)
        {
            return Ok(new LogoutResponse
            {
                Success = false,
                Error = "用户不存在"
            });
        }
        user.Status = UserStatus.Offline;
        await _db.SaveChangesAsync();

        // 触发在线用户变更信号
        OnlineUsersChanged?.Invoke();

        return Ok(new LogoutResponse
        {
            Success = true,
            UserName = user.UserName
        });
    }
}
