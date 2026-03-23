using System;
using System.IO;
using LocalNode.Core.Interfaces;
using LocalNode.Core.Models;

namespace LocalNode.Core.Services;

/// <summary>
/// Provides utility methods to categorize files into specific <see cref="IFileEntity"/> types based on their extensions.
/// </summary>
public static class FileCategorizer
{
    /// <summary>
    /// Categorizes a file located at the specified path into a concrete implementation of <see cref="IFileEntity"/>.
    /// </summary>
    public static IFileEntity Categorize(string filePath)
    {
        var info = new FileInfo(filePath);
        var ext = info.Extension.ToLowerInvariant();

        return ext switch
        {
            ".mp4" or ".avi" or ".mkv" or ".webm" or ".mp3" or ".wav" or ".flac"
                => new MediaFile(info.Name, info.Length, TimeSpan.Zero),
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz"
                => new ArchiveFile(info.Name, info.Length),
            ".pdf" or ".docx" or ".doc" or ".txt" or ".xlsx" or ".csv"
                => new DocumentFile(name: info.Name, size: info.Length, author: "System"),
            _
                => new UnknownFile(info.Name, info.Length)
        };
    }
}