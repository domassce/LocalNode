using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalNode.Core.Exceptions;
using LocalNode.Core.Extensions;
using LocalNode.Core.Interfaces;
using LocalNode.Core.Models;
using LocalNode.Core.Services;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LocalNode.UI.ViewModels
{
    public partial class HostedFileItem : ObservableObject
    {
        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _fileType = string.Empty;
        [ObservableProperty] private string _sizeFormatted = string.Empty;
        [ObservableProperty] private string _fullPath = string.Empty;
        [ObservableProperty] private bool _isFolder;
    }

    public partial class HostedFilesViewModel : ViewModelBase
    {
        private readonly FileHostingService _fileService;
        public SettingsViewModel Settings { get; set; } = null!;
        public DashboardViewModel Dashboard { get; set; } = null!;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanNavigateUp))]
        private string _currentLocalPath = string.Empty;

        [ObservableProperty]
        private ObservableCollection<HostedFileItem> _files = new();
        public bool CanNavigateUp =>
            !string.IsNullOrEmpty(CurrentLocalPath) &&
            Settings != null &&
            !string.IsNullOrWhiteSpace(Settings.DefaultHostFolder) &&
            !string.Equals(
                CurrentLocalPath.TrimEnd(Path.DirectorySeparatorChar),
                Settings.DefaultHostFolder.TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);

        public HostedFilesViewModel(FileHostingService fileService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        [RelayCommand]
        public void RefreshFiles()
        {
            Files.Clear();

            if (Settings == null || string.IsNullOrWhiteSpace(Settings.DefaultHostFolder) || !Directory.Exists(Settings.DefaultHostFolder))
                return;

            if (string.IsNullOrEmpty(CurrentLocalPath) || !Directory.Exists(CurrentLocalPath))
                CurrentLocalPath = Settings.DefaultHostFolder;

            var folderToScan = CurrentLocalPath;

            Task.Run(() =>
            {
                var newItems = new List<HostedFileItem>();
                string folderToScan = string.IsNullOrEmpty(CurrentLocalPath) ? Settings.DefaultHostFolder : CurrentLocalPath;

                try
                {
                    foreach (var d in Directory.GetDirectories(folderToScan)) { /* jūsų esamas kodas */ }

                    //REIKALAVIMAS
                    NodeFileCollection<IFileEntity> entities = _fileService.GetFilesInDirectory(folderToScan);

                    //REIKALAVIMAS
                    var bigFiles = entities.GetLargeFiles(1024 * 1024).ToList();
                    if (bigFiles.Any()) Console.WriteLine($"Found {bigFiles.Count} large files using yield iterator.");

                    foreach (var entity in entities)
                    {
                        newItems.Add(new HostedFileItem
                        {
                            Name = entity.Name,
                            FullPath = Path.Combine(folderToScan, entity.Name),
                            FileType = entity.GetType().Name.Replace("File", ""),
                            SizeFormatted = LocalNode.Core.Extensions.FileEntityExtensions.ToHumanReadableSize(entity),
                            IsFolder = false
                        });
                    }
                }
                //REIKALAVIMAS
                catch (NodeDirectoryNotFoundException ex)
                {
                    Console.WriteLine($"UI caught an exception: {ex.MissingPath}");
                }
                Dispatcher.UIThread.Post(() =>
                {
                    foreach (var item in newItems)
                    {
                        Files.Add(item);
                    }
                    OnPropertyChanged(nameof(CanNavigateUp));
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
            if (!_fileService.CanReadFile(fullPath))
            {
                return;
            }
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = fullPath,
                        UseShellExecute = true
                    });
                }
                catch
                {
                }
            }
        }
        [RelayCommand]
        public void DeleteFile(string fullPath)
        {
            if (_fileService.DeletePhysicalFile(fullPath))
            {
                RefreshFiles();
                Dashboard?.RefreshStats();
            }
        }

        public void AddPhysicalFile(string sourceFilePath)
        {
            if (string.IsNullOrEmpty(CurrentLocalPath)) return;
            _fileService.AddPhysicalFiles(CurrentLocalPath, sourceFilePath);
            RefreshFiles();
            Dashboard?.RefreshStats();
        }

        private string FormatSize(long bytes)
        {
            if (bytes == 0) return "0 B";
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblSByte = bytes;
            while (dblSByte >= 1024 && i < suffixes.Length - 1)
            {
                dblSByte /= 1024;
                i++;
            }
            return $"{dblSByte:0.##} {suffixes[i]}";
        }
    }
}