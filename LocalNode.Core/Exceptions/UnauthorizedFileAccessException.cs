using System;

namespace LocalNode.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when a user attempts to access a file without proper permissions.
    /// </summary>
    public class UnauthorizedFileAccessException : FileHostingException
    {
        /// <summary>
        /// Gets the ID of the file that was denied access.
        /// </summary>
        public Guid FileId { get; }

        public UnauthorizedFileAccessException(string message, Guid fileId) : base(message)
        {
            FileId = fileId;
        }
    }
}
