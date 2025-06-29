namespace AvaChat.Client.Models;

public class AuthApiService(string baseUrl)
{
    private readonly string _baseUrl = baseUrl.TrimEnd('/');
    private readonly HttpClient _httpClient = new();

    public async Task<RegisterResponse?> RegisterAsync(string userName, string password)
    {
        var req = new RegisterRequest { UserName = userName, Password = password };
        var resp = await _httpClient.PostAsJsonAsync(_baseUrl + "/api/register", req);
        return await resp.Content.ReadFromJsonAsync<RegisterResponse>();
    }

    public async Task<LoginResponse?> LoginAsync(string userId, string password)
    {
        var req = new LoginRequest { UserId = userId, Password = password };
        var resp = await _httpClient.PostAsJsonAsync(_baseUrl + "/api/login", req);
        return await resp.Content.ReadFromJsonAsync<LoginResponse>();
    }
}
