using System;
using LocalNode.Core.Enums;

namespace LocalNode.Core.Interfaces
{
    /// <summary>
    /// Represents a generic file entity in the hosting system.
    /// </summary>
    public interface IFileEntity
    {
        /// <summary>Gets the unique identifier for the file.</summary>
        Guid Id { get; }

        /// <summary>Gets the name of the file.</summary>
        string Name { get; }

        /// <summary>Gets the size of the file in bytes.</summary>
        long Size { get; }

        /// <summary>Gets the date and time when the file was created.</summary>
        DateTime CreatedAt { get; }

        /// <summary>Gets or sets the permissions associated with the file.</summary>
        FilePermissions Permissions { get; set; }

        /// <summary>
        /// Renames the file entity.
        /// </summary>
        /// <param name="newName">The new name for the file.</param>
        void Rename(string newName);

        /// <summary>
        /// Opens the file entity for viewing or processing.
        /// </summary>
        void Open();
    }
}