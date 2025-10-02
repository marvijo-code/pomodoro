using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UnoPomodoro.Data.Repositories;
using UnoPomodoro.Services;
using UnoPomodoro.ViewModels;
using System;

namespace UnoPomodoro;

public sealed partial class DashboardPage : Page
{
    public DashboardPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
        }
        else
        {
            // Create default view model
            var sessionRepository = e.NavigationMode == NavigationMode.New ?
                new SessionRepository(null) : null;
            var taskRepository = e.NavigationMode == NavigationMode.New ?
                new TaskRepository(null) : null;
            var statisticsService = e.NavigationMode == NavigationMode.New ?
                new StatisticsService(sessionRepository, taskRepository) : null;

            ViewModel = new DashboardViewModel(
                sessionRepository,
                taskRepository,
                statisticsService);
        }
    }

    public DashboardViewModel ViewModel
    {
        get => (DashboardViewModel)DataContext;
        set => DataContext = value;
    }
}