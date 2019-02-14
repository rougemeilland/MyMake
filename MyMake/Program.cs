using System;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MyMake
{
    class Program
    {

        static void Main(string[] args)
        {
            var targets = args;
            var mymakefile = new DirectoryInfo(Directory.GetCurrentDirectory()).GetFile("mymakefile.xml");
            var mymake = new FileInfo(Assembly.GetExecutingAssembly().Location);
            if (mymakefile.Exists)
                Open(mymake, mymakefile, targets);
        }

        private static void Open(FileInfo mymake, FileInfo mymakefile, string[] targrts)
        {
            var setting = new MyMakeFileSetting(mymakefile);

            OpenMakefile(mymake, mymakefile, setting, "x86", "Debug", targrts);
            OpenMakefile(mymake, mymakefile, setting, "x86", "Release", targrts);
            OpenMakefile(mymake, mymakefile, setting, "x64", "Debug", targrts);
            OpenMakefile(mymake, mymakefile, setting, "x64", "Release", targrts);

#if DEBUG
            Console.ReadLine();
#endif
        }

        private static void OpenMakefile(FileInfo mymake, FileInfo mymakefile, MyMakeFileSetting setting, string platform, string config, string[] targets)
        {
            var makefile = CreateMakefile(mymake, mymakefile, setting, platform, config);
            ExecuteMake(makefile, setting, platform, targets);
        }

        private static FileInfo CreateMakefile(FileInfo mymake, FileInfo mymakefile, MyMakeFileSetting setting, string platform, string config)
        {
            var base_dir = mymakefile.Directory;
            var target_file = base_dir.Parent.GetDirectory("dist").GetDirectory(config).GetDirectory(platform).GetFile(setting.TargetFileName);
            var module_definition_file = base_dir.GetFile(Path.GetFileNameWithoutExtension(setting.TargetFileName) + ".def");
            if (!module_definition_file.Exists)
                module_definition_file = null;
            var object_dir = base_dir.GetDirectory("build").GetDirectory(string.Format("{0}_{1}", platform, config));
            var map_file = object_dir.GetFile(Path.GetFileNameWithoutExtension(target_file.Name) + ".map");
            var makefile = base_dir.GetDirectory("myproject").GetFile(string.Format("Makefile.{0}_{1}.mk", platform, config));

            //if (makefile.Exists && makefile.LastWriteTimeUtc >= mymakefile.LastWriteTimeUtc && makefile.LastWriteTimeUtc >= mymake.LastWriteTimeUtc)
            //    return (makefile);

            var dependencies = base_dir.EnumerateFiles("*.*", SearchOption.AllDirectories)
                               .Where(file => new[] { ".c", ".rc" }.Contains(file.Extension.ToLower()))
                               .Select(file => new FileDependency(file, setting.IncludeFilePaths.Where(item => new[] { null, platform, config }.Contains(item.On)).Select(item => item.Value)));

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
                                                           .Distinct(new FileInfoComparer())
                                                           .Select(dir => makefile.Directory.GetRelativePath(dir).Replace('\\', '/')))));
                writer.WriteLine();

                writer.WriteLine("test:");
                foreach (var commandline in setting.TestCommandlines)
                    writer.WriteLine(string.Format("\t{0}", commandline.Replace("{config}", config).Replace("{platform}", platform)));
                writer.WriteLine();

                var lib_dirs = setting.LibraryFilePaths.Where(item => new[] { null, platform, config }.Contains(item.On)).Select(item => item.Value);

                writer.WriteLine(string.Format("OBJS = {0}", string.Join(" ", 
                                                                         file_infos
                                                                         .Select(file_info => file_info.object_file)
                                                                         .Concat(setting.AdditionalLibraries.Select(lib => lib_dirs.Select(dir => dir.GetFile(lib)).Where(file=> file.Exists).FirstOrDefault()).Where(file => file != null))
                                                                         .Select(file => makefile.Directory.GetRelativePath(file).Replace('\\', '/'))
                                                                         )));
                writer.WriteLine();
                writer.WriteLine(string.Format("{0}: $(OBJS) {1}",
                                               makefile.Directory.GetRelativePath(target_file).Replace('\\', '/'),
                                               module_definition_file != null ? makefile.Directory.GetRelativePath(module_definition_file).Replace('\\', '/') : ""));
                writer.WriteLine(string.Format("\tmkdir -p {0}", makefile.Directory.GetRelativePath(target_file.Directory).Replace('\\', '/')));
                writer.WriteLine(string.Format("\tgcc -o {0} $(OBJS) {1} {2} -Wl,-Map={3} {4}",
                                               makefile.Directory.GetRelativePath(target_file).Replace('\\', '/'),
                                               module_definition_file != null ? makefile.Directory.GetRelativePath(module_definition_file).Replace('\\', '/') : "",
                                               string.Join(" ", setting.Ldflags.Where(item => new[] { null, platform, config }.Contains(item.On)).Select(item => item.Value)),
                                               makefile.Directory.GetRelativePath(map_file).Replace('\\', '/'),
                                               string.Join(" ", lib_dirs.Select(dir => string.Format("-L{0}", dir.FullName.Replace('\\', '/'))))));
                writer.WriteLine();
                foreach (var file_info in file_infos)
                {
                    writer.WriteLine(string.Format("{0}: {1}",
                                                   makefile.Directory.GetRelativePath(file_info.object_file).Replace('\\', '/'),
                                                   string.Join("  ", new[] { file_info.source_file }.Concat(file_info.include_files).Concat(new[] { mymakefile }).Select(path => makefile.Directory.GetRelativePath(path).Replace('\\', '/')))));
                    writer.WriteLine(string.Format("\tmkdir -p {0}", makefile.Directory.GetRelativePath(file_info.object_file.Directory).Replace('\\', '/')));
                    switch (file_info.source_file.Extension.ToLower())
                    {
                        case ".c":
                            writer.WriteLine(string.Format("\tgcc -c -save-temps=obj -Werror {0} {1} -o {2} {3}",
                                                           string.Join(" ", setting.Cflags.Where(item => new[] { null, platform, config }.Contains(item.On)).Select(item => item.Value)),
                                                           string.Join(" ", setting.IncludeFilePaths.Where(item => new[] { null, platform, config }.Contains(item.On)).Select(item => string.Format("-I{0}", item.Value.FullName.Replace('\\', '/')))),
                                                           makefile.Directory.GetRelativePath(file_info.object_file).Replace('\\', '/'),
                                                           makefile.Directory.GetRelativePath(file_info.source_file).Replace('\\', '/')));
                            break;
                        case ".rc":
                            writer.WriteLine(string.Format("\twindres -c 65001 -o {0} {1}",
                                                           makefile.Directory.GetRelativePath(file_info.object_file).Replace('\\', '/'),
                                                           makefile.Directory.GetRelativePath(file_info.source_file).Replace('\\', '/')));
                            break;
                        default:
                            throw new ApplicationException();

                    }
                    writer.WriteLine();
                }
            }
            return (makefile);
        }

        private static void ExecuteMake(FileInfo makefile, MyMakeFileSetting setting, string platform, string[] targets)
        {
            var tool_chain_path = setting.ToolChains[platform];
            var start_info = new ProcessStartInfo();
            start_info.Arguments = string.Join(" ", new[] { "-f", makefile.Name }.Concat(targets));
            start_info.Environment["PATH"] = string.Format("{0};{1}", tool_chain_path, start_info.Environment["PATH"]);
            start_info.FileName = "make.exe";
            start_info.UseShellExecute = false;
            start_info.WorkingDirectory = makefile.Directory.FullName;
            var p = Process.Start(start_info);
            p.WaitForExit();
        }
    }
}
