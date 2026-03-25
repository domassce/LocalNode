using LocalNode.Core.Interfaces;
using LocalNode.UI.ViewModels;
using System;
using System.IO;

namespace LocalNode.UI.Services;

public class FileLogger : ILogger
{
    private readonly SettingsViewModel _settings;
    private readonly object _lockObj = new(); 
    public FileLogger(SettingsViewModel settings)
    {
        _settings = settings;
    }

    private string GetLogFilePath()
    {
        string dir = string.IsNullOrWhiteSpace(_settings.LogDirectory)
            ? AppDomain.CurrentDomain.BaseDirectory
            : _settings.LogDirectory;

        return Path.Combine(dir, "Log.txt");
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

    public void LogInfo(string message) => WriteToFile(level: "INFO", message: message);

    public void LogWarning(string message) => WriteToFile(level: "WARN", message: message);

    public void LogError(string message, Exception? ex = null) => WriteToFile(level: "ERROR", message: message, ex: ex);
}