using System;
using LocalNode.Core.Attributes;

namespace LocalNode.Core.Models
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
        }
    }
}
