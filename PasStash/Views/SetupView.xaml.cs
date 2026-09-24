using System.Windows;
using System.Windows.Controls;
using PassVault.ViewModels;

namespace PassVault.Views;

public partial class SetupView : UserControl
{
    public SetupView()
    {
        InitializeComponent();
    }

    private void SetupPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PasswordInput = SetupPasswordBox.Password;
        }
    }

    private void SetupConfirmBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PasswordConfirm = SetupConfirmBox.Password;
        }
    }
}
