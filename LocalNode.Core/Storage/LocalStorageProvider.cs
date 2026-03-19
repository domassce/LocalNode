using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LocalNode.Core.Interfaces;
using LocalNode.Core.Exceptions;

namespace LocalNode.Core.Storage
{
    /// <summary>
    /// Implements storage provider for local disk.
    /// Simulates capacity constraints.
    /// </summary>
    public class LocalStorageProvider : IStorageProvider
    {
        private readonly Dictionary<Guid, IFileEntity> _storage = new();

        /// <summary>
        /// Gets the display name of the storage provider.
        /// </summary>
        /// <value>Always returns "Local Disk Storage".</value>
        public string ProviderName => "Local Disk Storage";

        /// <summary>
        /// Gets the total storage capacity of the local disk partition in bytes.
        /// </summary>
        public long TotalCapacity { get; }

        /// <summary>
        /// Gets the currently available free space in bytes.
        /// </summary>
        /// <remarks>
        /// This value is calculated dynamically by subtracting the sum of all stored file sizes 
        /// from the total capacity defined for this provider.
        /// </remarks>
        public long FreeSpace
        {
            get
            {
                // REIKALAVIMAS: Naudojama LINQ operacija (Sum)
                long usedSpace = _storage.Values.Sum(f => f.Size);
                return TotalCapacity - usedSpace;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalStorageProvider"/> class with a fixed capacity.
        /// </summary>
        /// <param name="capacityBytes">The maximum storage capacity in bytes.</param>
        public LocalStorageProvider(long capacityBytes)
        {
            TotalCapacity = capacityBytes;
        }

        /// <summary>
        /// Saves a file to the local storage if there is sufficient free space.
        /// </summary>
        /// <param name="file">The file entity to be saved.</param>
        /// <returns>True if the file was saved successfully.</returns>
        /// <exception cref="StorageFullException">Thrown when the file size exceeds the available free space.</exception>
        public bool SaveFile(IFileEntity file)
        {
            if (file.Size > FreeSpace)
            {
                throw new StorageFullException($"Not enough space in {ProviderName}.", file.Size);
            }

            _storage[file.Id] = file;
            return true;
        }

        /// <summary>
        /// Asynchronously saves a file to the local storage.
        /// </summary>
        /// <param name="file">The file entity to be saved.</param>
        /// <returns>A task representing the asynchronous operation, containing true if successful.</returns>
        public Task<bool> SaveFileAsync(IFileEntity file)
        {
            // Simulate disk I/O delay
            return Task.FromResult(SaveFile(file));
        }

        /// <summary>
        /// Removes a file from the local storage by its unique identifier.
        /// </summary>
        /// <param name="id">The unique ID of the file to delete.</param>
        /// <returns>True if the file was found and removed; otherwise, false.</returns>
        public bool DeleteFile(Guid id)
        {
            return _storage.Remove(id);
        }

        /// <summary>
        /// Retrieves a list of all files currently stored in this provider.
        /// </summary>
        /// <returns>An enumerable collection of <see cref="IFileEntity"/>.</returns>
        public IEnumerable<IFileEntity> GetAllFiles()
        {
            return _storage.Values.ToList();
        }
    }
}
