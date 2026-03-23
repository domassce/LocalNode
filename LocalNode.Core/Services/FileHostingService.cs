using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LocalNode.Core.Interfaces;
using LocalNode.Core.Models;
using LocalNode.Core.Enums;
using LocalNode.Core.Records;
namespace LocalNode.Core.Services
{
    public class FileHostingService
    {
        private readonly ILogger _logger;
        private DateTime _lastScanTime = DateTime.MinValue;
        public FileHostingService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInfo("[System] FileHostingService instance initialized.");
        }


        public IEnumerable<IFileEntity> GetFilesInDirectory(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return Enumerable.Empty<IFileEntity>();

            var files = Directory.GetFiles(folderPath);
            var entities = new List<IFileEntity>();

            foreach (var f in files)
            {
                entities.Add(FileCategorizer.Categorize(f));
            }

            _lastScanTime = DateTime.Now;
            return entities;
        }


        public long GetDirectorySize(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return 0;
            return Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                            .Sum(f => new FileInfo(f).Length);
        }


        public void AddPhysicalFiles(string destinationFolder, params string[] sourceFilePaths)
        {
            if (!Directory.Exists(destinationFolder)) return;

            foreach (var sourcePath in sourceFilePaths)
            {
                if (File.Exists(sourcePath))
                {
                    var destPath = Path.Combine(destinationFolder, Path.GetFileName(sourcePath));
                    if (!File.Exists(destPath)) File.Copy(sourcePath, destPath);
                }
            }
            _logger.LogInfo($"Added {sourceFilePaths.Length} files to {destinationFolder}");
        }


        public bool DeletePhysicalFile(string fullPath)
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInfo($"Deleted file: {fullPath}");
                return true;
            }
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
                _logger.LogInfo($"Deleted directory: {fullPath}");
                return true;
            }
            return false;
        }


        public bool TryGetPhysicalFile(string filePath, out IFileEntity fileEntity)
        {
            if (File.Exists(filePath))
            {
                fileEntity = FileCategorizer.Categorize(filePath);
                return true;
            }
            fileEntity = null!;
            return false;
        }


        public IEnumerable<IFileEntity> FilterFiles(string folderPath, Func<IFileEntity, bool> predicate)
        {
            return GetFilesInDirectory(folderPath).Where(predicate);
        }


        public IFileEntity[]? GetRecentFiles(string folderPath, int count)
        {
            var files = GetFilesInDirectory(folderPath).ToArray();
            if (files.Length == 0) return null;

            int start = Math.Max(0, files.Length - count);
            var recent = files[start..^0];
            var firstRecent = recent?[0];
            return recent;
        }


        public void AnalyzeDirectory(string folderPath)
        {
            var files = GetFilesInDirectory(folderPath).ToList();

            foreach (var file in files)
            {
                if (file is DocumentFile doc)
                {
                    _logger.LogInfo($"Found Document: {doc.Name}");
                }


                switch (file)
                {
                    case MediaFile media when media.Size > 1024 * 1024 * 50:
                        _logger.LogWarning($"HUGE Media File: {media.Name}");
                        break;
                    case ArchiveFile archive when archive.Size < 1024:
                        _logger.LogInfo($"Tiny Archive detected: {archive.Name}");
                        break;
                    case DocumentFile { WordCount: > 0 } largeDoc:
                        _logger.LogInfo($"Document with mapped words: {largeDoc.Name}");
                        break;
                }
            }
        }

        public bool CanReadFile(string filePath)
        {
            if (TryGetPhysicalFile(filePath, out var fileEntity))
            {
                bool hasPermission = (fileEntity.Permissions & FilePermissions.Read) == FilePermissions.Read;

                if (!hasPermission)
                {
                    _logger.LogWarning($"[Security] Access denied. User attempted to open '{filePath}' without Read permissions.");
                }
                else
                {
                    _logger.LogInfo($"[Action] User successfully opened '{fileEntity.Name}'.");
                }
                return hasPermission;
            }

            return Directory.Exists(filePath);
        }
    
    public FileStatsRecord GetSystemStats(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return new FileStatsRecord(0, 0);

            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            return new FileStatsRecord(files.Length, totalSize);
        }
    } 
}