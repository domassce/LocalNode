using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalFileHosting.Core.Services;
using LocalFileHosting.Core.Models;
using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace LocalFileHosting.UI.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly FileHostingService _fileService;
    private HttpListener? _listener;

    // Stat properties
    [ObservableProperty] private int _totalFiles;
    [ObservableProperty] private string _totalSize = "0 MB";
    [ObservableProperty] private string _lastUpdated = "Never";

    // Form properties
    [ObservableProperty] private string _selectedFolderPath = string.Empty;
    [ObservableProperty] private string _port = "5050";
    [ObservableProperty] private string _roomPassword = string.Empty;

    [ObservableProperty] private bool _isHosting = false;
    [ObservableProperty] private string _serverStatusMessage = "Ready to host.";

    public DashboardViewModel(FileHostingService fileService)
    {
        _fileService = fileService;
        RefreshStats();
    }

    public void RefreshStats()
    {
        var stats = _fileService.GetStats();
        TotalFiles = stats.TotalFiles;
        TotalSize = $"{stats.TotalSize / 1024.0 / 1024.0:F2} MB";
        LastUpdated = stats.LastUpdated.ToString("T");
    }

    [RelayCommand]
    private void ToggleHosting()
    {
        if (IsHosting)
        {
            StopServer();
        }
        else
        {
            StartServer();
        }
    }

    private void StartServer()
    {
        if (string.IsNullOrWhiteSpace(SelectedFolderPath) || !Directory.Exists(SelectedFolderPath))
        {
            ServerStatusMessage = "Please select a valid folder first!";
            return;
        }

        if (!int.TryParse(Port, out int portNumber))
        {
            ServerStatusMessage = "Invalid port number!";
            return;
        }

        // 1. Load files into the Core Service
        var files = Directory.GetFiles(SelectedFolderPath);
        foreach (var f in files)
        {
            _fileService.AddFiles(FileCategorizer.Categorize(f));
        }
        RefreshStats();

        // 2. Start the HttpListener
        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{portNumber}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{portNumber}/");
            _listener.Start();

            IsHosting = true;
            ServerStatusMessage = $"Hosting on port {portNumber}...";

            // Run the listener loop in the background so we don't freeze the UI!
            Task.Run(ListenLoop);
        }
        catch (Exception ex)
        {
            ServerStatusMessage = $"Failed to start: {ex.Message}";
        }
    }

    private void StopServer()
    {
        _listener?.Stop();
        _listener?.Close();
        _listener = null;

        IsHosting = false;
        ServerStatusMessage = "Server stopped.";
    }

    // This is essentially the HandleRequest method from your old ProgramHost.cs
    private async Task ListenLoop()
    {
        while (IsHosting && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();

                // Process the request immediately in a fire-and-forget task
                _ = Task.Run(async () =>
                {
                    var req = context.Request;
                    var res = context.Response;

                    try
                    {
                        // Check Password
                        string? clientPass = req.Headers["X-Room-Password"];
                        if (!string.IsNullOrEmpty(RoomPassword) && clientPass != RoomPassword)
                        {
                            res.StatusCode = 401;
                            res.Close();
                            return;
                        }

                        if (req.Url!.AbsolutePath == "/api/files")
                        {
                            // We get the files from the core service now
                            // Using a little reflection to get the type name, just like the old console app
                            var dto = _fileService.FilterFiles(f => true)
                                .Select(f => new { f.Name, Type = f.GetType().Name, f.Size })
                                .ToList();

                            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(dto);
                            res.ContentType = "application/json";
                            res.ContentLength64 = bytes.Length;
                            res.StatusCode = 200;

                            await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                        }
                        else if (req.Url.AbsolutePath == "/api/download")
                        {
                            var fileName = req.QueryString["name"];
                            var filePath = Path.Combine(SelectedFolderPath, fileName ?? "");

                            if (File.Exists(filePath))
                            {
                                res.ContentType = "application/octet-stream";
                                res.AddHeader("Content-Disposition", $"attachment; filename=\"{Uri.EscapeDataString(fileName!)}\"");
                                using var fs = File.OpenRead(filePath);
                                res.ContentLength64 = fs.Length;
                                await fs.CopyToAsync(res.OutputStream);
                            }
                            else
                            {
                                res.StatusCode = 404;
                            }
                        }
                    }
                    catch
                    {
                        res.StatusCode = 500;
                    }
                    finally
                    {
                        try { res.Close(); } catch { }
                    }
                });
            }
            catch (HttpListenerException)
            {
                // Expected when listener is stopped
                break;
            }
            catch (ObjectDisposedException)
            {
                // Expected when listener is stopped
                break;
            }
        }
    }
}