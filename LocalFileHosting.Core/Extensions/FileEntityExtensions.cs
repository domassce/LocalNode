using System;
using LocalFileHosting.Core.Interfaces;

namespace LocalFileHosting.Core.Extensions
{
    /// <summary>
    /// Provides extension methods for IFileEntity objects.
    /// </summary>
    public static class FileEntityExtensions
    {
        /// <summary>
        /// Converts the file size in bytes to a human-readable format (e.g., KB, MB, GB).
        /// </summary>
        public static string ToHumanReadableSize(this IFileEntity file)
        {
            string[] sizes = ["B", "KB", "MB", "GB", "TB"];
            double len = file.Size;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Extracts the file extension from the file name.
        /// </summary>
        public static string GetExtension(this IFileEntity file)
        {
            if (string.IsNullOrWhiteSpace(file.Name)) return string.Empty;
            
            int lastDotIndex = file.Name.LastIndexOf('.');
            if (lastDotIndex >= 0 && lastDotIndex < file.Name.Length - 1)
            {
                return file.Name[lastDotIndex..];
            }

            return string.Empty;
        }
    }
}
