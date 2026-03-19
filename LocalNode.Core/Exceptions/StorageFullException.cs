using System;

namespace LocalNode.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when storage is full.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="requiredSpace">Space needed in bytes.</param>
    public class StorageFullException(string message, long requiredSpace)
        : FileHostingException(message)
    {
        /// <summary>Gets the required space.</summary>
        public long RequiredSpace { get; } = requiredSpace;
    }
}