using System;
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
        
        SyncManager.Load();

        SyncManager.Items.CollectionChanged += (sender, args) => SyncManager.Save();
        
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