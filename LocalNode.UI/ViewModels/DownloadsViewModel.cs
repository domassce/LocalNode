using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace LocalNode.UI.ViewModels;

public enum DownloadState { Queued, Downloading, Paused, Completed, Error }

public partial class DownloadTaskItem : ObservableObject
{
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private string _localPath = string.Empty;
    [ObservableProperty] private string _serverUrl = string.Empty;
    [ObservableProperty] private long _totalBytes;
    [ObservableProperty] private long _downloadedBytes;
    [ObservableProperty] private double _progressPercentage;
    [ObservableProperty] private string _speedMessage = "Waiting...";
    [ObservableProperty] private DownloadState _state = DownloadState.Queued;

    public CancellationTokenSource? Cts { get; set; }
    public SettingsViewModel? Settings { get; set; }

    partial void OnStateChanged(DownloadState value)
    {
        OnPropertyChanged(nameof(IsCompleted));
        OnPropertyChanged(nameof(IsNotCompleted));
    }

    public bool IsCompleted => State == DownloadState.Completed;
    public bool IsNotCompleted => State != DownloadState.Completed;
}

public partial class DownloadsViewModel : ViewModelBase
{
    private static readonly HttpClient _httpClient = new() { Timeout = Timeout.InfiniteTimeSpan };

    [ObservableProperty] private ObservableCollection<DownloadTaskItem> _activeDownloads = new();
    [RelayCommand]
    public void OpenFolder(DownloadTaskItem item)
    {
        try
        {
            string? dir = Path.GetDirectoryName(item.LocalPath);
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
            {
               
                Process.Start(new ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            item.SpeedMessage = $"Error opening folder: {ex.Message}";
        }
    }

    [RelayCommand]
    public void DeleteFile(DownloadTaskItem item)
    {
        item.Cts?.Cancel();
        ActiveDownloads.Remove(item);

        if (File.Exists(item.LocalPath))
        {
            try { File.Delete(item.LocalPath); } catch { }
        }
    }
    [RelayCommand]
    public void PauseDownload(DownloadTaskItem item)
    {
        if (item.State == DownloadState.Downloading)
        {
            item.Cts?.Cancel();
            item.State = DownloadState.Paused;
            item.SpeedMessage = "Paused";
        }
    }

    [RelayCommand]
    public void ResumeDownload(DownloadTaskItem item)
    {
        if (item.State == DownloadState.Paused || item.State == DownloadState.Error)
        {
            _ = ProcessDownloadAsync(item);
        }
    }

    [RelayCommand]
    public async Task CancelDownloadAsync(DownloadTaskItem item)
    {
        item.Cts?.Cancel();
        ActiveDownloads.Remove(item);
        await Task.Delay(500);
        if (File.Exists(item.LocalPath))
        {
            try
            {
                File.Delete(item.LocalPath);
            }
            catch
            {
            }
        }
    }

    [RelayCommand]
    public void ClearCompleted()
    {
        for (int i = ActiveDownloads.Count - 1; i >= 0; i--)
        {
            if (ActiveDownloads[i].State == DownloadState.Completed)
                ActiveDownloads.RemoveAt(i);
        }
    }

    public void AddNewDownload(string fileName, long totalSize, string targetUrl, SettingsViewModel settings)
    {
        string downloadDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        string localPath = Path.Combine(downloadDir, fileName);

        var taskItem = new DownloadTaskItem
        {
            FileName = fileName,
            TotalBytes = totalSize,
            ServerUrl = targetUrl,
            LocalPath = localPath,
            Settings = settings
        };


        if (File.Exists(localPath))
        {
            taskItem.DownloadedBytes = new FileInfo(localPath).Length;
            if (taskItem.DownloadedBytes < taskItem.TotalBytes)
            {
                taskItem.State = DownloadState.Paused;
                taskItem.ProgressPercentage = (double)taskItem.DownloadedBytes / taskItem.TotalBytes * 100;
                taskItem.SpeedMessage = "Paused (Resumable)";
            }
            else
            {
                taskItem.State = DownloadState.Completed;
                taskItem.ProgressPercentage = 100;
                taskItem.SpeedMessage = "Already downloaded";
            }
        }

        ActiveDownloads.Add(taskItem);

        if (taskItem.State != DownloadState.Completed && taskItem.State != DownloadState.Paused)
        {
            _ = ProcessDownloadAsync(taskItem);
        }
    }

    private async Task ProcessDownloadAsync(DownloadTaskItem item)
    {
        item.State = DownloadState.Downloading;
        if (item.FileName.EndsWith(".zip") && item.TotalBytes == 0)
        {
            item.SpeedMessage = "Zipping on server (this takes time)...";
        }
        else
        {
            item.SpeedMessage = "Starting...";
        }       
        item.Cts = new CancellationTokenSource();

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, item.ServerUrl);

           
            if (item.Settings != null)
            {
                request.Headers.Add("X-Room-Password", item.Settings.RoomPassword);
                request.Headers.Add("X-User-Name", string.IsNullOrWhiteSpace(item.Settings.DisplayName) ? "Anonymous" : item.Settings.DisplayName);
            }

            if (item.DownloadedBytes > 0)
            {
                request.Headers.Range = new RangeHeaderValue(item.DownloadedBytes, null);
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, item.Cts.Token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength.HasValue)
            {
                item.TotalBytes = response.Content.Headers.ContentLength.Value;
            }
            using var stream = await response.Content.ReadAsStreamAsync(item.Cts.Token);

           
            using var fs = new FileStream(item.LocalPath, item.DownloadedBytes > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None);

            byte[] buffer = new byte[81920];
            int bytesRead;
            var sw = Stopwatch.StartNew();
            long bytesSinceLastUpdate = 0;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, item.Cts.Token)) > 0)
            {
                await fs.WriteAsync(buffer, 0, bytesRead, item.Cts.Token);

                item.DownloadedBytes += bytesRead;
                bytesSinceLastUpdate += bytesRead;
                item.ProgressPercentage = item.TotalBytes > 0 ? ((double)item.DownloadedBytes / item.TotalBytes * 100) : 100;

                if (sw.ElapsedMilliseconds >= 1000)
                {
                    double speedBps = bytesSinceLastUpdate / (sw.ElapsedMilliseconds / 1000.0);
                    item.SpeedMessage = FormatSize((long)speedBps) + "/s";
                    bytesSinceLastUpdate = 0;
                    sw.Restart();
                }
            }

            item.State = DownloadState.Completed;
            item.SpeedMessage = "Completed!";
            item.ProgressPercentage = 100;

            // Log to Database using Entity Framework
            using var db = new LocalNode.Core.Database.NodeDbContext();
            db.Database.EnsureCreated();
            db.DownloadLogs.Add(new LocalNode.Core.Database.DownloadLog { FileName = item.FileName, DownloadedAt = DateTime.Now });
            db.SaveChanges();
        }
        catch (TaskCanceledException)
        {
            item.State = DownloadState.Paused;
            item.SpeedMessage = "Paused";
        }
        catch (Exception ex)
        {
            item.State = DownloadState.Error;
            item.SpeedMessage = $"Error: {ex.Message}";
        }
    }

    private string FormatSize(long size)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = size;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}