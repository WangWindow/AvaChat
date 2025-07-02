using System.ComponentModel;
using AvaChat.Server.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaChat.Server.Views;

public partial class MainWindow : Window
{
    private ScrollViewer? _messagesScroller;
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        // 窗口加载完成后的处理
        Loaded += MainWindow_Loaded;

        // 数据上下文更改时的处理
        DataContextChanged += MainWindow_DataContextChanged;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        _messagesScroller = this.FindControl<ScrollViewer>("MessagesScroller");
    }

    private void MainWindow_DataContextChanged(object? sender, EventArgs e)
    {
        // 解除旧ViewModel的事件订阅
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        // 设置新的ViewModel并订阅事件
        _viewModel = DataContext as MainWindowViewModel;
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 当系统消息更新时，自动滚动到底部
        if (e.PropertyName == nameof(MainWindowViewModel.SystemMessages) && _messagesScroller != null)
        {
            _messagesScroller.ScrollToEnd();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // 释放资源
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            (_viewModel as IDisposable)?.Dispose();
        }

        base.OnClosed(e);
    }
}
