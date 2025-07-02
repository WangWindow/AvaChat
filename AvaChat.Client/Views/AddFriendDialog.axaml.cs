using AvaChat.Client.ViewModels;

namespace AvaChat.Client.Views;

public partial class AddFriendDialog : Window
{
    public AddFriendDialog()
    {
        InitializeComponent();
        DataContext = new AddFriendDialogViewModel();
    }

    public AddFriendDialog(AddFriendDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
