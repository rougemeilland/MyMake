using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MyMake
{
    class FileDependency
    {
        public FileDependency(FileInfo source_file, DirectoryInfo[] include_file_path)
        {
            SourceFile = source_file;
            IncludeFiles = new List<FileInfo>();

            using (var reader = source_file.OpenText())
            {
                while (true)
                {
                    var text = reader.ReadLine();
                    if (text == null)
                        break;
                    if (text.StartsWith("#include"))
                    {
                        text = text.Substring("#include".Length).Trim();
                        if (text.StartsWith("\""))
                        {
                            var length = text.IndexOf("\"", 1) - 1;
                            var include_file_name = text.Substring(1, length);
                            var include_file = include_file_path.Select(dir => dir.GetFile(include_file_name)).Where(file => file.Exists).FirstOrDefault();
                            if (include_file == null)
                                throw new ApplicationException(string.Format("インクルードファイル'{0}'が見つかりません。", include_file_name));
                            IncludeFiles.Add(include_file);
                        }
                    }
                }
            }
        }

        public FileInfo SourceFile { get; private set; }
        public IList<FileInfo> IncludeFiles { get; private set; }
    }

}
