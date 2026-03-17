using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LocalFileHosting.Core.Interfaces;
using LocalFileHosting.Core.Models;
using LocalFileHosting.Core.Enums;
using LocalFileHosting.Core.Exceptions;
using LocalFileHosting.Core.Records;

namespace LocalFileHosting.Core.Services
{
    /// <summary>
    /// Core service for managing files in the hosting system.
    /// </summary>
    public class FileHostingService
    {
        private readonly ILogger _logger;
        private readonly IStorageProvider _storage;
        
        // REIKALAVIMAS: Naudojamos duomenų struktūros iš System.Collections.Generic (1 t.)
        private readonly Dictionary<Guid, IFileEntity> _fileIndex;

        /// <summary>
        /// Occurs when a new file is successfully added to the storage provider.
        /// </summary>
        public event EventHandler<IFileEntity>? FileAdded;

        /// <summary>
        /// Occurs when a file is deleted from the storage provider. 
        /// The event argument contains the unique identifier of the deleted file.
        /// </summary>
        public event EventHandler<Guid>? FileDeleted;

        /// <summary>
        /// REIKALAVIMAS: Naudojamas statinis konstruktorius (1 t.)
        /// </summary>
        static FileHostingService()
        {
            Console.WriteLine("[System] FileHostingService static initialization complete.");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileHostingService"/> class.
        /// </summary>
        /// <param name="logger">The logging service used for tracking system operations.</param>
        /// <param name="storage">The storage provider implementation used for file persistence.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> or <paramref name="storage"/> is null.</exception>
        public FileHostingService(ILogger logger, IStorageProvider storage)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _fileIndex = new Dictionary<Guid, IFileEntity>();

            _logger.LogInfo($"FileHostingService initialized with {_storage.ProviderName}");
        }

        /// <summary>
        /// REIKALAVIMAS: Naudojamas raktažodis 'params' (0.5 t.)
        /// </summary>
        public void AddFiles(params IFileEntity[] files)
        {
            if (files == null) return;

            foreach (var file in files)
            {
                try
                {
                    if (_storage.SaveFile(file))
                    {
                        _fileIndex[file.Id] = file;
                        _logger.LogInfo($"Added file: {file.Name}");
                        FileAdded?.Invoke(this, file);
                    }
                }
                catch (StorageFullException ex)
                {
                    _logger.LogError($"Failed to add {file.Name}. Storage full. Required: {ex.RequiredSpace} bytes.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Unexpected error adding {file.Name}", ex);
                }
            }
        }

        /// <summary>
        /// Demonstrates asynchronous programming.
        /// </summary>
        public async Task<bool> UploadDocumentAsync(string name, long size, bool isHidden = false, string author = "Anonymous")
        {
            _logger.LogInfo($"Starting async upload for {name}...");
            var doc = new DocumentFile(name, size, author);
            
            if (isHidden) doc.Permissions &= ~FilePermissions.Read;

            try
            {
                bool success = await _storage.SaveFileAsync(doc);
                if (success)
                {
                    _fileIndex[doc.Id] = doc;
                    FileAdded?.Invoke(this, doc);
                    _logger.LogInfo($"Async upload complete for {name}.");
                    return true;
                }
            }
            catch (StorageFullException ex)
            {
                _logger.LogError($"Async upload failed. Storage full. Required: {ex.RequiredSpace} bytes.");
            }
            
            return false;
        }

        /// <summary>
        /// Attempts to retrieve a file by its unique identifier.
        /// This method demonstrates the use of the 'out' parameter modifier,
        /// which allows a method to return multiple values (a boolean indicating success,
        /// and the actual file entity if found).
        /// REIKALAVIMAS: Realizuota inicializacija naudojant 'out' argumentus (1 t.)
        /// </summary>
        /// <param name="id">The unique identifier of the file to retrieve.</param>
        /// <param name="file">When this method returns, contains the file entity associated with the specified ID, if the ID is found; otherwise, null. This parameter is passed uninitialized.</param>
        /// <returns>true if the file hosting service contains a file with the specified ID; otherwise, false.</returns>
        public bool TryGetFile(Guid id, out IFileEntity file)
        {
            return _fileIndex.TryGetValue(id, out file!);
        }

