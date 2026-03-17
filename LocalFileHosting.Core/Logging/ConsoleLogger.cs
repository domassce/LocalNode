using System;
using LocalFileHosting.Core.Interfaces;

namespace LocalFileHosting.Core.Logging
{
    /// <summary>
    /// Implementation of ILogger that writes to the console with colors.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        public void LogInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss} - {message}");
            Console.ResetColor();
        }

        public void LogWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN] {DateTime.Now:HH:mm:ss} - {message}");
            Console.ResetColor();
        }

        public void LogError(string message, Exception? ex = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} - {message}");
            if (ex != null)
            {
                Console.WriteLine($"        Exception: {ex.Message}");
                Console.WriteLine($"        StackTrace: {ex.StackTrace}");
            }
            Console.ResetColor();
        }
    }
}
