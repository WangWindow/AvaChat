namespace AvaChat.Client.Models;

public class ApiService(string baseUrl)
{
    private readonly string _baseUrl = NormalizeServerAddress(baseUrl).TrimEnd('/');
    private readonly HttpClient _httpClient = new();

    /// <summary>
    /// 补全ServerAddress前缀
    /// </summary>
    private static string NormalizeServerAddress(string addr)
    {
        if (string.IsNullOrWhiteSpace(addr)) return "http://localhost:5000";
        if (!addr.StartsWith("http://") && !addr.StartsWith("https://"))
            return "http://" + addr;
        return addr;
    }

    public async Task<RegisterResponse?> RegisterAsync(string userName, string password)
    {
        var req = new RegisterRequest
        {
            UserName = userName,
            Password = password
        };
        var url = _baseUrl + "/api/auth/register";
        var resp = await _httpClient.PostAsJsonAsync(url, req);
        var content = await resp.Content.ReadAsStringAsync();
        Console.WriteLine($"[RegisterAsync] url={url}, status={resp.StatusCode}, content={content}");
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"API错误: {resp.StatusCode}, 内容: {content}");
        try
        {
            return JsonSerializer.Deserialize<RegisterResponse>(content);
        }
        catch (Exception ex)
        {
            throw new Exception($"反序列化失败: {ex.Message}, 原始内容: {content}");
        }
    }

    public async Task<LoginResponse?> LoginAsync(string userId, string password)
    {
        var req = new LoginRequest
        {
            UserId = userId,
            Password = password
        };
        var url = _baseUrl + "/api/auth/login";
        var resp = await _httpClient.PostAsJsonAsync(url, req);
        var content = await resp.Content.ReadAsStringAsync();
        Console.WriteLine($"[LoginAsync] url={url}, status={resp.StatusCode}, content={content}");
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"API错误: {resp.StatusCode}, 内容: {content}");
        try
        {
            return JsonSerializer.Deserialize<LoginResponse>(content);
        }
        catch (Exception ex)
        {
            throw new Exception($"反序列化失败: {ex.Message}, 原始内容: {content}");
        }
    }

    public async Task<LogoutResponse?> LogoutAsync(string userId)
    {
        var req = new LogoutRequest
        {
            UserId = userId
        };
        var url = _baseUrl + "/api/auth/logout";
        var resp = await _httpClient.PostAsJsonAsync(url, req);
        var content = await resp.Content.ReadAsStringAsync();
        Console.WriteLine($"[LogoutAsync] url={url}, status={resp.StatusCode}, content={content}");
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"API错误: {resp.StatusCode}, 内容: {content}");
        try
        {
            return JsonSerializer.Deserialize<LogoutResponse>(content);
        }
        catch (Exception ex)
        {
            throw new Exception($"反序列化失败: {ex.Message}, 原始内容: {content}");
        }
    }

    // 获取用户基本信息（本地优先，云端兜底）
    public async Task<UserInfo?> GetUserDataAsync(string userId)
    {
        // 本地缓存优先
        var factory = new ClientDbContextFactory();
        using (var db = factory.CreateDbContext([]))
        {
            var local = db.Users.FirstOrDefault(u => u.UserId == userId);
            if (local != null)
                return local;
        }
        // 云端拉取
        var url = _baseUrl + $"/api/sync/user?userId={userId}";
        var resp = await _httpClient.GetAsync(url);
        if (!resp.IsSuccessStatusCode) return null;
        var content = await resp.Content.ReadAsStringAsync();
        Console.WriteLine($"[GetUserDataAsync] url={url}, status={resp.StatusCode}, content={content}");
        try
        {
            var userInfo = JsonSerializer.Deserialize<UserInfo>(content);
            if (userInfo == null) return null;
            // 写入本地缓存（插入或更新）
            using (var db = factory.CreateDbContext([]))
            {
                var exist = db.Users.FirstOrDefault(u => u.UserId == userInfo.UserId);
                if (exist != null)
                {
                    // 更新字段
                    exist.UserName = userInfo.UserName;
                    exist.Status = userInfo.Status;
                    exist.LastLoginTime = userInfo.LastLoginTime;
                    Console.WriteLine($"[GetUserDataAsync] Update local user: {userInfo.UserId}");
                }
                else
                {
                    db.Users.Add(userInfo);
                    Console.WriteLine($"[GetUserDataAsync] Add local user: {userInfo.UserId}");
                }
                db.SaveChanges();
            }
            return userInfo;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetUserDataAsync] Exception: {ex.Message}");
            return null;
        }
    }

    // 获取好友关系列表（本地优先，云端兜底）
    public async Task<List<Friendship>?> GetFriendshipsAsync(string userId)
    {
        var factory = new ClientDbContextFactory();
        using (var db = factory.CreateDbContext([]))
        {
            var local = db.Friendships.Where(f => f.UserId == userId).ToList();
            if (local.Count > 0)
                return local;
        }
        // 云端拉取
        var url = _baseUrl + $"/api/sync/friends?userId={userId}";
        var resp = await _httpClient.GetAsync(url);
        if (!resp.IsSuccessStatusCode) return null;
        var content = await resp.Content.ReadAsStringAsync();
        Console.WriteLine($"[GetFriendshipsAsync] url={url}, status={resp.StatusCode}, content={content}");
        try
        {
            // 反序列化为DTO后可映射为本地Friendship
            var dtos = JsonSerializer.Deserialize<List<Friendship>>(content);
            if (dtos == null) return null;
            var friendships = dtos
                .Where(d => d.FriendshipId != 0 || d.FriendUserId == "00000000")
                .Select(d => new Friendship
                {
                    FriendshipId = d.FriendshipId,
                    UserId = d.UserId,
                    FriendUserId = d.FriendUserId,
                    CreatedAt = d.CreatedAt,
                    AlterName = d.AlterName,
                }).ToList();
            using (var db = factory.CreateDbContext([]))
            {
                foreach (var f in friendships)
                {
                    // 仅当FriendshipId为0且FriendUserId不是System时跳过
                    if (f.FriendshipId == 0 && f.FriendUserId != "00000000")
                    {
                        Console.WriteLine($"[GetFriendshipsAsync] Skip friendship with FriendshipId=0, UserId={f.UserId}, FriendUserId={f.FriendUserId}");
                        continue;
                    }
                    var exist = db.Friendships.FirstOrDefault(x => x.FriendshipId == f.FriendshipId && x.FriendUserId == f.FriendUserId);
                    if (exist != null)
                    {
                        exist.UserId = f.UserId;
                        exist.FriendUserId = f.FriendUserId;
                        exist.CreatedAt = f.CreatedAt;
                        exist.AlterName = f.AlterName;
                        Console.WriteLine($"[GetFriendshipsAsync] Update local friendship: {f.FriendshipId}, FriendUserId: {f.FriendUserId}");
                    }
                    else
                    {
                        db.Friendships.Add(f);
                        Console.WriteLine($"[GetFriendshipsAsync] Add local friendship: {f.FriendshipId}, FriendUserId: {f.FriendUserId}");
                    }
                }
                db.SaveChanges();
            }
            return friendships;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetFriendshipsAsync] Exception: {ex.Message}");
            return null;
        }
    }
}
