using Avalonia.Controls;
using Avalonia.Input;

namespace AvaChat.Client.Views;

public partial class LoginStatusWindow : Window
{
    public LoginStatusWindow()
    {
        InitializeComponent();

        // 添加鼠标事件处理器以支持拖拽移动
        PointerPressed += OnPointerPressed;
    }

    /// <summary>
    /// 处理鼠标按下事件，实现拖拽移动功能
    /// </summary>
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 检查是否是左键点击
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // 开始拖拽窗口
            BeginMoveDrag(e);
        }
    }
}
