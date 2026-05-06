using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LocalNode.UI.ViewModels;


public partial class LogEntry : ObservableObject
{
    [ObservableProperty] private string _timestamp = string.Empty;
    [ObservableProperty] private string _level = string.Empty;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private string _details = string.Empty;
    [ObservableProperty] private string _colorHex = "#A1A5B7"; 
}

public partial class SystemLogsViewModel : ViewModelBase
{
    private readonly SettingsViewModel _settings;
    private readonly List<LogEntry> _allLogs = new();

    [ObservableProperty] private ObservableCollection<LogEntry> _filteredLogs = new();


    [ObservableProperty] private bool _showInfo = true;
    [ObservableProperty] private bool _showWarn = true;
    [ObservableProperty] private bool _showError = true;
    [ObservableProperty] private string _statusMessage = "Ready";

    public SystemLogsViewModel(SettingsViewModel settings)
    {
        _settings = settings;
        _ = LoadLogsAsync();
    }


    partial void OnShowInfoChanged(bool value) => ApplyFilter();
    partial void OnShowWarnChanged(bool value) => ApplyFilter();
    partial void OnShowErrorChanged(bool value) => ApplyFilter();

    private string GetLogFilePath()
    {
        string dir = string.IsNullOrWhiteSpace(_settings.LogDirectory)
            ? AppDomain.CurrentDomain.BaseDirectory
            : _settings.LogDirectory;
        return Path.Combine(dir, "Log.txt");
    }

    [RelayCommand]
    public async Task LoadLogsAsync()
    {
        StatusMessage = "Loading logs...";

        try
        {
            string path = GetLogFilePath();
            var parsedLogs = new List<LogEntry>();

            if (File.Exists(path))
            {
                await Task.Run(async () =>
                {
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fs);

                    string? line;
                    LogEntry? currentEntry = null;

                    var logPattern = new Regex(@"^\[(INFO|WARN|ERROR)\]\s(\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2})\s-\s(.*)");
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        var match = logPattern.Match(line);
                        if (match.Success)
                        {
                            currentEntry = new LogEntry
                            {
                                Level = match.Groups[1].Value,
                                Timestamp = match.Groups[2].Value,
                                Message = match.Groups[3].Value
                            };

                            currentEntry.ColorHex = currentEntry.Level switch
                            {
                                "INFO" => "#50CD89",
                                "WARN" => "#F6C000",
                                "ERROR" => "#F1416C",
                                _ => "#A1A5B7"
                            };

                            parsedLogs.Add(currentEntry);
                        }
                        else if (currentEntry != null && (line.Trim().StartsWith("Exception") || line.Trim().StartsWith("StackTrace")))
                        {
                            currentEntry.Details += line + Environment.NewLine;
                        }
                    }

                    parsedLogs.Reverse();
                });
            }
            _allLogs.Clear();
            _allLogs.AddRange(parsedLogs);
            ApplyFilter();
            StatusMessage = $"Loaded {_allLogs.Count} entries.";
        }
        catch (Exception ex)
        {
            _allLogs.Add(new LogEntry { Level = "ERROR", Timestamp = DateTime.Now.ToString(), Message = $"Failed to read logs: {ex.Message}", ColorHex = "#F1416C" });
            ApplyFilter();
            StatusMessage = "Error loading logs.";
        }
    }

    private void ApplyFilter()
    {
        FilteredLogs.Clear();
        foreach (var log in _allLogs)
        {
            if (log.Level == "INFO" && !ShowInfo) continue;
            if (log.Level == "WARN" && !ShowWarn) continue;
            if (log.Level == "ERROR" && !ShowError) continue;

            FilteredLogs.Add(log);
        }
    }

    [RelayCommand]
    public void ClearLogs()
    {
        string path = GetLogFilePath();
        try
        {
            if (File.Exists(path))
            {
                File.WriteAllText(path, string.Empty);
            }
            _allLogs.Clear();
            ApplyFilter();
            StatusMessage = "Logs cleared.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error clearing logs: {ex.Message}";
        }
    }
}