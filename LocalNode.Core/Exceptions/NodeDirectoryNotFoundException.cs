using System;

namespace LocalNode.Core.Exceptions;

//REIKALAVIMAS
public class NodeDirectoryNotFoundException : Exception
{
    public string MissingPath { get; }

    public NodeDirectoryNotFoundException(string path)
        : base($"Critical error: Node directory not found '{path}'.")
    {
        MissingPath = path;
    }
}