using System;

namespace LocalNode.Core.Attributes
{
    /// <summary>
    /// Custom attribute to define the category of a file entity class.
    /// Demonstrates the use of Attributes and Reflection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FileCategoryAttribute(string category) : Attribute
    {
        /// <summary>
        /// Gets the category name.
        /// </summary>
        public string Category { get; } = category;
    }
}
