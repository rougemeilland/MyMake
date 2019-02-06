using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MyMake
{
    class MyMakeFileSetting
    {
        public MyMakeFileSetting(FileInfo mymakefile)
        {
            var xmldoc = XDocument.Load(mymakefile.FullName);
            var node_setting = xmldoc.Element("setting");

            var node_tool_chains = node_setting.Element("toolchains");
            ToolChains = node_tool_chains.Elements("toolchain")
                         .Select(node => new { value = node.Value, platform = node.Attribute("platform") })
                         .Select(item => new { item.value, platform = item.platform != null ? item.platform.Value : null })
                         .ToDictionary(item => item.platform, item => new DirectoryInfo(item.value));

            var node_targe_tfile_name = node_setting.Element("targetfilename");
            TargetFileName = node_targe_tfile_name.Value;

            var node_source_file_filters = node_setting.Element("sourcefilefilters");
            SourceFileFilters = node_source_file_filters.Elements("sourcefilefilter").Select(node => new Regex(node.Value, RegexOptions.Compiled)).ToArray();

            var node_cflags = node_setting.Element("cflags");
            Cflags = node_cflags.Elements("cflag")
                     .Select(node => new { value = node.Value, on = node.Attribute("on") })
                     .Select(item => new { item.value, on = item.on != null ? item.on.Value : null })
                     .Select(item => new FlagSetting(item.value, item.on))
                     .ToArray();

            var node_ldflags = node_setting.Element("ldflags");
            Ldflags = node_ldflags.Elements("ldflag")
                      .Select(node => new { value = node.Value, on = node.Attribute("on") })
                      .Select(item => new { item.value, on = item.on != null ? item.on.Value : null })
                      .Select(item => new FlagSetting(item.value, item.on))
                      .ToArray();

            var node_test = node_setting.Element("test");
            if (node_test != null)
            {
                var node_test_command_lines = node_test.Element("commandlines");
                TestCommandlines = node_test_command_lines.Elements("commandline")
                                   .Select(node => node.Value)
                                   .ToArray();
            }
            else
                TestCommandlines = new string[0];

            var node_include_paths = node_setting.Element("includepaths");
            IncludeFilePaths = node_include_paths.Elements("includepath")
                               .Select(node => new { value = node.Value, on = node.Attribute("on") })
                               .Select(item => new { item.value, on = item.on != null ? item.on.Value : null })
                               .Select(item => new DirectoryPathSetting(new DirectoryInfo(item.value), item.on))
                               .ToArray();

            var node_library_paths = node_setting.Element("librarypaths");
            LibraryFilePaths = node_library_paths.Elements("librarypath")
                               .Select(node => new { value = node.Value, on = node.Attribute("on") })
                               .Select(item => new { item.value, on = item.on != null ? item.on.Value : null })
                               .Select(item => new DirectoryPathSetting(new DirectoryInfo(item.value), item.on))
                               .ToArray();

            var node_additionallibraries = node_setting.Element("additionallibraries");
            AdditionalLibraries = node_additionallibraries.Elements("additionallibrary")
                                 .Select(node => node.Value)
                                 .ToArray();
        }

        public IDictionary<string, DirectoryInfo> ToolChains { get; private set; }
        public string TargetFileName { get; private set; }
        public IEnumerable<Regex> SourceFileFilters { get; private set; }
        public IEnumerable<FlagSetting> Cflags { get; private set; }
        public IEnumerable<FlagSetting> Ldflags { get; private set; }
        public IEnumerable<string> TestCommandlines { get; private set; }
        public IEnumerable<DirectoryPathSetting> IncludeFilePaths { get; private set; }
        public IEnumerable<DirectoryPathSetting> LibraryFilePaths { get; private set; }
        public IEnumerable<string> AdditionalLibraries { get; private set; }
    }
}
