using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LocalNode.UI.ViewModels;

public class RemoteFileItem
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long Size { get; set; }
    public bool IsFolder { get; set; }

    public string Icon => IsFolder ? "📁" : "📄";

    // Dynamically calculate the size for BOTH files and folders!
    public string SizeFormatted
    {
        get
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = Size;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}

public partial class NetworkClientViewModel : ViewModelBase
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly SettingsViewModel _settings;

    [ObservableProperty] private string _serverUrl = "http://localhost:5050";
    [ObservableProperty] private string _roomPassword = string.Empty;
    [ObservableProperty] private string _statusMessage = "Ready to connect.";
    [ObservableProperty] private bool _isConnected = false;

    // Tracks where we are inside the server's folders
    [ObservableProperty] private string _currentRemotePath = string.Empty;

    [ObservableProperty] private ObservableCollection<RemoteFileItem> _remoteFiles = new();

    private CancellationTokenSource? _pingCts;

    public NetworkClientViewModel(SettingsViewModel settings)
    {
        _settings = settings;
    }

    private void AttachClientHeaders(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(RoomPassword)) request.Headers.Add("X-Room-Password", RoomPassword);
        request.Headers.Add("X-User-Name", _settings.DisplayName);
    }

    [RelayCommand]
    public async Task ConnectAsync()
    {
        if (IsConnected) { Disconnect(); return; }

        if (string.IsNullOrWhiteSpace(ServerUrl))
        {
            StatusMessage = "Please enter a valid Server URL.";
            return;
        }

        StatusMessage = "Connecting...";
        CurrentRemotePath = string.Empty; // Reset path to root

        await LoadFilesFromPath(CurrentRemotePath);

        if (IsConnected)
        {
            _pingCts = new CancellationTokenSource();
            _ = StartHealthCheckLoop(_pingCts.Token);
        }
    }

    // Fetches the files for a specific directory path
    public async Task LoadFilesFromPath(string path)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ServerUrl.TrimEnd('/')}/api/files?path={Uri.EscapeDataString(path)}");
            AttachClientHeaders(request);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                StatusMessage = "Connection failed: Incorrect Password.";
                IsConnected = false;
                return;
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var files = JsonSerializer.Deserialize<RemoteFileItem[]>(json, _jsonOptions);

            RemoteFiles.Clear();
            if (files != null) foreach (var f in files) RemoteFiles.Add(f);

            CurrentRemotePath = path;
            IsConnected = true;
            StatusMessage = string.IsNullOrEmpty(path) ? "Viewing Root Directory" : $"Viewing: /{path.Replace("\\", "/")}";
        }
        catch (Exception ex)
        {
            IsConnected = false;
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    // Called when user clicks a folder
    public async Task NavigateIntoFolder(string folderName)
    {
        var newPath = Path.Combine(CurrentRemotePath, folderName);
        await LoadFilesFromPath(newPath);
    }

    // Called when user clicks the "Back" button
    [RelayCommand]
    public async Task NavigateUp()
    {
        if (string.IsNullOrEmpty(CurrentRemotePath)) return; // Already at root

        var parentPath = Path.GetDirectoryName(CurrentRemotePath) ?? string.Empty;
        await LoadFilesFromPath(parentPath);
    }

    private void Disconnect()
    {
        _pingCts?.Cancel();
        IsConnected = false;
        RemoteFiles.Clear();
        StatusMessage = "Disconnected from server.";
    }

    private async Task StartHealthCheckLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && IsConnected)
        {
            try
            {
                await Task.Delay(3000, token);
                var request = new HttpRequestMessage(HttpMethod.Get, $"{ServerUrl.TrimEnd('/')}/api/files?path={Uri.EscapeDataString(CurrentRemotePath)}");
                AttachClientHeaders(request);
                var response = await _httpClient.SendAsync(request, token);
                response.EnsureSuccessStatusCode();
            }
            catch
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (IsConnected)
                    {
                        IsConnected = false;
                        RemoteFiles.Clear();
                        StatusMessage = "Lost connection to the server! Auto-disconnected.";
                    }
                });
                break;
            }
        }
    }

    public async Task DownloadItemAsync(string itemName)
    {
        try
        {
            StatusMessage = $"Downloading {itemName}...";
            var remotePathToDownload = Path.Combine(CurrentRemotePath, itemName);

            var request = new HttpRequestMessage(HttpMethod.Get, $"{ServerUrl.TrimEnd('/')}/api/download?path={Uri.EscapeDataString(remotePathToDownload)}");
            AttachClientHeaders(request);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // Extract the file name from headers (adds .zip if it was a folder!)
            string finalFileName = itemName;
            if (response.Content.Headers.ContentDisposition?.FileName != null)
            {
                finalFileName = response.Content.Headers.ContentDisposition.FileName.Trim('"');
            }

            // We default to Downloads folder for simplicity in this example, you can still use the picker in the View!
            string downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", finalFileName);

            using var stream = await response.Content.ReadAsStreamAsync();
            using var fs = File.Create(downloadPath);
            await stream.CopyToAsync(fs);

            StatusMessage = $"Saved to Downloads: {finalFileName}!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Download failed: {ex.Message}";
        }
    }
}