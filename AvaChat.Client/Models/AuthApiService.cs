
namespace AvaChat.Client.Models;

public class AuthApiService
{
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient = new();

    public AuthApiService(string baseUrl)
    {
        _baseUrl = NormalizeServerAddress(baseUrl).TrimEnd('/');
    }
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
            return System.Text.Json.JsonSerializer.Deserialize<RegisterResponse>(content);
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
            return System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(content);
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
            return System.Text.Json.JsonSerializer.Deserialize<LogoutResponse>(content);
        }
        catch (Exception ex)
        {
            throw new Exception($"反序列化失败: {ex.Message}, 原始内容: {content}");
        }
    }
}
