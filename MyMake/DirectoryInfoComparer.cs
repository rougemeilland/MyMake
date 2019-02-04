using System.Collections.Generic;
using System.IO;

namespace MyMake
{
    class DirectoryInfoComparer
        : IEqualityComparer<DirectoryInfo>
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

        public int GetHashCode(DirectoryInfo obj)
        {
            return (obj.FullName.GetHashCode());
        }
    }
}
