using System;
using System.IO;
using Avalonia;

namespace EpisodeMatcherGui;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Write any unhandled exception to a log file next to the exe,
        // so silent crashes on Windows produce a visible crash.log.
        var logPath = Path.Combine(
            AppContext.BaseDirectory, "crash.log");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            try { File.WriteAllText(logPath, e.ExceptionObject?.ToString() ?? "unknown"); }
            catch { }
        };

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            try { File.WriteAllText(logPath, ex.ToString()); } catch { }
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
