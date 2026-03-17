using System;
using LocalFileHosting.Core.Attributes;

namespace LocalFileHosting.Core.Models
{
    /// <summary>
    /// Represents a media file (e.g., Video, Audio).
    /// </summary>
    [FileCategory("Media")]
    public sealed class MediaFile : FileEntity
    {
        public TimeSpan Duration { get; set; }
        public int Bitrate { get; set; }
        public string Resolution { get; set; } = "Unknown";

        public MediaFile(string name, long size, TimeSpan duration) : base(name, size)
        {
            Duration = duration;
        }

        public override void Open()
        {
            Console.WriteLine($"[Player] Playing media: '{Name}'. Duration: {Duration.TotalMinutes:F2} mins. Resolution: {Resolution}");
        }
    }
}
