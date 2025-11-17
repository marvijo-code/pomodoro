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
            throw new InvalidOperationException("DashboardPage requires a DashboardViewModel parameter.");
        }
    }

    public DashboardViewModel ViewModel
    {
        get => (DashboardViewModel)DataContext;
        set => DataContext = value;
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (Frame?.CanGoBack == true)
        {
            Frame.GoBack();
        }
    }
}