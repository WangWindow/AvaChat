using AvaChat.Client.ViewModels;

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
                MessageScrollViewer.ScrollToEnd();
            });
        }
    }
}
