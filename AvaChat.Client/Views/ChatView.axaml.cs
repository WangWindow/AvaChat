using AvaChat.Client.ViewModels;
using Avalonia.Threading;

namespace AvaChat.Client.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();

        // 监听数据上下文变化
        DataContextChanged += ChatView_DataContextChanged;
    }

    private void ChatView_DataContextChanged(object? sender, EventArgs e)
    {
        // 当 ViewModel 改变时，重新订阅事件
        if (DataContext is ChatViewModel viewModel)
        {
            viewModel.ScrollToBottomRequested += ViewModel_ScrollToBottomRequested;
        }
    }

    private void ViewModel_ScrollToBottomRequested(object? sender, EventArgs e)
    {
        // 滚动到底部
        if (MessageScrollViewer != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                // 延迟一帧确保UI更新完成
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        // 滚动到最大偏移量
                        MessageScrollViewer.ScrollToEnd();

                        // 备用方法：直接设置垂直偏移
                        var maxOffset = MessageScrollViewer.ScrollBarMaximum.Y;
                        if (maxOffset > 0)
                        {
                            MessageScrollViewer.Offset = new Vector(0, maxOffset);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ChatView] 滚动到底部失败: {ex.Message}");
                    }
                }, DispatcherPriority.Background);
            });
        }
    }
}
