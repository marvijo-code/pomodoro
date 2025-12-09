using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UnoPomodoro.ViewModels;

namespace UnoPomodoro;

public sealed partial class MainPage : Page
{
    private MainViewModel? _viewModel;

    public MainPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is MainViewModel viewModel)
        {
            SetViewModel(viewModel);
        }
    }

    private void SetViewModel(MainViewModel viewModel)
    {
        // Unsubscribe from old view model
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        _viewModel = viewModel;
        this.DataContext = viewModel;

        // Subscribe to property changes
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ShowTasks) && _viewModel?.ShowTasks == true)
        {
            // Focus the TextBox when Tasks panel opens
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                NewTaskTextBox.Focus(FocusState.Programmatic);
            });
        }
    }

    public MainViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (value != null)
            {
                SetViewModel(value);
            }
        }
    }
}
