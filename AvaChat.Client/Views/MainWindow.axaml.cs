namespace AvaChat.Client.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 处理窗口关闭事件，支持最小化到托盘
        Closing += MainWindow_Closing;
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
}