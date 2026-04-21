using System.IO;
using LocalNode.Core.Interfaces;

namespace LocalNode.Core.Extensions;

public static class AppExtensions
{
    //REIKALAVIMAS
    public static string SanitizePath(this string path) => path.Replace('\\', '/');

    //REIKALAVIMAS
    public static void LogEntityAction<T>(this T entity, ILogger logger, string action) where T : IFileEntity
    {
        logger.LogInfo($"[{action}] Generic processing for type {typeof(T).Name}: {entity.Name}");
    }

    //REIKALAVIMAS
    public static void Deconstruct(this FileInfo info, out string name, out long size)
    {
        name = info.Name;
        size = info.Length;
    }
}