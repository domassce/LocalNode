using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalFileHosting.Core.Extensions;
using LocalFileHosting.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace LocalFileHosting.UI.ViewModels;

public class HostedFileItem
{
    public string FullPath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string SizeFormatted { get; set; } = string.Empty;
    public bool IsFolder { get; set; }
    public string Icon => IsFolder ? "📁" : "📄";
}

public partial class HostedFilesViewModel : ViewModelBase
{
    public DashboardViewModel? Dashboard { get; set; }
    private readonly FileHostingService _fileService;
    public SettingsViewModel? Settings { get; set; }

    [ObservableProperty] private ObservableCollection<HostedFileItem> _files = new();
    [ObservableProperty][NotifyPropertyChangedFor(nameof(CanNavigateUp))] private string _currentLocalPath = string.Empty;

    public bool CanNavigateUp =>
        Settings != null &&
        !string.IsNullOrEmpty(CurrentLocalPath) &&
        !string.Equals(CurrentLocalPath.TrimEnd(Path.DirectorySeparatorChar),
                       Settings.DefaultHostFolder.TrimEnd(Path.DirectorySeparatorChar),
                       StringComparison.OrdinalIgnoreCase);

    public HostedFilesViewModel(FileHostingService fileService)
    {
        _fileService = fileService;
    }

    private string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
        return $"{len:0.##} {sizes[order]}";
    }

    private long GetDirectorySize(string folderPath)
    {
        try { return Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length); }
        catch { return 0; }
    }

    [RelayCommand]
    public void RefreshFiles()
    {
        if (Dashboard == null || !Dashboard.IsHosting)
        {
            Files.Clear();
            return;
        }

        if (Settings == null || string.IsNullOrWhiteSpace(Settings.DefaultHostFolder) || !Directory.Exists(Settings.DefaultHostFolder))
        {
            Files.Clear();
            return;
        }

        if (string.IsNullOrEmpty(CurrentLocalPath) || !Directory.Exists(CurrentLocalPath))
        {
            CurrentLocalPath = Settings.DefaultHostFolder;
        }

        string folderToScan = CurrentLocalPath;


        Task.Run(() =>
        {
            var newItems = new List<HostedFileItem>();

            try
            {
                // 1. Grab Subfolders and calculate their size!
                foreach (var d in Directory.GetDirectories(folderToScan))
                {
                    long folderSize = GetDirectorySize(d);
                    newItems.Add(new HostedFileItem
                    {
                        FullPath = d,
                        Name = Path.GetFileName(d),
                        FileType = "Folder",
                        SizeFormatted = FormatSize(folderSize), // NO MORE "--"
                        IsFolder = true
                    });
                }

                // 2. Grab Files
                foreach (var f in Directory.GetFiles(folderToScan))
                {
                    var info = new FileInfo(f);
                    var category = FileCategorizer.Categorize(f);
                    newItems.Add(new HostedFileItem
                    {
                        FullPath = f,
                        Name = Path.GetFileName(f),
                        FileType = category.GetType().Name.Replace("File", ""),
                        SizeFormatted = FormatSize(info.Length),
                        IsFolder = false
                    });
                }
            }
            catch { }

            Dispatcher.UIThread.Post(() =>
            {
                Files.Clear();
                foreach (var item in newItems) Files.Add(item);
            });
        });
    }

    public void NavigateIntoFolder(string folderName)
    {
        var newPath = Path.Combine(CurrentLocalPath, folderName);
        if (Directory.Exists(newPath))
        {
            CurrentLocalPath = newPath;
            RefreshFiles();
        }
    }

    [RelayCommand]
    public void NavigateUp()
    {
        if (!CanNavigateUp) return;
        var parentPath = Path.GetDirectoryName(CurrentLocalPath);
        if (!string.IsNullOrEmpty(parentPath))
        {
            CurrentLocalPath = parentPath;
            RefreshFiles();
        }
    }

    [RelayCommand]
    public void OpenFile(string fullPath)
    {
        if (System.IO.File.Exists(fullPath) || Directory.Exists(fullPath))
            Process.Start(new ProcessStartInfo { FileName = fullPath, UseShellExecute = true });
    }

    [RelayCommand]
    public void DeleteFile(string fullPath)
    {
        try
        {
            // Explicitly using System.IO.File and System.IO.Directory
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            else if (System.IO.Directory.Exists(fullPath)) System.IO.Directory.Delete(fullPath, true);

            DashboardViewModel.GlobalLastActionTime = DateTime.Now;
            RefreshFiles();
        }
        catch { }
    }

    public void AddPhysicalFile(string sourceFilePath)
    {
        if (string.IsNullOrEmpty(CurrentLocalPath)) return;
        var destPath = System.IO.Path.Combine(CurrentLocalPath, System.IO.Path.GetFileName(sourceFilePath));

        if (!System.IO.File.Exists(destPath)) System.IO.File.Copy(sourceFilePath, destPath);

        DashboardViewModel.GlobalLastActionTime = DateTime.Now;
        RefreshFiles();
    }
}