using System.Windows;
using GiglingBroadcastDeck.App.ViewModels;

namespace GiglingBroadcastDeck.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();

        Loaded += async (_, _) => await _viewModel.StartAsync();
        Closing += (_, _) => _viewModel.Stop();
    }
}
