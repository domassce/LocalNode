using System;
using System.IO;
using LocalNode.Core.Models;
using LocalNode.Core.Interfaces;

namespace LocalNode.Core.Services
{
    /// <summary>
    /// Provides utility methods to categorize files into specific <see cref="IFileEntity"/> types based on their extensions.
    /// </summary>
    public static class FileCategorizer
    {
        /// <summary>
        /// Categorizes a file located at the specified path into a concrete implementation of <see cref="IFileEntity"/>.
        /// </summary>
        /// <param name="filePath">The full or relative path to the file to be categorized.</param>
        /// <returns>A concrete file model (MediaFile, ArchiveFile, or DocumentFile) based on the file extension.</returns>
        public static IFileEntity Categorize(string filePath)
        {
            var info = new FileInfo(filePath);
            var ext = info.Extension.ToLowerInvariant();

            return ext switch
            {
                ".mp4" or ".avi" or ".mkv" or ".mp3" or ".wav" => new MediaFile(info.Name, info.Length, TimeSpan.Zero),
                ".zip" or ".rar" or ".7z" or ".tar" => new ArchiveFile(info.Name, info.Length),
                ".docx" or ".txt" or ".xlsx" or ".pdf" or ".odt" => new DocumentFile(info.Name, info.Length, "System"),
                _=> new UnknownFile(info.Name, info.Length)
            };
        }
    }
}