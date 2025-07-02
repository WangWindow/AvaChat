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
                .Where(d => d.FriendshipId != 0 || d.FriendUserId == "system")
                .Select(d => new Friendship
                {
                    FriendshipId = d.FriendshipId,
                    UserId = d.UserId,
                    FriendUserId = d.FriendUserId,
                    CreatedAt = d.CreatedAt,
                }).ToList();
            using (var db = factory.CreateDbContext([]))
            {
                foreach (var f in friendships)
                {
                    // 仅当FriendshipId为0且FriendUserId不是System时跳过
                    if (f.FriendshipId == 0 && f.FriendUserId != "system")
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

    // 发送好友申请
    public async Task<AddFriendResponse?> SendFriendRequestAsync(string fromUserId, string toUserId, string message = "")
    {
        var url = _baseUrl + "/api/friend/request";
        var req = new AddFriendRequest
        {
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Message = message
        };
        var json = JsonSerializer.Serialize(req);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            Console.WriteLine($"[SendFriendRequestAsync] 发送请求: {url}, content={json}");
            var resp = await _httpClient.PostAsync(url, content);

            var responseContent = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"[SendFriendRequestAsync] 响应状态: {resp.StatusCode}, 内容: {responseContent}");

            if (!resp.IsSuccessStatusCode)
                return new AddFriendResponse { Success = false, Error = $"HTTP错误: {resp.StatusCode}" };

            try
            {
                return JsonSerializer.Deserialize<AddFriendResponse>(responseContent);
            }
            catch (Exception jsonEx)
            {
                Console.WriteLine($"[SendFriendRequestAsync] JSON解析错误: {jsonEx.Message}");
                return new AddFriendResponse { Success = false, Error = $"响应解析失败: {jsonEx.Message}" };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SendFriendRequestAsync] 异常: {ex.Message}");
            return new AddFriendResponse { Success = false, Error = ex.Message };
        }
    }

    // 处理好友申请
    public async Task<HandleFriendRequestResponse?> HandleFriendRequestAsync(string userId, string fromUserId, bool accept)
    {
        var url = _baseUrl + "/api/friend/handle";
        var req = new HandleFriendRequestRequest
        {
            UserId = userId,
            FromUserId = fromUserId,
            Accept = accept
        };
        var json = JsonSerializer.Serialize(req);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var resp = await _httpClient.PostAsync(url, content);
            if (!resp.IsSuccessStatusCode) return null;
            var responseContent = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<HandleFriendRequestResponse>(responseContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HandleFriendRequestAsync] Exception: {ex.Message}");
            return null;
        }
    }

    // 获取待处理的好友申请
    public async Task<GetPendingFriendRequestsResponse?> GetPendingFriendRequestsAsync(string userId)
    {
        var url = _baseUrl + $"/api/friend/pending?userId={userId}";
        try
        {
            var resp = await _httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return null;
            var responseContent = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GetPendingFriendRequestsResponse>(responseContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetPendingFriendRequestsAsync] Exception: {ex.Message}");
            return null;
        }
    }

    // 搜索用户
    public async Task<List<UserInfo>?> SearchUsersAsync(string query, string currentUserId)
    {
        var url = _baseUrl + $"/api/friend/search?query={Uri.EscapeDataString(query)}&currentUserId={currentUserId}";
        try
        {
            var resp = await _httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return null;
            var responseContent = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<UserInfo>>(responseContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SearchUsersAsync] Exception: {ex.Message}");
            return null;
        }
    }

    // 同步聊天记录
    public async Task<List<Message>?> SyncMessagesAsync(string userId, DateTime? lastSyncTime = null)
    {
        var url = _baseUrl + $"/api/chat/sync?userId={userId}";
        if (lastSyncTime.HasValue)
        {
            url += $"&lastSyncTime={lastSyncTime.Value:yyyy-MM-ddTHH:mm:ss.fffZ}";
        }

        try
        {
            var resp = await _httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return null;
            var responseContent = await resp.Content.ReadAsStringAsync();
            var messages = JsonSerializer.Deserialize<List<Message>>(responseContent);

            // 保存到本地数据库
            if (messages != null && messages.Count > 0)
            {
                var factory = new ClientDbContextFactory();
                using var db = factory.CreateDbContext([]);
                foreach (var msg in messages)
                {
                    var existingMsg = db.Messages.FirstOrDefault(m => m.MessageId == msg.MessageId);
                    if (existingMsg == null)
                    {
                        db.Messages.Add(msg);
                    }
                }
                db.SaveChanges();
            }

            return messages;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SyncMessagesAsync] Exception: {ex.Message}");
            return null;
        }
    }

    // 获取聊天记录
    public async Task<List<Message>?> GetChatHistoryAsync(string userId, string friendId, int count = 50)
    {
        // 先从本地获取
        var factory = new ClientDbContextFactory();
        using var db = factory.CreateDbContext([]);
        var localMessages = db.Messages
            .Where(m => (m.SenderId == userId && m.ReceiverId == friendId) ||
                       (m.SenderId == friendId && m.ReceiverId == userId))
            .OrderBy(m => m.Timestamp)
            .Take(count)
            .ToList();

        if (localMessages.Count > 0)
        {
            return localMessages;
        }

        // 本地没有则从服务器获取
        var url = _baseUrl + $"/api/chat/history?userId={userId}&friendId={friendId}&count={count}";
        try
        {
            var resp = await _httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return null;
            var responseContent = await resp.Content.ReadAsStringAsync();
            var messages = JsonSerializer.Deserialize<List<Message>>(responseContent);

            // 保存到本地
            if (messages != null && messages.Count > 0)
            {
                foreach (var msg in messages)
                {
                    var existingMsg = db.Messages.FirstOrDefault(m => m.MessageId == msg.MessageId);
                    if (existingMsg == null)
                    {
                        db.Messages.Add(msg);
                    }
                }
                db.SaveChanges();
            }

            return messages;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetChatHistoryAsync] Exception: {ex.Message}");
            return null;
        }
    }

    // 发送消息
    public async Task<SendMessageResponse?> SendMessageAsync(string fromUserId, string toUserId, string content)
    {
        var url = _baseUrl + "/api/chat/send";
        var req = new SendMessageRequest
        {
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Content = content
        };
        var json = JsonSerializer.Serialize(req);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var resp = await _httpClient.PostAsync(url, httpContent);
            if (!resp.IsSuccessStatusCode) return null;
            var responseContent = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SendMessageResponse>(responseContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SendMessageAsync] Exception: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 批量同步用户数据（登录时调用）
    /// 优先级策略：
    /// 1. 用户基本信息 - 立即同步
    /// 2. 好友关系列表 - 立即同步
    /// 3. 聊天记录 - 增量同步
    /// 4. 好友申请 - 立即同步
    /// </summary>
    public async Task<bool> BatchSyncAsync(string userId)
    {
        try
        {
            // 获取本地最后同步时间
            var factory = new ClientDbContextFactory();
            DateTime? lastSyncTime = null;

            using (var db = factory.CreateDbContext([]))
            {
                var setting = db.ClientSettings.FirstOrDefault(s => s.Key == "LastSyncTime");
                if (setting != null && DateTime.TryParse(setting.Value, out var parsedTime))
                {
                    lastSyncTime = parsedTime;
                }
            }

            var req = new BatchSyncRequest
            {
                UserId = userId,
                LastSyncTime = lastSyncTime
            };

            var url = _baseUrl + "/api/sync/batch";
            var resp = await _httpClient.PostAsJsonAsync(url, req);

            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine($"[BatchSyncAsync] 同步失败: {resp.StatusCode}");
                return false;
            }

            var content = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"[BatchSyncAsync] 同步响应: {content}");

            var syncResponse = JsonSerializer.Deserialize<BatchSyncResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (syncResponse == null || !syncResponse.Success)
            {
                Console.WriteLine($"[BatchSyncAsync] 同步失败: {syncResponse?.Error}");
                return false;
            }

            // 保存同步数据到本地数据库
            using (var db = factory.CreateDbContext([]))
            {
                // 1. 同步用户信息
                if (syncResponse.UserInfo != null)
                {
                    var existingUser = db.Users.FirstOrDefault(u => u.UserId == userId);
                    if (existingUser != null)
                    {
                        existingUser.UserName = syncResponse.UserInfo.UserName;
                        existingUser.Status = syncResponse.UserInfo.Status;
                        existingUser.LastLoginTime = syncResponse.UserInfo.LastLoginTime;
                    }
                    else
                    {
                        db.Users.Add(syncResponse.UserInfo);
                    }
                }

                // 2. 同步好友关系（全量替换）
                if (syncResponse.Friendships.Count > 0)
                {
                    // 删除现有好友关系
                    var existingFriendships = db.Friendships.Where(f => f.UserId == userId).ToList();
                    db.Friendships.RemoveRange(existingFriendships);

                    // 添加新的好友关系
                    foreach (var friendship in syncResponse.Friendships)
                    {
                        // 保存或更新好友用户信息
                        var friendUser = db.Users.FirstOrDefault(u => u.UserId == friendship.FriendUserId);
                        if (friendUser != null)
                        {
                            friendUser.UserName = friendship.FriendUser.UserName;
                            friendUser.Status = friendship.FriendUser.Status;
                            friendUser.LastLoginTime = friendship.FriendUser.LastLoginTime;
                        }
                        else
                        {
                            db.Users.Add(friendship.FriendUser);
                        }

                        // 添加好友关系
                        db.Friendships.Add(new Friendship
                        {
                            FriendshipId = friendship.FriendshipId,
                            UserId = friendship.UserId,
                            FriendUserId = friendship.FriendUserId,
                            CreatedAt = friendship.CreatedAt
                        });
                    }
                }

                // 3. 同步聊天记录（增量添加）
                if (syncResponse.Messages.Count > 0)
                {
                    foreach (var message in syncResponse.Messages)
                    {
                        var existing = db.Messages.FirstOrDefault(m => m.MessageId == message.MessageId);
                        if (existing == null)
                        {
                            db.Messages.Add(message);
                        }
                    }
                }

                // 4. 更新最后同步时间
                var syncTimeSetting = db.ClientSettings.FirstOrDefault(s => s.Key == "LastSyncTime");
                if (syncTimeSetting != null)
                {
                    syncTimeSetting.Value = syncResponse.LastSyncTime.ToString("O");
                }
                else
                {
                    db.ClientSettings.Add(new ClientSetting
                    {
                        Key = "LastSyncTime",
                        Value = syncResponse.LastSyncTime.ToString("O")
                    });
                }

                await db.SaveChangesAsync();
            }

            Console.WriteLine($"[BatchSyncAsync] 同步完成 - 用户信息: {syncResponse.UserInfo != null}, 好友: {syncResponse.Friendships.Count}, 消息: {syncResponse.Messages.Count}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BatchSyncAsync] 异常: {ex.Message}");
            return false;
        }
    }
}
