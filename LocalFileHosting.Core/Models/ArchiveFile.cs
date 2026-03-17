using System;
using System.Collections;
using System.Collections.Generic;
using LocalFileHosting.Core.Interfaces;
using LocalFileHosting.Core.Attributes;

namespace LocalFileHosting.Core.Models
{
    /// <summary>
    /// Represents a compressed archive containing multiple files.
    /// Demonstrates implementing IEnumerable for custom iteration.
    /// </summary>
    [FileCategory("Archive")]
    public class ArchiveFile : FileEntity, IEnumerable<IFileEntity>
    {
        private readonly List<IFileEntity> _archivedFiles = new();

        public int FileCount => _archivedFiles.Count;

        public ArchiveFile(string name) : base(name, 0)
        {
        }

        public ArchiveFile(string name, long size) : base(name, size)
        {
        }

        /// <summary>
        /// Adds a file to the archive and updates the total size.
        /// </summary>
        public void AddFile(IFileEntity file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            
            _archivedFiles.Add(file);
            Size += file.Size; // Archive size grows with its contents
        }

        public override void Open()
        {
            Console.WriteLine($"[Archive] Opening archive '{Name}'. Contains {FileCount} files. Total Size: {Size} bytes.");
            foreach (var file in _archivedFiles)
            {
                Console.WriteLine($"   |- {file.Name} ({file.Size} bytes)");
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the archived files.
        /// </summary>
        public IEnumerator<IFileEntity> GetEnumerator()
        {
            return _archivedFiles.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
