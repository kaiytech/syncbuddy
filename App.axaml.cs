using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SyncBuddy.ViewModels;
using SyncBuddy.Views;

namespace SyncBuddy;

public partial class App : Application
{
    public static MainWindow MainWindow;
    public static WindowNotificationManager NotificationManager;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        SyncApp.IsActive = SyncApp.ReadConfig();
        
        SyncApp.Load();

        SyncApp.Items.CollectionChanged += (sender, args) => SyncApp.Save();
        SyncApp.Items.CollectionChanged += (sender, args) =>
        {
            if (args.NewItems != null && args.NewItems.Count > 0)
                foreach (SyncItemExtended item in args.NewItems)
                    item.PeriodicCheck(new CancellationTokenSource()); // don't await

            if (args.OldItems != null && args.OldItems.Count > 0)
                foreach (SyncItemExtended item in args.OldItems)
                    item.Dispose();
        };
        
        MainWindow = new MainWindow();
        
        MainWindow.Show();
        
        NotificationManager = new WindowNotificationManager(MainWindow);
        NotificationManager.Position = NotificationPosition.BottomRight;
    }

    private void TrayIcon_OnClicked(object? sender, EventArgs e)
    {
        if (MainWindow.IsVisible)
            MainWindow.Hide();
        else
            MainWindow.Show();
    }
}