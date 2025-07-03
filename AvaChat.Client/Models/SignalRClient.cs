using System.Net;
using AvaChat.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace AvaChat.Client.Models;

/// <summary>
/// SignalR客户端 - 用于与服务器建立实时通信
/// </summary>
public class SignalRClient : IDisposable
{
    private HubConnection? _hubConnection;
    private readonly string _serverUrl;
    private readonly string _userId;
    private bool _isConnected;

    // 事件
    public event EventHandler<Message>? OnMessageReceived;
    public event EventHandler<Message>? OnSystemMessageReceived;
    public event EventHandler<Message>? OnMessageSent;
    public event EventHandler<string>? OnErrorReceived;
    public event EventHandler<(string UserId, UserStatus Status)>? OnFriendStatusChanged;

    public bool IsConnected => _isConnected;

    public SignalRClient(string serverUrl, string userId)
    {
        _serverUrl = NormalizeServerAddress(serverUrl);
        _userId = userId;
    }

    /// <summary>
    /// 开始连接到SignalR Hub
    /// </summary>
    public async Task ConnectAsync()
    {
        if (_hubConnection != null)
        {
            await DisconnectAsync();
        }

        try
        {
            // 创建连接
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_serverUrl}/chatHub")
                .WithAutomaticReconnect()
                .Build();

            // 注册接收消息的处理器
            RegisterHandlers();

            // 连接到服务器
            await _hubConnection.StartAsync();
            _isConnected = true;

            // 告诉服务器用户已连接
            await _hubConnection.InvokeAsync("UserConnected", _userId);
        }
        catch (Exception ex)
        {
            _isConnected = false;
            Console.WriteLine($"[SignalRClient] 连接失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 断开与SignalR Hub的连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }

        _isConnected = false;
    }

    /// <summary>
    /// 发送私人消息
    /// </summary>
    public async Task<bool> SendPrivateMessageAsync(string receiverId, string content)
    {
        if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
        {
            return false;
        }

        try
        {
            await _hubConnection.InvokeAsync("SendPrivateMessage", _userId, receiverId, content);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalRClient] 发送消息失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 注册接收消息的处理器
    /// </summary>
    private void RegisterHandlers()
    {
        if (_hubConnection == null) return;

        // 接收私人消息
        _hubConnection.On<Message>("ReceiveMessage", (message) =>
        {
            OnMessageReceived?.Invoke(this, message);

            // 显示通知（仅当消息不是自己发送的）
            if (message.SenderId != _userId)
            {
                NotificationManager.ShowMessageNotification(false, message.SenderId, message.Content);
            }

            // 本地保存消息
            SaveMessageToLocalDb(message);
        });

        // 接收系统消息
        _hubConnection.On<Message>("ReceiveSystemMessage", (message) =>
        {
            OnSystemMessageReceived?.Invoke(this, message);

            // 显示系统消息通知
            NotificationManager.ShowMessageNotification(true, "系统", message.Content);

            // 本地保存系统消息
            SaveMessageToLocalDb(message);
        });

        // 消息发送成功回调
        _hubConnection.On<Message>("MessageSent", (message) =>
        {
            OnMessageSent?.Invoke(this, message);
        });

        // 接收错误信息
        _hubConnection.On<string>("ReceiveError", (error) =>
        {
            OnErrorReceived?.Invoke(this, error);
        });

        // 好友状态变更
        _hubConnection.On<string, UserStatus>("FriendStatusChanged", (userId, status) =>
        {
            OnFriendStatusChanged?.Invoke(this, (userId, status));

            // 更新本地好友状态
            UpdateFriendStatus(userId, status);
        });

        // 自动重连成功后
        _hubConnection.Closed += async (error) =>
        {
            _isConnected = false;
            await Task.Delay(new Random().Next(0, 5) * 1000);
            await ConnectAsync();
        };
    }

    /// <summary>
    /// 将消息保存到本地数据库
    /// </summary>
    private void SaveMessageToLocalDb(Message message)
    {
        Task.Run(() =>
        {
            try
            {
                var factory = new ClientDbContextFactory();
                using var db = factory.CreateDbContext([]);

                // 检查消息是否已存在
                var exists = db.Messages.Any(m => m.MessageId == message.MessageId);
                if (!exists)
                {
                    db.Messages.Add(message);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveMessageToLocalDb] 保存消息失败: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// 更新本地好友状态
    /// </summary>
    private void UpdateFriendStatus(string friendId, UserStatus status)
    {
        Task.Run(() =>
        {
            try
            {
                var factory = new ClientDbContextFactory();
                using var db = factory.CreateDbContext([]);

                // 更新用户状态
                var user = db.Users.FirstOrDefault(u => u.UserId == friendId);
                if (user != null)
                {
                    user.Status = status;
                    if (status == UserStatus.Online)
                    {
                        user.LastLoginTime = DateTime.Now;
                    }
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateFriendStatus] 更新好友状态失败: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// 补全ServerAddress前缀
    /// </summary>
    private static string NormalizeServerAddress(string addr)
    {
        if (string.IsNullOrWhiteSpace(addr))
        {
            // 从数据库中加载服务器地址
            try
            {
                var factory = new ClientDbContextFactory();
                using var db = factory.CreateDbContext([]);
                var serverSetting = db.ClientSettings.FirstOrDefault(s => s.Key == "ServerAddress");
                if (serverSetting != null && !string.IsNullOrEmpty(serverSetting.Value))
                {
                    addr = serverSetting.Value;
                }
                else
                {
                    return "http://localhost:5000";
                }
            }
            catch
            {
                return "http://localhost:5000";
            }
        }

        if (!addr.StartsWith("http://") && !addr.StartsWith("https://"))
            return "http://" + addr;
        return addr;
    }

    public void Dispose()
    {
        DisconnectAsync().Wait();
    }
}
