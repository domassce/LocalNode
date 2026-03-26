using System;

namespace LocalNode.Core.Enums
{
    /// <summary>
    /// Defines the permissions that can be applied to a file or folder.
    /// This enum uses the Flags attribute to allow bitwise combinations.
    /// </summary>
    [Flags]
    public enum FilePermissions
    {
        /// <summary>No permissions granted.</summary>
        None = 0,

        //REIKALAVIMAS
        /// <summary>Permission to read the file.</summary>
        Read = 1 << 0,

        /// <summary>Permission to modify the file.</summary>
        Write = 1 << 1,

        /// <summary>Permission to run the file.</summary>
        Execute = 1 << 2,

        /// <summary>Permission to remove the file.</summary>
        Delete = 1 << 3,

        /// <summary>Permission to share the file with others.</summary>
        Share = 1 << 4,

        /// <summary>All permissions granted.</summary>
        All = Read | Write | Execute | Delete | Share
    }
}
