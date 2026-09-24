using System.Windows;
using System.Windows.Threading;

namespace PassVault;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Catch any unhandled XAML / rendering exceptions
        DispatcherUnhandledException += (sender, args) =>
        {
            try
            {
                System.IO.File.WriteAllText("dispatcher_exception.log", args.Exception.ToString());
            }
            catch { }

            MessageBox.Show(
                $"Beklenmeyen hata:\n\n{args.Exception.Message}\n\n{args.Exception.StackTrace}",
                "PasStash - Hata",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}
