using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Material.Icons;
using SyncBuddy.ViewModels;
using SyncBuddyApp.Views;
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
        
        InitializeComponent();

        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (_viewModel.AppActive)
            {
                SyncApp.IsActive = true;
                SyncApp.UpdateConfig(true);
                ActiveIcon.Kind = MaterialIconKind.RadioButtonChecked;
                ActiveIcon.Foreground = new SolidColorBrush(Colors.GreenYellow);
                ActiveText.Text = "Active";
            }
            else
            {
                SyncApp.IsActive = false;
                SyncApp.UpdateConfig(false);
                ActiveIcon.Kind = MaterialIconKind.RadioButtonUnchecked;
                ActiveIcon.Foreground = new SolidColorBrush(Colors.IndianRed);
                ActiveText.Text = "Inactive";
            }
        };
        
        _viewModel.AppActive = SyncApp.IsActive;
        
        foreach (var item in _viewModel.SyncItems)
        {
            item.SyncStatus = SyncStatus.Synced;
        }
        
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
        if (EditWindow is null)
            Hide();
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
        var newItem = new SyncItemExtended(SyncApp.Items.Any() ? SyncApp.Items.Max(_ => _.Id) + 1 : 0, "", "", DateTime.Now, true, 10);
        await OpenEditDialog(newItem);
        if (newItem.SourceDir == "")
            return;
        
        SyncApp.Items.Add(newItem);
    }

    private void ActiveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _viewModel.AppActive = !_viewModel.AppActive;
        
        if (_viewModel.AppActive)
            App.NotificationManager?.Show(new Notification("Syncing is now ACTIVE", "Expect things to happen", NotificationType.Information));
        else
            App.NotificationManager?.Show(new Notification("Syncing is now INACTIVE", "Things won't happen anymore", NotificationType.Information));

    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("explorer.exe", Path.GetFullPath(SyncApp.AppSettingsDir))
            { UseShellExecute = true });
    }

    private void ExitButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var lifetime = Application.Current?.ApplicationLifetime as IControlledApplicationLifetime;
        lifetime?.Shutdown();
    }

    private void AboutButton_OnClick(object? sender, RoutedEventArgs e)
    {
        new AboutWindow().Show();
    }
}