using System.Windows;
using System.Windows.Input;
using PassVault.ViewModels;

namespace PassVault;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        try
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            Loaded += (s, e) =>
            {
                Topmost = true;
                Activate();
                Focus();
                Topmost = false;
            };
        }
        catch (System.Exception ex)
        {
            System.IO.File.WriteAllText("startup_error.log", ex.ToString());
            throw;
        }
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        _viewModel.ResetInactivityTimer();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        _viewModel.ResetInactivityTimer();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_StateChanged(object sender, System.EventArgs e)
    {
        if (MaximizeIcon != null)
        {
            MaximizeIcon.Text = WindowState == WindowState.Maximized ? "\u29C9" : "\u25A2";
        }
    }
}