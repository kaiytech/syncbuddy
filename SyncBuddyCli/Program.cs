using System;
using System.IO;
using CommandLine;
using SyncBuddyLib;

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

    static void Main(string[] args)
    {
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
}