using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalNode.Core.Interfaces;
using LocalNode.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace LocalNode.UI.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{

    public static DateTime GlobalLastActionTime { get; set; } = DateTime.MinValue;
    private readonly FileHostingService _fileService;
    private readonly SettingsViewModel _settings;
    private readonly ILogger _logger;
    private HttpListener? _listener;
    private readonly DiscoveryService _discoveryService = new();

    [ObservableProperty] private int _totalFiles;
    [ObservableProperty] private string _totalSize = "0 MB";
    [ObservableProperty] private string _lastUpdated = "Never";

    [ObservableProperty] private bool _isHosting = false;
    [ObservableProperty] private string _serverStatusMessage = "Ready to host.";


    public DashboardViewModel(FileHostingService fileService, SettingsViewModel settings, ILogger logger)
    {
        _fileService = fileService;
        _settings = settings;
        _logger = logger;
    }
    public void RefreshStats()
    {
        if (!IsHosting || string.IsNullOrWhiteSpace(_settings.DefaultHostFolder) || !Directory.Exists(_settings.DefaultHostFolder))
        {
            TotalFiles = 0;
            TotalSize = "0 B";
            return;
        }

        Task.Run(() =>
        {
            var stats = _fileService.GetSystemStats(_settings.DefaultHostFolder);

            Dispatcher.UIThread.Post(() =>
            {
                TotalFiles = stats.TotalFiles;
                TotalSize = FormatSize(stats.TotalSizeBytes);

                LastUpdated = GlobalLastActionTime == DateTime.MinValue
                    ? "Just now"
                    : GlobalLastActionTime.ToString("HH:mm:ss");
            });
        });
    }
    private string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
        return $"{len:0.##} {sizes[order]}";
    }

    private bool _isBusy = false;

    [RelayCommand]
    private async Task ToggleHostingAsync()
    {
        if (_isBusy) return;

        _isBusy = true;

        if (IsHosting)
        {
            StopServer();
            _discoveryService.Stop();
        }
        else
        {
            StartServer();
            bool requiresPassword = !string.IsNullOrWhiteSpace(_settings.RoomPassword);
            string nodeName = !string.IsNullOrWhiteSpace(_settings.DisplayName) ? _settings.DisplayName : "LocalNode User";

            _discoveryService.StartAnnouncing(nodeName, 5050, requiresPassword);
        }

        await Task.Delay(500);
        _isBusy = false;
    }

    private void StartServer()
    {
        string folder = _settings.DefaultHostFolder;
        string portStr = _settings.DefaultPort;

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            ServerStatusMessage = "Please set a valid Hosting Folder in Settings!";
            return;
        }

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:5050/");
            _listener.Start();

            IsHosting = true;
            GlobalLastActionTime = DateTime.Now;
            ServerStatusMessage = $"Hosting on port {portStr}...";
            _logger.LogInfo($"[Server] Started hosting on port {portStr}. Folder: {folder}");

            Task.Run(ListenLoop);
            RefreshStats();
        }
        catch (Exception ex)
        {
            ServerStatusMessage = $"Failed to start: {ex.Message}";
            _logger.LogError("[Server] Failed to start HTTP Listener.", ex);
        }
    }

    private void StopServer()
    {
        _listener?.Stop();
        _listener?.Close();
        _listener = null;

        IsHosting = false;
        ServerStatusMessage = "Server stopped.";


        RefreshStats();
    }

    private async Task ListenLoop()
    {
        while (IsHosting && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();

                _ = Task.Run(async () =>
                {
                    var req = context.Request;
                    var res = context.Response;
                    string clientIp = req.RemoteEndPoint?.ToString() ?? "Unknown IP";
                    string clientName = req.Headers["X-User-Name"] ?? "Anonymous Client";

                    try
                    {
                        string requiredPass = _settings.RoomPassword;
                        if (!string.IsNullOrEmpty(requiredPass) && req.Headers["X-Room-Password"] != requiredPass)
                        {
                            res.StatusCode = 401;
                            res.Close();
                            return;
                        }

                        string requestedPath = req.QueryString["path"] ?? "";
                        string targetPhysicalPath = Path.GetFullPath(Path.Combine(_settings.DefaultHostFolder, requestedPath));

                        // Security check
                        if (!targetPhysicalPath.StartsWith(Path.GetFullPath(_settings.DefaultHostFolder)))
                        {
                            res.StatusCode = 403;
                            return;
                        }

                        if (req.Url!.AbsolutePath == "/api/files")
                        {
                            var items = new List<object>();

                            if (Directory.Exists(targetPhysicalPath))
                            {
                                // 1. Add Folders
                                foreach (var d in Directory.GetDirectories(targetPhysicalPath))
                                {
                                    items.Add(new { Name = Path.GetFileName(d), Type = "Folder", Size = 0L, IsFolder = true });
                                }
                                // 2. Add Files
                                foreach (var f in Directory.GetFiles(targetPhysicalPath))
                                {
                                    var info = new FileInfo(f);
                                    var cat = FileCategorizer.Categorize(f).GetType().Name.Replace("File", "");
                                    items.Add(new { Name = info.Name, Type = cat, Size = info.Length, IsFolder = false });
                                }
                            }

                            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(items);
                            res.ContentType = "application/json";
                            res.ContentLength64 = bytes.Length;
                            res.StatusCode = 200;
                            await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                        }
                        else if (req.Url.AbsolutePath == "/api/download")
                        {
                            if (Directory.Exists(targetPhysicalPath))
                            {
                                // ZIP FOLDER ON THE FLY
                                _logger.LogInfo($"[Network] {clientName} ({clientIp}) is downloading folder '{requestedPath}'.");
                                string tempZip = Path.GetTempFileName() + ".zip";
                                ZipFile.CreateFromDirectory(targetPhysicalPath, tempZip);

                                res.ContentType = "application/zip";
                                res.AddHeader("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(targetPhysicalPath)}.zip\"");
                                using var fs = File.OpenRead(tempZip);
                                res.ContentLength64 = fs.Length;
                                await fs.CopyToAsync(res.OutputStream);
                                fs.Close();
                                File.Delete(tempZip); // Clean up temp file
                            }
                            else if (File.Exists(targetPhysicalPath))
                            {
                                // STANDARD FILE DOWNLOAD
                                _logger.LogInfo($"[Network] {clientName} ({clientIp}) is downloading '{requestedPath}'.");
                                res.ContentType = "application/octet-stream";
                                res.AddHeader("Content-Disposition", $"attachment; filename=\"{Uri.EscapeDataString(Path.GetFileName(targetPhysicalPath))}\"");
                                using var fs = File.OpenRead(targetPhysicalPath);
                                res.ContentLength64 = fs.Length;
                                await fs.CopyToAsync(res.OutputStream);
                            }
                            else { res.StatusCode = 404; }
                        }
                    }
                    catch { res.StatusCode = 500; }
                    finally { try { res.Close(); } catch { } }
                });
            }
            catch { break; }
        }
    }
}