using UnoPomodoro.ViewModels;

namespace UnoPomodoro;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }
    
    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is MainViewModel viewModel)
        {
            this.DataContext = viewModel;
        }
    }
}
