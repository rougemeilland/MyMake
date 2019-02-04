using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MyMake
{
    class FlagSetting
    {
        public FlagSetting(string value, string on)
        {
            Value = value;
            On = on;
        }
        public string Value { get; private set; }
        public string On { get; private set; }
    }

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
        }

        public IDictionary<string, DirectoryInfo> ToolChains { get; private set; }
        public string TargetFileName { get; private set; }
        public IEnumerable<Regex> SourceFileFilters { get; private set; }
        public IEnumerable<FlagSetting> Cflags { get; private set; }
        public IEnumerable<FlagSetting> Ldflags { get; private set; }
    }
}
