using System;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaChat.Client.ViewModels;

public partial class LoginStatusViewModel : ObservableObject
{
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
