using System;
using LocalNode.Core.Interfaces;
using LocalNode.Core.Enums;
using LocalNode.Core.Attributes;

namespace LocalNode.Core.Models
{
    /// <summary>
    /// Base abstract class for all file types in the system.
    /// </summary>
    [FileCategory("Generic")]
    public abstract class FileEntity : IFileEntity
    {
        public Guid Id { get; protected set; }
        public string Name { get; protected set; }
        public long Size { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public FilePermissions Permissions { get; set; }

        protected FileEntity(string name, long size)
        {
            Id = Guid.NewGuid();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Size = size;
            CreatedAt = DateTime.UtcNow;
            Permissions = FilePermissions.Read | FilePermissions.Write;
        }

        public virtual void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("New name cannot be empty.", nameof(newName));
            
            Name = newName;
        }

        public abstract void Open();
    }
}
