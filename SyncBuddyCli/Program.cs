using CommandLine;
using NLog;
using SyncBuddyLib;

namespace SyncBuddyCli;

class Program
{
    class Options
    {
        [Option('s', "source", Required = true, HelpText = "Source directory")]
        public string SourceDirectory { get; set; }

        [Option('t', "target", Required = true, HelpText = "Target directory")]
        public string TargetDirectory { get; set; }

        [Option('i', "interval", HelpText = "Interval in minutes", Required = false)]
        public int Interval { get; set; } = 0;

        [Option('l', "log", Required = false, HelpText = "Log file path")]
        public string Log { get; set; } = string.Empty;
    }

    private static Logger _logger;

    static void Main(string[] args)
    {
        try
        {
            var config = new NLog.Config.LoggingConfiguration();

            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, new NLog.Targets.ConsoleTarget("logconsole"));
            NLog.LogManager.Configuration = config;

            _logger = NLog.LogManager.GetCurrentClassLogger();

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options =>
                {
                    if (!Directory.Exists(options.SourceDirectory))
                    {
                        Console.WriteLine($"Source directory '{options.SourceDirectory}' does not exist.");
                        return;
                    }

                    if (!Directory.Exists(options.TargetDirectory))
                    {
                        Console.WriteLine($"Target directory '{options.TargetDirectory}' does not exist.");
                        return;
                    }

                    if (!SyncItem.AreDirectoriesIndependent(options.SourceDirectory, options.TargetDirectory))
                    {
                        Console.WriteLine($"Provided directories are not independent.");
                        return;
                    }

                    if (options.Interval < 0)
                    {
                        Console.WriteLine($"Interval (when set) has to be a positive integer.");
                        return;
                    }

                    if (options.Log != string.Empty)
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(options.Log)))
                        {
                            try
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(options.Log));
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Failed to create log directory => {e.Message}");
                                return;
                            }
                        }

                        try
                        {
                            File.AppendAllText(options.Log, "");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Filed to write to log file => {e.Message}");
                            return;
                        }

                        var logfile = new NLog.Targets.FileTarget("logfile") { FileName = options.Log };
                        config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logfile);
                        NLog.LogManager.Configuration = config;
                    }

                    var syncItem = new SyncItem(0, options.SourceDirectory, options.TargetDirectory, DateTime.Now, true,
                        options.Interval);

                    syncItem.CurrentLog.CollectionChanged += (sender, eventArgs) =>
                    {
                        if (eventArgs.NewItems is null || eventArgs.NewItems.Count == 0) return;
                        foreach (string item in eventArgs.NewItems)
                            _logger.Info(item);
                    };

                    using var cts = new CancellationTokenSource();
                    Console.CancelKeyPress += (_, e) =>
                    {
                        e.Cancel = true;
                        cts.Cancel();
                    };

                    if (options.Interval > 0)
                    {
                        _logger.Info($"Interval is set. Running the job now, then every {options.Interval} minutes.");
                    } 
                    
                    try
                    {
                        syncItem.Sync().Wait();
                        if (options.Interval > 0)
                            syncItem.PeriodicCheck(cts).Wait();
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Operation canceled.");
                        return;
                    }
                    catch (AggregateException e)
                    {
                        Console.WriteLine($"Operation failed: {e.Message}");
                        return;
                    }
                })
                .WithNotParsed(errors =>
                {
                    foreach (var error in errors)
                    {
                        Console.WriteLine(error);
                    }
                });
        }
        finally
        {
            LogManager.Shutdown();
        }
    }
}