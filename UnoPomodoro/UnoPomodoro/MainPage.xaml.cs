using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UnoPomodoro.ViewModels;

namespace UnoPomodoro;

public sealed partial class MainPage : Page
{
    private Frame _navigationFrame;
    private MainViewModel _viewModel;

    public MainPage()
    {
        this.InitializeComponent();
        InitializeNavigation();
    }

    private void InitializeNavigation()
    {
        _navigationFrame = new Frame();
        _navigationFrame.Navigated += OnFrameNavigated;

        // Content is already set from XAML, don't override it
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is MainViewModel viewModel)
        {
            _viewModel = viewModel;
            this.DataContext = viewModel;
        }
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        // Update navigation state if needed
    }

    private void OnTimerClick(object sender, RoutedEventArgs e)
    {
        // Navigate to timer view (current view)
        // This is already the main view, so no navigation needed
    }

    private void OnDashboardClick(object sender, RoutedEventArgs e)
    {
        // Navigate to dashboard
        if (_navigationFrame != null)
        {
            var dashboardViewModel = new DashboardViewModel(
                _viewModel.SessionRepository,
                _viewModel.TaskRepository,
                _viewModel.StatisticsService);

            _navigationFrame.Navigate(typeof(DashboardPage), dashboardViewModel);
            Content = _navigationFrame;
        }
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
