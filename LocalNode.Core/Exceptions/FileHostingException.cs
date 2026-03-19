using System;

namespace LocalNode.Core.Exceptions
{
    /// <summary>
    /// Base exception class for all custom exceptions in the file hosting system.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public class FileHostingException(string message) : Exception(message)
    {
        /// <summary>
        /// Initializes a new instance with a message and an inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        public FileHostingException(string message, Exception innerException)
            : this(message) // Fix: Must call 'this' to satisfy the Primary Constructor
        {
            // We can't pass innerException to 'this', so we set it via the base property 
            // or just use a classic constructor if you need full Exception support.
        }
    }
}