using System;

namespace LocalNode.Core.Interfaces
{
    /// <summary>
    /// Defines a simple logging contract.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a message with the Information severity level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogInfo(string message);

        /// <summary>
        /// Logs a message with the Warning severity level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogWarning(string message);

        /// <summary>
        /// Logs a message with the Error severity level, optionally including exception details.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="ex">An optional exception to include in the log entry.</param>
        void LogError(string message, Exception? ex = null);
    }
}