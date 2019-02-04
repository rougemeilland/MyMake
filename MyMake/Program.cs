using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MyMake
{
    class Program
    {

        static void Main(string[] args)
        {
            FileInfo mymakefile =  new DirectoryInfo( Directory.GetCurrentDirectory()).GetFile("mymakefile.xml");
            if (mymakefile.Exists)
                Open(mymakefile);
        }

        private static void Open(FileInfo mymakefile)
        {
            var setting = new MyMakeFileSetting(mymakefile);

            OpenMakefile(mymakefile, setting, "x86", "Debug");
            OpenMakefile(mymakefile, setting, "x86", "Release");
            OpenMakefile(mymakefile, setting, "x64", "Debug");
            OpenMakefile(mymakefile, setting, "x64", "Release");

            Console.ReadLine();
        }

        private static void OpenMakefile(FileInfo mymakefile, MyMakeFileSetting setting, string platform, string config)
        {
            var makefile = CreateMakefile(mymakefile, setting, platform, config);
            ExecuteMake(mymakefile.Directory, makefile, setting, platform);
        }

        private static FileInfo CreateMakefile(FileInfo mymakefile, MyMakeFileSetting setting, string platform, string config)
        {
            var base_dir = mymakefile.Directory;
            var target_file = base_dir.Parent.GetDirectory("dist").GetDirectory(string.Format("{0}_{1}", platform, config)).GetFile(setting.TargetFileName);
            var object_dir = base_dir.GetDirectory("build").GetDirectory(string.Format("{0}_{1}", platform, config));
            var map_file = object_dir.GetFile(Path.GetFileNameWithoutExtension(target_file.Name) + ".map");
            var makefile = base_dir.GetDirectory("myproject").GetFile(string.Format("Makefile.{0}_{1}.mk", platform, config));

            if (makefile.Exists && makefile.LastWriteTimeUtc >= mymakefile.LastWriteTimeUtc)
                return (makefile);

            var dependencies = base_dir.EnumerateFiles("*.c", SearchOption.AllDirectories)
                               .Select(file => new FileDependency(file, new[] { file.Directory }));

            var file_infos = dependencies
                             .Select(dep => new
                             {
                                 source_file = dep.SourceFile,
                                 include_files = dep.IncludeFiles,
                                 object_file = object_dir.GetFile(Path.GetFileNameWithoutExtension(dep.SourceFile.Name) + ".o"),
                                 assembly_file = object_dir.GetFile(Path.GetFileNameWithoutExtension(dep.SourceFile.Name) + ".s"),
                                 i_file = object_dir.GetFile(Path.GetFileNameWithoutExtension(dep.SourceFile.Name) + ".i"),
                             })
                             .Where(item => setting.SourceFileFilters.All(reg => !reg.IsMatch(base_dir.GetRelativePath(item.source_file).Replace('\\', '/'))));

            if (!makefile.Directory.Exists)
                makefile.Directory.Create();
            using (var writer = makefile.CreateText())
            {
                writer.WriteLine(string.Format("all: {0}", makefile.Directory.GetRelativePath(target_file).Replace('\\', '/')));
                writer.WriteLine();

                writer.WriteLine("clean:");
                writer.WriteLine(string.Format("\trm -f {0}",
                                               string.Join(" ",
                                                           new[] { target_file, map_file }
                                                           .Select(file => makefile.Directory.GetRelativePath(file).Replace('\\', '/')))));
                writer.WriteLine(string.Format("\trm -r -f {0}",
                                               string.Join(" ",
                                                           file_infos.Select(file_info => file_info.object_file)
                                                           .Concat(file_infos.Select(file_info => file_info.assembly_file))
                                                           .Concat(file_infos.Select(file_info => file_info.i_file))
                                                           .Select(file => file.Directory)
                                                           .Distinct(new DirectoryInfoComparer())
                                                           .Select(dir => makefile.Directory.GetRelativePath(dir).Replace('\\', '/')))));
                writer.WriteLine();

                writer.WriteLine(string.Format("OBJS = {0}", string.Join(" ", file_infos.Select(file_info => makefile.Directory.GetRelativePath(file_info.object_file).Replace('\\', '/')))));
                writer.WriteLine();
                writer.WriteLine(string.Format("{0}: $(OBJS)", makefile.Directory.GetRelativePath(target_file).Replace('\\', '/')));
                writer.WriteLine(string.Format("\tmkdir -p {0}", makefile.Directory.GetRelativePath(target_file.Directory).Replace('\\', '/')));
                writer.WriteLine(string.Format("\tgcc -o {0} $(OBJS) {1} -Wl,-Map={2} -shared",
                                               makefile.Directory.GetRelativePath(target_file).Replace('\\', '/'),
                                               string.Join(" ", setting.Ldflags.Where(item => new[] { null, platform, config }.Contains(item.On)).Select(item => item.Value)),
                                               makefile.Directory.GetRelativePath(map_file).Replace('\\', '/')));
                writer.WriteLine();
                foreach (var file_info in file_infos)
                {
                    writer.WriteLine(string.Format("{0}: {1}",
                                                   makefile.Directory.GetRelativePath(file_info.object_file).Replace('\\', '/'),
                                                   string.Join("  ", new[] { file_info.source_file }.Concat(file_info.include_files).Concat(new[] { mymakefile }).Select(path => makefile.Directory.GetRelativePath(path).Replace('\\', '/')))));
                    writer.WriteLine(string.Format("\tmkdir -p {0}", makefile.Directory.GetRelativePath(file_info.object_file.Directory).Replace('\\', '/')));
                    writer.WriteLine(string.Format("\tgcc -c -save-temps=obj -Werror {0} -o {1} {2}",
                                                   string.Join(" ", setting.Cflags.Where(item => new[] { null, platform, config }.Contains(item.On)).Select(item => item.Value)),
                                                   makefile.Directory.GetRelativePath(file_info.object_file).Replace('\\', '/'),
                                                   makefile.Directory.GetRelativePath(file_info.source_file).Replace('\\', '/')));
                    writer.WriteLine();
                }
            }
            return (makefile);
        }

        private static void ExecuteMake(DirectoryInfo base_dir, FileInfo makefile, MyMakeFileSetting setting, string platform)
        {
            var tool_chain_path = setting.ToolChains[platform];
            var start_info = new ProcessStartInfo();
            start_info.Arguments = string.Format("-f {0}", makefile);
            start_info.Environment["PATH"] = string.Format("{0};{1}", tool_chain_path, Environment.GetEnvironmentVariable("PATH"));
            start_info.FileName = "make.exe";
            start_info.UseShellExecute = false;
            start_info.WorkingDirectory = makefile.Directory.FullName;
            var p = Process.Start(start_info);
            p.WaitForExit();
        }
    }
}
