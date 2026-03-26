using System;
using System;
using System.Collections;
using System.Collections.Generic;
using LocalNode.Core.Interfaces;
using LocalNode.Core.Attributes;

namespace LocalNode.Core.Models
{
    /// <summary>
    /// Represents a compressed archive containing multiple files.
    /// Demonstrates implementing IEnumerable for custom iteration.
    /// </summary>
    [FileCategory("Archive")]
    public sealed class ArchiveFile : FileEntity, IEnumerable<IFileEntity>
    {
        private readonly List<IFileEntity> _archivedFiles = new();

        public int FileCount => _archivedFiles.Count;

        public ArchiveFile(string name, long size) : base(name, size)
        {
        }

        public void AddFile(IFileEntity file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            _archivedFiles.Add(file);
            Size += file.Size;
        }

        public override void Open()
        {
          
            foreach (var file in _archivedFiles)
            {
                Console.WriteLine($"   |- {file.Name} ({file.Size} bytes)");
            }
        }

        public IEnumerator<IFileEntity> GetEnumerator() => _archivedFiles.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}