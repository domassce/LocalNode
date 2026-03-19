using System;
using LocalNode.Core.Attributes;

namespace LocalNode.Core.Models
{
    /// <summary>
    /// Represents an unknown file.
    /// </summary>
    [FileCategory("Unknown")]
    public sealed class UnknownFile : FileEntity
    {
        public UnknownFile(string name, long size) : base(name, size) {}
        public override void Open()
        {
            Console.WriteLine($"Cannot open unknown file.");
        }

    }
}