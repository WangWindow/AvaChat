using AvaChat.Client.Models;
using Avalonia.Controls;

namespace AvaChat.Client.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 处理窗口关闭事件，支持最小化到托盘
        Closing += MainWindow_Closing;

        // 处理窗口激活事件，停止托盘图标闪烁
        Activated += MainWindow_Activated;
    }

    /// <summary>
    /// 处理窗口关闭事件
    /// </summary>
    private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        // 阻止窗口关闭，改为隐藏到托盘
        e.Cancel = true;
        Hide();
    }

    /// <summary>
    /// 处理窗口激活事件
    /// </summary>
    private void MainWindow_Activated(object? sender, EventArgs e)
    {
        // 当用户查看窗口时，停止托盘图标闪烁
        NotificationManager.OnMessageViewed();
    }
}