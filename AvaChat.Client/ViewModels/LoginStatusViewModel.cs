namespace AvaChat.Client.ViewModels;

public partial class LoginStatusViewModel : ObservableObject
{
    // 登录/状态相关
    [ObservableProperty]
    private bool _isConnecting = true;

    [ObservableProperty]
    private string _statusMessage = "正在连接服务器...";

    [ObservableProperty]
    private string _statusIcon = "";

    [ObservableProperty]
    private IBrush? _statusColor;

    [ObservableProperty]
    private string? _errorDetail;

    [ObservableProperty]
    private bool _hasErrorDetail;

    [ObservableProperty]
    private bool _showRetryButton;

    // 注册相关
    [ObservableProperty]
    private string _registerUserName = string.Empty;

    [ObservableProperty]
    private string _registerPassword = string.Empty;

    [ObservableProperty]
    private string _registerError = string.Empty;

    [ObservableProperty]
    private bool _hasRegisterError = false;

    // 服务器设置相关
    [ObservableProperty]
    private string _serverAddress = "localhost:5000";

    [ObservableProperty]
    private string _serverSettingsError = string.Empty;

    [ObservableProperty]
    private bool _hasServerSettingsError = false;

    // 可见性控制
    [ObservableProperty]
    private bool _showStatusPanel = true;

    [ObservableProperty]
    private bool _showRegisterPanel = false;

    [ObservableProperty]
    private bool _showServerSettingsPanel = false;

    /// <summary>
    /// 窗口关闭事件
    /// </summary>
    public event EventHandler? WindowCloseRequested;

    /// <summary>
    /// 重试登录事件
    /// </summary>
    public event EventHandler? RetryRequested;

    /// <summary>
    /// 显示连接中状态
    /// </summary>
    public void ShowConnecting()
    {
        ShowStatusPanel = true;
        ShowRegisterPanel = false;
        ShowServerSettingsPanel = false;
        IsConnecting = true;
        StatusMessage = "正在连接服务器...";
        ShowRetryButton = false;
        HasErrorDetail = false;
    }

    /// <summary>
    /// 显示登录成功状态
    /// </summary>
    public async Task ShowSuccessAsync()
    {
        ShowStatusPanel = true;
        ShowRegisterPanel = false;
        ShowServerSettingsPanel = false;
        IsConnecting = false;
        StatusMessage = "登录成功！";
        StatusIcon = "✓";
        StatusColor = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // Green
        ShowRetryButton = false;
        HasErrorDetail = false;

        // 1秒后自动关闭
        await Task.Delay(1000);
        WindowCloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 显示登录失败状态
    /// </summary>
    public void ShowError(string errorMessage, string? errorDetail = null)
    {
        ShowStatusPanel = true;
        ShowRegisterPanel = false;
        ShowServerSettingsPanel = false;
        IsConnecting = false;
        StatusMessage = "登录失败";
        StatusIcon = "✗";
        StatusColor = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
        ShowRetryButton = true;

        if (!string.IsNullOrEmpty(errorDetail))
        {
            ErrorDetail = errorDetail;
            HasErrorDetail = true;
        }
        else
        {
            HasErrorDetail = false;
        }
    }

    /// <summary>
    /// 显示注册面板
    /// </summary>
    public void ShowRegisterPanelView()
    {
        ShowStatusPanel = false;
        ShowRegisterPanel = true;
        ShowServerSettingsPanel = false;
        RegisterUserName = string.Empty;
        RegisterPassword = string.Empty;
        RegisterError = string.Empty;
        HasRegisterError = false;
    }

    /// <summary>
    /// 显示服务器设置面板
    /// </summary>
    public void ShowServerSettingsPanelView()
    {
        ShowStatusPanel = false;
        ShowRegisterPanel = false;
        ShowServerSettingsPanel = true;
        ServerSettingsError = string.Empty;
        HasServerSettingsError = false;
    }

    /// <summary>
    /// 注册确认命令
    /// </summary>
    [RelayCommand]
    private async Task RegisterConfirmAsync()
    {
        if (string.IsNullOrWhiteSpace(RegisterUserName) || string.IsNullOrWhiteSpace(RegisterPassword))
        {
            RegisterError = "用户名和密码不能为空";
            HasRegisterError = true;
            return;
        }

        try
        {
            ShowStatusPanel = true;
            ShowRegisterPanel = false;
            ShowServerSettingsPanel = false;
            IsConnecting = true;
            StatusMessage = "正在注册...";
            StatusIcon = string.Empty;
            StatusColor = null;
            ShowRetryButton = false;
            HasErrorDetail = false;

            // 调用API
            var api = new AuthApiService(ServerAddress);
            var resp = await api.RegisterAsync(RegisterUserName, RegisterPassword);

            System.Diagnostics.Debug.WriteLine($"[RegisterConfirmAsync] resp = {{ Success = {resp?.Success}, UserId = {resp?.UserId}, Error = {resp?.Error} }}");

            if (resp == null)
            {
                throw new Exception("无法连接服务器");
            }
            if (resp.Success)
            {
                // 注册成功，切回登录面板并自动填充UserId
                StatusMessage = $"注册成功，分配号码：{resp.UserId}";
                StatusIcon = "✓";
                StatusColor = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                IsConnecting = false;
                ShowRetryButton = false;
                HasErrorDetail = false;

                // 通知LoginViewModel自动填充UserId
                RegisterSuccess?.Invoke(this, resp.UserId ?? string.Empty);

                // 注册成功后1秒自动关闭窗口
                await Task.Delay(1000);
                WindowCloseRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // 注册失败，回到注册面板并显示错误
                ShowStatusPanel = false;
                ShowRegisterPanel = true;
                ShowServerSettingsPanel = false;
                RegisterError = resp.Error ?? "注册失败";
                HasRegisterError = true;
            }
        }
        catch (Exception ex)
        {
            ShowStatusPanel = false;
            ShowRegisterPanel = true;
            ShowServerSettingsPanel = false;
            RegisterError = ex.Message;
            HasRegisterError = true;
        }
    }

    /// <summary>
    /// 注册成功事件（用于回传UserId给LoginViewModel自动填充）
    /// </summary>
    public event EventHandler<string>? RegisterSuccess;

    /// <summary>
    /// 保存服务器设置命令
    /// </summary>
    [RelayCommand]
    private void SaveServerSettings()
    {
        if (string.IsNullOrWhiteSpace(ServerAddress))
        {
            ServerSettingsError = "服务器地址不能为空";
            HasServerSettingsError = true;
            return;
        }
        // 假设保存成功
        ShowStatusPanel = true;
        ShowRegisterPanel = false;
        ShowServerSettingsPanel = false;
        StatusMessage = "服务器设置已保存";
        StatusIcon = "✓";
        StatusColor = new SolidColorBrush(Color.FromRgb(34, 197, 94));
        IsConnecting = false;
        ShowRetryButton = false;
        HasErrorDetail = false;
    }

    /// <summary>
    /// 重试命令
    /// </summary>
    [RelayCommand]
    private void Retry()
    {
        RetryRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 取消命令
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        WindowCloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
