using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using SyncBuddy.ViewModels;
using SyncBuddyLib;

namespace SyncBuddy.Views;

public partial class MainWindow : Window
{
    public WindowNotificationManager NotificationManager = new WindowNotificationManager();
    
    private PixelPoint _cachedPos;
    private MainWindowViewModel _viewModel = new MainWindowViewModel();
    public SyncItemEditWindow? EditWindow = null;
    
    public MainWindow()
    {
        if (Design.IsDesignMode)
            DataContext = _viewModel;
        
        foreach (var item in _viewModel.SyncItems)
        {
            item.SyncStatus = SyncStatus.Synced;
        }
        
        InitializeComponent();
        
        PositionWindow();

        if (Design.IsDesignMode)
            Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
        
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) =>
        {
            foreach (var item in _viewModel.SyncItems)
            {
                item.UpdateTimes();
            }
        };
        timer.Start();
    }
    
    private void PositionWindow()
    {
        var workingArea = Screens.Primary.WorkingArea;
        _cachedPos = new PixelPoint(workingArea.Width - (int)Width - 20, workingArea.Height - (int)Height - 50);
        Position = _cachedPos;
    }

    private void TopLevel_OnOpened(object? sender, EventArgs e)
    {
        DataContext = _viewModel;
        
        Activate();
        Focus();
    }

    private void WindowBase_OnDeactivated(object? sender, EventArgs e)
    {
        //Hide();
    }

    public async Task OpenEditDialog(SyncItemExtended item)
    {
        EditWindow = new SyncItemEditWindow(item);
        await EditWindow.ShowDialog(this);
        EditWindow?.Close();
        EditWindow = null;
        _viewModel.CallChanged();
    }

    private async void NewEntryButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var newItem = new SyncItemExtended(SyncManager.Items.Any() ? SyncManager.Items.Max(_ => _.Id) + 1 : 0, "", "");
        await OpenEditDialog(newItem);
        if (newItem.SourceDir == "")
            return;
        
        SyncManager.Items.Add(newItem);
    }
}