using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UnoPomodoro.ViewModels;

namespace UnoPomodoro;

public sealed partial class MainPage : Page
{
    private MainViewModel _viewModel;

    public MainPage()
    {
        this.InitializeComponent();
    }

    private Frame? RootFrame => App.RootFrame;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is MainViewModel viewModel)
        {
            _viewModel = viewModel;
            this.DataContext = viewModel;
        }
    }

    private void OnTimerClick(object sender, RoutedEventArgs e)
    {
        // Navigate to timer view (current view)
        // This is already the main view, so no navigation needed
    }

    private void OnDashboardClick(object sender, RoutedEventArgs e)
    {
        // Navigate to dashboard
        if (_viewModel == null)
        {
            return;
        }

        var dashboardViewModel = new DashboardViewModel(
            _viewModel.SessionRepository,
            _viewModel.TaskRepository,
            _viewModel.StatisticsService);

        RootFrame?.Navigate(typeof(DashboardPage), dashboardViewModel);
    }

    private void OnHistoryClick(object sender, RoutedEventArgs e)
    {
        // Navigate to history view
        // For now, show history on current page
        if (_viewModel != null)
        {
            _viewModel.ShowHistory = !_viewModel.ShowHistory;
        }
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        // Navigate to settings view
        // For now, just show a message
        if (_viewModel != null)
        {
            // Settings functionality would be implemented here
        }
    }

    public MainViewModel ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;
            DataContext = value;
        }
    }
}
