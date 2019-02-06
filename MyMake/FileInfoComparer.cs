using System.Collections.Generic;
using System.IO;

namespace MyMake
{
    class FileInfoComparer
        : IEqualityComparer<DirectoryInfo>, IEqualityComparer<FileInfo>
    {
        public bool Equals(DirectoryInfo x, DirectoryInfo y)
        {
            if (x == null)
                return (y == null);
            else if (y == null)
                return (false);
            else
                return (x.FullName.Equals(y.FullName));
        }

        public bool Equals(FileInfo x, FileInfo y)
        {
            if (x == null)
                return (y == null);
            else if (y == null)
                return (false);
            else
                return (x.FullName.Equals(y.FullName));
        }

        public int GetHashCode(DirectoryInfo obj)
        {
            return (obj.FullName.GetHashCode());
        }

        public int GetHashCode(FileInfo obj)
        {
            return (obj.FullName.GetHashCode());
        }
    }
}
