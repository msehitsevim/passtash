using System.Windows.Controls;
using PassVault.ViewModels;

namespace PassVault.Views;

public partial class VaultView : UserControl
{
    public VaultView()
    {
        InitializeComponent();
    }

    private void FavoritesButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ShowFavoritesOnly = !vm.ShowFavoritesOnly;
            vm.SelectedCategory = null;
        }
    }
}