        /// <summary>
        /// Deletes a file from the system by its unique identifier.
        /// </summary>
        /// <param name="id">The unique <see cref="Guid"/> of the file to be removed.</param>
        /// <returns>True if the file was found and deleted; otherwise, false.</returns>
        public bool DeleteFile(Guid id)
        {
            if (_fileIndex.Remove(id))
            {
                _storage.DeleteFile(id);
                FileDeleted?.Invoke(this, id);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Filters files based on a provided condition using a delegate.
        /// This demonstrates the power of functional programming in C# using LINQ and lambda expressions.
        /// By passing a Func delegate, the caller can define custom filtering logic without modifying this class.
        /// REIKALAVIMAS: Naudojami delegatai arba lambda funkcijos (1.5 t.)
        /// </summary>
        /// <param name="predicate">A function to test each file entity for a condition.</param>
        /// <returns>An IEnumerable containing files that satisfy the condition.</returns>
        public IEnumerable<IFileEntity> FilterFiles(Func<IFileEntity, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return _storage.GetAllFiles().Where(predicate);
        }

        /// <summary>
        /// Processes all files and performs actions based on their type and properties.
        /// This method heavily utilizes modern C# pattern matching features.
        /// </summary>
        public void ProcessFiles()
        {
            _logger.LogInfo("--- Processing Files ---");
            var files = _storage.GetAllFiles().ToList();
            
            foreach (var file in files)
            {
                // REIKALAVIMAS: Naudojamas operatorius 'is' (0.5 t.)
                // The 'is' operator checks if the runtime type of an object is compatible with a given type.
                // Here, we also use declaration pattern matching to assign it to the 'doc' variable.
                if (file is DocumentFile doc)
                {
                    _logger.LogInfo($"[Process] Found Document: {doc.Name} by {doc.Author}");
                }

                // REIKALAVIMAS: Naudojamas šablonų atitikimas (1 t.)
                // REIKALAVIMAS: Naudojate 'switch' su 'when' raktažodžiu (0.5 t.)
                // The switch expression here uses type patterns and relational patterns combined with 'when' clauses
                // to execute complex conditional logic cleanly.
                switch (file)
                {
                    case MediaFile media when media.Size > 100_000_000:
                        _logger.LogWarning($"[Process] HUGE Media File detected: {media.Name} ({media.Size / 1024 / 1024} MB)");
                        break;
                    
                    case MediaFile media when media.Duration.TotalMinutes > 60:
                        _logger.LogInfo($"[Process] Long Media File: {media.Name} (> 1 hour)");
                        break;
                        
                    case ArchiveFile archive when archive.FileCount > 10:
                        _logger.LogInfo($"[Process] Large Archive: {archive.Name} contains {archive.FileCount} files.");
                        break;
                        
                    case DocumentFile { WordCount: > 10000 } largeDoc:
                        // Property pattern matching: matches DocumentFile where WordCount > 10000
                        _logger.LogInfo($"[Process] Lengthy Document: {largeDoc.Name} ({largeDoc.WordCount} words)");
                        break;
                        
                    case null:
                        _logger.LogWarning("[Process] Encountered a null file reference.");
                        break;
                }
            }
        }

        /// <summary>
        /// Gets a specified number of the most recently added files.
        /// Demonstrates the use of the C# 8.0 Range operator (..).
        /// REIKALAVIMAS: Naudojate 'Range' tipą (0.5 t.)
        /// </summary>
        /// <param name="count">The number of recent files to retrieve.</param>
        /// <returns>An array of recent file entities.</returns>
        public IFileEntity[] GetRecentFiles(int count)
        {
            var array = _storage.GetAllFiles().ToArray();
            if (array.Length == 0) return Array.Empty<IFileEntity>();
            
            // Calculate the starting index, ensuring it doesn't go below 0
            int start = Math.Max(0, array.Length - count);
            
            // Using Range operator '..' to slice the array from 'start' to the end ('^0')
            return array[start..^0]; 
        }

        /// <summary>
        /// Demonstrates the use of various null-checking operators in C#.
        /// </summary>
        /// <param name="id">The file ID.</param>
        /// <returns>The file name or a default string.</returns>
        public string GetFileNameOrDefault(Guid id)
        {
            _fileIndex.TryGetValue(id, out var file);
            
            // REIKALAVIMAS: Naudojami operatoriai ?. ir ?? (0.5 t.)
            // ?. is the null-conditional operator. It returns null if 'file' is null, instead of throwing a NullReferenceException.
            // ?? is the null-coalescing operator. It returns the left-hand operand if it is not null; otherwise, it returns the right-hand operand.
            string name = file?.Name ?? "Unknown_File_Name";
            
            // REIKALAVIMAS: Naudojamas operatorius ??= (0.5 t.)
            // ??= is the null-coalescing assignment operator. It assigns the value of its right-hand operand to its left-hand operand only if the left-hand operand evaluates to null.
            string? cachedName = null;
            cachedName ??= name;
            
            return cachedName;
        }
        
        /// <summary>
        /// Demonstrates the null-conditional index operator.
        /// </summary>
        /// <returns>The first file entity, or null if the collection is empty.</returns>
        public IFileEntity? GetFirstFileOrNull()
        {
            var files = _storage.GetAllFiles().ToList();
            IFileEntity[]? array = files.Count > 0 ? files.ToArray() : null;
            
            // REIKALAVIMAS: Naudojamas operatorius ?[] (0.5 t.)
            // ?[] is the null-conditional index operator. It accesses the array element only if the array is not null.
            return array?[0];
        }

        /// <summary>
        /// Checks if a file has a specific permission using bitwise operations.
        /// REIKALAVIMAS: Naudojamos bitinės operacijos (1 t.)
        /// </summary>
        /// <param name="file">The file entity.</param>
        /// <param name="permission">The permission to check.</param>
        /// <returns>True if the file has the permission, false otherwise.</returns>
        public bool HasPermission(IFileEntity file, FilePermissions permission)
        {
            // Bitwise AND (&) checks if the specific bits of 'permission' are set in 'file.Permissions'
            return (file.Permissions & permission) == permission;
        }

        /// <summary>
        /// Grants a specific permission to a file using bitwise operations.
        /// </summary>
        public void GrantPermission(IFileEntity file, FilePermissions permission)
        {
            // Bitwise OR (|) sets the bits of 'permission' in 'file.Permissions'
            file.Permissions |= permission;
        }

        /// <summary>
        /// Revokes a specific permission from a file using bitwise operations.
        /// </summary>
        public void RevokePermission(IFileEntity file, FilePermissions permission)
        {
            // Bitwise AND with bitwise NOT (& ~) clears the bits of 'permission' in 'file.Permissions'
            file.Permissions &= ~permission;
        }

        /// <summary>
        /// Returns system statistics using a Record type.
        /// </summary>
        public FileStatsRecord GetStats()
        {
            var allFiles = _storage.GetAllFiles().ToList();
            return new FileStatsRecord(
                TotalFiles: allFiles.Count,
                TotalSize: allFiles.Sum(f => f.Size),
                LastUpdated: DateTime.UtcNow
            );
        }
    }
}
