using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LocalNode.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for a storage provider.
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>Gets the name of the storage provider.</summary>
        string ProviderName { get; }

        /// <summary>Gets the total storage capacity in bytes.</summary>
        long TotalCapacity { get; }

        /// <summary>Gets the available free space in bytes.</summary>
        long FreeSpace { get; }

        /// <summary>
        /// Saves a file entity to the storage synchronously.
        /// </summary>
        /// <param name="file">The file entity to save.</param>
        /// <returns>True if the operation succeeded; otherwise, false.</returns>
        bool SaveFile(IFileEntity file);

        /// <summary>
        /// Saves a file entity to the storage asynchronously.
        /// </summary>
        /// <param name="file">The file entity to save.</param>
        /// <returns>A task representing the asynchronous operation, containing true if succeeded.</returns>
        Task<bool> SaveFileAsync(IFileEntity file);

        /// <summary>
        /// Deletes a file from storage by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the file to delete.</param>
        /// <returns>True if the file was deleted; otherwise, false.</returns>
        bool DeleteFile(Guid id);

        /// <summary>
        /// Retrieves all file entities currently stored by the provider.
        /// </summary>
        /// <returns>A collection of file entities.</returns>
        IEnumerable<IFileEntity> GetAllFiles();
    }
}