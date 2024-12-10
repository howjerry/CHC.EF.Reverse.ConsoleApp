using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHC.EF.Reverse.ConsoleApp
{
    public class Options
    {
        [Option('c', "connection", Required = false, HelpText = "Database connection string")]
        public string ConnectionString { get; set; }

        [Option('p', "provider", Required = false,
            HelpText = "Database provider (SqlServer/MySql)")]
        public string Provider { get; set; }

        [Option('n', "namespace", Required = false,
            HelpText = "Namespace for generated code")]
        public string Namespace { get; set; }

        [Option('o', "output", Required = false,
            HelpText = "Output directory")]
        public string OutputDirectory { get; set; } 

        [Option("pluralize", Required = false,
            HelpText = "Pluralize collection names")]
        public bool? IsPluralize { get; set; }

        [Option("data-annotations", Required = false,
            HelpText = "Use data annotations")]
        public bool? UseDataAnnotations { get; set; }

        [Option("config", Required = false, Default = "appsettings.json",
            HelpText = "Path to custom configuration file.")]
        public string ConfigFile { get; set; }
    }
}
