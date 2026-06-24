using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GiglingBroadcastDeck.App.ViewModels;

namespace GiglingBroadcastDeck.App;

/// <summary>
/// Main operator window for race selection, overlay control, and rundown management.
/// </summary>
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

    private void RundownItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox { SelectedItem: string rundownItem })
        {
            return;
        }

        if (_viewModel.SelectRundownRaceCommand.CanExecute(rundownItem))
        {
            _viewModel.SelectRundownRaceCommand.Execute(rundownItem);
        }
    }
}
