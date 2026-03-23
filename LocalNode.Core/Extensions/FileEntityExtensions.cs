using System;
using LocalNode.Core.Interfaces;

namespace LocalNode.Core.Extensions
{
    /// <summary>
    /// Provides extension methods for IFileEntity objects.
    /// </summary>
    public static class FileEntityExtensions
    {
        /// <summary>
        /// Converts the file size in bytes to a human-readable format.
        /// </summary>
        public static string ToHumanReadableSize(this IFileEntity file)
        {
            if (file == null || file.Size == 0) return "0 B";

            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblSByte = file.Size;

            while (dblSByte >= 1024 && i < suffixes.Length - 1)
            {
                dblSByte /= 1024;
                i++;
            }

            return $"{dblSByte:0.##} {suffixes[i]}";
        }

        /// <summary>
        /// Extracts the file extension from the file name.
        /// </summary>
        public static string GetExtension(this IFileEntity file)
        {
            if (file == null || string.IsNullOrWhiteSpace(file.Name))
                return string.Empty;

            int lastDotIndex = file.Name.LastIndexOf('.');
            if (lastDotIndex < 0 || lastDotIndex == file.Name.Length - 1)
                return string.Empty;

            return file.Name.Substring(lastDotIndex).ToLowerInvariant();
        }
    }
}