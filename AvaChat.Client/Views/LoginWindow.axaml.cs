
using AvaChat.Client.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
namespace AvaChat.Client.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private void UserIdDropButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.IsUserIdListOpen = !vm.IsUserIdListOpen;
        }
        // 让ListBox获得焦点，便于键盘操作
        var listBox = this.FindControl<ListBox>("UserIdListBox");
        if (listBox != null && listBox.IsVisible)
        {
            listBox.Focus();
        }
    }

    private void UserIdListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.IsUserIdListOpen = false;
        }
        // 让TextBox获得焦点
        var textBox = this.FindControl<TextBox>("UserIdTextBox");
        textBox?.Focus();
    }

    private void UserIdListBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.IsUserIdListOpen = false;
        }
    }

    private void UserIdTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        // 如果焦点不在ListBox上则关闭
        var listBox = this.FindControl<ListBox>("UserIdListBox");
        if (listBox != null && !listBox.IsFocused)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.IsUserIdListOpen = false;
            }
        }
    }
}
