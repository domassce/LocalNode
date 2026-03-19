using LocalFileHosting.Core.Interfaces;
using LocalFileHosting.UI.ViewModels;
using System;
using System.IO;

namespace LocalFileHosting.UI.Services;

public class FileLogger : ILogger
{
    private readonly SettingsViewModel _settings;
    private readonly object _lockObj = new(); // Prevents multiple threads from writing at the exact same millisecond

    // We pass the SettingsViewModel in so the logger always knows the latest directory!
    public FileLogger(SettingsViewModel settings)
    {
        _settings = settings;
    }

    private string GetLogFilePath()
    {
        // If the user hasn't set a log directory, save it right next to the .exe file
        string dir = string.IsNullOrWhiteSpace(_settings.LogDirectory)
            ? AppDomain.CurrentDomain.BaseDirectory
            : _settings.LogDirectory;

        return Path.Combine(dir, "LocalFileHost_Logs.txt");
    }

    private void WriteToFile(string level, string message, Exception? ex = null)
    {
        lock (_lockObj)
        {
            try
            {
                string path = GetLogFilePath();
                using var writer = File.AppendText(path);

                writer.WriteLine($"[{level}] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");

                if (ex != null)
                {
                    writer.WriteLine($"        Exception: {ex.Message}");
                    writer.WriteLine($"        StackTrace: {ex.StackTrace}");
                }
            }
            catch
            {
                // If logging fails (e.g. folder was deleted by user), we safely ignore it to prevent crashing the app
            }
        }
    }

    public void LogInfo(string message) => WriteToFile("INFO", message);

    public void LogWarning(string message) => WriteToFile("WARN", message);

    public void LogError(string message, Exception? ex = null) => WriteToFile("ERROR", message, ex);
}