using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

    private Queue<string> _logCache = new Queue<string>();
    private SemaphoreSlim _lock = new(1, 1);
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        var config = new NLog.Config.LoggingConfiguration();
        var logfile = new NLog.Targets.FileTarget("logfile") { FileName = Path.Combine(SyncApp.AppSettingsDir, "app.log") };
        config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logfile);
        config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, new NLog.Targets.ConsoleTarget("logconsole"));
        NLog.LogManager.Configuration = config;

        var logger = NLog.LogManager.GetCurrentClassLogger();
        logger.Info("SyncBuddy started");

        //if (ApplicationLifetime is IControlledApplicationLifetime controlledLifetime)
        //    controlledLifetime.Exit += (sender, args) => Log.CloseAndFlush();

        SyncApp.IsActive = SyncApp.ReadConfig();
        
        SyncApp.Load();

        SyncApp.Items.CollectionChanged += (sender, args) => SyncApp.Save();
        
        
        SyncApp.Items.CollectionChanged += (sender, args) =>
        {
            if (args.NewItems != null && args.NewItems.Count > 0)
                foreach (SyncItemExtended item in args.NewItems)
                {
                    item.CurrentLog.CollectionChanged += (o, eventArgs) =>
                    {
                        // WeirdLogging:
                        // NLog does not like when Logs are created outside
                        // the main thread. It prints to the console, yes, but 
                        // refuses to print to the file. To work around this issue,
                        // we put cached logs in a list and then dequeue every 50ms
                        //
                        // This has downsides, like potentially inaccurate log timing
                        // or decreased app speed. :( But it works.
                        #region WeirdLogging
                        if (eventArgs.NewItems != null && eventArgs.NewItems.Count > 0)
                        {
                            foreach (string logItem in eventArgs.NewItems)
                            {
                                _logCache.Enqueue($"Sync #{item.Id}: {logItem}");
                            }
                        }
                        #endregion
                    };
                    
                    item.PeriodicCheck(new CancellationTokenSource()); // don't await
                }

            if (args.OldItems != null && args.OldItems.Count > 0)
                foreach (SyncItemExtended item in args.OldItems)
                    item.Dispose();
        };

        // see above for the comment about WeirdLogging.
        #region WeirdLogging
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        timer.Tick += (_, _) =>
        {
            while (_logCache.Count > 0)
            {
                logger.Info(new string(_logCache.Dequeue()));
            }
        };
        timer.Start();
        #endregion
        
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