using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace CHC.EF.Reverse.ConsoleApp
{
    public class Settings
    {
        public string ConnectionString { get; set; }
        public string ProviderName { get; set; }
        public string Namespace { get; set; } = "MyApp.Data";
        public string DbContextName { get; set; } = "MyDbContext";
        public bool UseDataAnnotations { get; set; } = true;
        public bool IncludeComments { get; set; } = true;
        public bool IsPluralize { get; set; } = true;
        public bool UsePascalCase { get; set; } = true;
        public bool GenerateSeparateFiles { get; set; } = true;
        public string OutputDirectory { get; set; } = "C:\\CodeGenOutput";
    }
}