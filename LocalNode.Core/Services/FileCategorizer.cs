using LocalNode.Core.Extensions;
using LocalNode.Core.Interfaces;
using LocalNode.Core.Models;
using System;
using System.IO;

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

        //REIKALAVIMAS
        var (fileName, fileSize) = info;

        var ext = info.Extension.ToLowerInvariant();

        return ext switch
        {
            ".mp4" or ".mp3" => new MediaFile(fileName, fileSize, TimeSpan.Zero),
            ".zip" or ".rar" => new ArchiveFile(fileName, fileSize),
            ".pdf" or ".docx" or ".txt" => new DocumentFile(fileName, fileSize, "System"),
            _ => new UnknownFile(fileName, fileSize)
        };
    }
}