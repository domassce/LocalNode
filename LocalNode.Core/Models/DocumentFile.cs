using System;
using LocalNode.Core.Interfaces;
using LocalNode.Core.Attributes;

namespace LocalNode.Core.Models
{
    /// <summary>
    /// Represents a document file (e.g., PDF, Word, Text).
    /// </summary>
    [FileCategory("Document")]
    public sealed class DocumentFile : FileEntity, IComparable<DocumentFile>, IEquatable<DocumentFile>, IFormattable
    {
        public string Author { get; set; }
        public int WordCount { get; set; }


        public DocumentFile(string name, long size, string author = "Unknown") : base(name, size)
        {
            Author = author;
        }

        public override void Open()
        {
            Console.WriteLine($"[Viewer] Opening document: '{Name}' authored by {Author}. Size: {Size} bytes.");
        }

        #region IComparable<T> Implementation
        public int CompareTo(DocumentFile? other)
        {
            if (other == null) return 1;
            int sizeComparison = this.Size.CompareTo(other.Size);
            if (sizeComparison != 0) return sizeComparison;
            return string.Compare(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }
        #endregion

        #region IEquatable<T> Implementation
        public bool Equals(DocumentFile? other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            return this.Id == other.Id;
        }
        public override bool Equals(object? obj) => Equals(obj as DocumentFile);
        public override int GetHashCode() => Id.GetHashCode();
        #endregion

        #region IFormattable Implementation
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            if (string.IsNullOrEmpty(format)) format = "G";
            switch (format.ToUpperInvariant())
            {
                case "G":
                case "N": return $"{Name} ({Size} bytes)";
                case "D": return $"Document: {Name} | Author: {Author} | Created: {CreatedAt:yyyy-MM-dd}";
                case "S": return $"{Name} [{Size / 1024.0:F2} KB]";
                default: throw new FormatException($"The '{format}' format string is not supported.");
            }
        }
        #endregion

        #region Deconstructor
        public void Deconstruct(out string name, out long size, out string author, out int wordCount)
        {
            name = Name;
            size = Size;
            author = Author;
            wordCount = WordCount;
        }
        #endregion

        #region Operator Overloading
        public static bool operator ==(DocumentFile? left, DocumentFile? right)
        {
            if (ReferenceEquals(left, null)) return ReferenceEquals(right, null);
            return left.Equals(right);
        }
        public static bool operator !=(DocumentFile? left, DocumentFile? right) => !(left == right);

        public static DocumentFile operator +(DocumentFile left, DocumentFile right)
        {
            if (left == null || right == null) throw new ArgumentNullException("Cannot add null documents.");
            return new DocumentFile($"{left.Name}_{right.Name}", left.Size + right.Size, $"{left.Author} & {right.Author}")
            {
                WordCount = left.WordCount + right.WordCount
            };
        }
        #endregion
    }
}
