using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using SyncBuddy.ViewModels;
using SyncBuddy.Views;

namespace SyncBuddy;

public partial class App : Application
{
    public static MainWindow MainWindow;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        MainWindow = new MainWindow();
    }

    private void TrayIcon_OnClicked(object? sender, EventArgs e)
    {
        if (MainWindow.IsVisible)
            MainWindow.Hide();
        else
            MainWindow.Show();
    }
}