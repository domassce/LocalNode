using System;

namespace LocalNode.Core.Records
{
    /// <summary>
    /// A record type representing statistics about the file hosting system.
    /// Demonstrates C# 9+ Record types for immutable data transfer objects.
    /// </summary>
    public record FileStatsRecord(int TotalFiles, long TotalSize, DateTime LastUpdated);
}
