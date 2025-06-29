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
    private void RegisterConfirm()
    {
        // TODO: 实现注册逻辑，成功后关闭窗口或切回登录状态，失败则显示错误
        if (string.IsNullOrWhiteSpace(RegisterUserName) || string.IsNullOrWhiteSpace(RegisterPassword))
        {
            RegisterError = "用户名和密码不能为空";
            HasRegisterError = true;
            return;
        }
        // 假设注册成功
        ShowStatusPanel = true;
        ShowRegisterPanel = false;
        ShowServerSettingsPanel = false;
        StatusMessage = "注册成功，请登录";
        StatusIcon = "✓";
        StatusColor = new SolidColorBrush(Color.FromRgb(34, 197, 94));
        IsConnecting = false;
        ShowRetryButton = false;
        HasErrorDetail = false;
    }

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
