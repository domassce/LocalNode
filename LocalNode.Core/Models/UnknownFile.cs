using System;
using LocalNode.Core.Attributes;
using LocalNode.Core.Interfaces;

namespace LocalNode.Core.Models
{
    [FileCategory("Unknown")]
    public sealed class UnknownFile : FileEntity
    {
        public static ILogger? GlobalLogger { get; set; }
        public static readonly string DefaultWarning;
        //REIKALAVIMAS
        static UnknownFile()
        {
            DefaultWarning = "Attempted to open an unknown/unsafe file:";
        }
        public UnknownFile(string name, long size) : base(name, size) { }

        public override void Open()
        {
            if (GlobalLogger != null)
            {
                GlobalLogger.LogWarning($"{DefaultWarning} '{Name}' ({Size} bytes)");
            }
        }
    }
}