using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PassVault.ViewModels;

namespace PassVault.Views;

public partial class UnlockView : UserControl
{
    public UnlockView()
    {
        InitializeComponent();
        Loaded += (_, _) => UnlockPasswordBox.Focus();
    }

    private void UnlockPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PasswordInput = UnlockPasswordBox.Password;
        }
    }

    private void UnlockPasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainViewModel vm)
        {
            vm.UnlockCommand.Execute(null);
        }
    }
}
