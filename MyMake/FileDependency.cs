using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MyMake
{
    class FileDependency
    {
        public FileDependency(FileInfo source_file, IEnumerable<DirectoryInfo> include_file_paths)
        {
            SourceFile = source_file;
            IncludeFiles = ExtractIncludeFiles(source_file, include_file_paths).Distinct(new FileInfoComparer()).ToArray();
        }

        public FileInfo SourceFile { get; private set; }
        public IList<FileInfo> IncludeFiles { get; private set; }

        private static IEnumerable<FileInfo> ExtractIncludeFiles(FileInfo source_file, IEnumerable<DirectoryInfo> include_file_paths)
        {
            var include_files = new List<string>();
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
                            include_files.Add(include_file_name);
                        }
                    }
                }
            }
            var found_files = include_files
                .Select(file_name => new[] { source_file.Directory }.Concat(include_file_paths).Select(dir => dir.GetFile(file_name)).Where(file => file.Exists).FirstOrDefault())
                .Where(file => file != null)
                .ToArray();
            return (found_files.SelectMany(file => ExtractIncludeFiles(file, include_file_paths)).Concat(found_files));
        }
    }
}
