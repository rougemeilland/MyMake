using System.IO;

namespace MyMake
{
    class DirectoryPathSetting
    {
        public DirectoryPathSetting(DirectoryInfo value, string on)
        {
            Value = value;
            On = on;
        }
        public DirectoryInfo Value { get; private set; }
        public string On { get; private set; }
    }

}
