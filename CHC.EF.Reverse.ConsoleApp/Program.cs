using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CHC.EF.Reverse.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(async options =>
                {
                    try
                    {
                        if (options.Init)
                        {
                            await InitializeConfigFiles(options.OutputDirectory);
                            return;
                        }

                        var settings = await GetSettingsAsync(options);
                        var services = ConfigureServices(settings);

                        using (var scope = services.CreateScope())
                        {
                            var codeGenService = scope.ServiceProvider.GetRequiredService<CodeGenerationService>();
                            await codeGenService.Run();
                            Console.WriteLine("Code generation completed successfully.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        Environment.Exit(1);
                    }
                });
        }

        private static async Task<Settings> GetSettingsAsync(Options options)
        {
            Settings settings = new Settings();

            // 1. 嘗試從 appsettings.json 讀取基本設定
            if (File.Exists(options.SettingsFile))
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(options.SettingsFile)
                    .Build();

                var section = configuration.GetSection("CodeGenerator");
                if (section.Exists())
                {
                    settings = section.Get<Settings>();
                }
                else
                {
                    Console.WriteLine("Warning: 'CodeGenerator' section not found in appsettings.json");
                }
            }
            else
            {
                Console.WriteLine($"Warning: {options.SettingsFile} not found");
            }

            // 2. 如果指定了自定義配置文件，覆蓋設定
            if (!string.IsNullOrEmpty(options.ConfigFile) && File.Exists(options.ConfigFile))
            {
                var jsonConfig = await File.ReadAllTextAsync(options.ConfigFile);
                var customSettings = System.Text.Json.JsonSerializer.Deserialize<Settings>(jsonConfig);

                // 合併設定
                settings = MergeSettings(settings, customSettings);
            }

            // 3. 命令列參數優先，覆蓋之前的設定
            settings = MergeSettings(settings, new Settings
            {
                ConnectionString = options.ConnectionString,
                ProviderName = options.Provider,
                Namespace = options.Namespace,
                OutputDirectory = options.OutputDirectory,
                IsPluralize = options.IsPluralize ?? false,
                UseDataAnnotations = options.UseDataAnnotations ?? false
            });

            ValidateSettings(settings);
            return settings;
        }

        private static Settings MergeSettings(Settings target, Settings source)
        {
            // 只覆蓋非空值
            if (!string.IsNullOrEmpty(source.ConnectionString))
                target.ConnectionString = source.ConnectionString;
            if (!string.IsNullOrEmpty(source.ProviderName))
                target.ProviderName = source.ProviderName;
            if (!string.IsNullOrEmpty(source.Namespace))
                target.Namespace = source.Namespace;
            if (!string.IsNullOrEmpty(source.OutputDirectory))
                target.OutputDirectory = source.OutputDirectory;
            if (!string.IsNullOrEmpty(source.DbContextName))
                target.DbContextName = source.DbContextName;

            return target;
        }

        private static void ValidateSettings(Settings settings)
        {
            if (string.IsNullOrEmpty(settings.ConnectionString))
                throw new ArgumentException("Connection string is required. Please specify it in configuration file or command line.");

            if (string.IsNullOrEmpty(settings.ProviderName))
                settings.ProviderName = "Microsoft.Data.SqlClient";

            if (string.IsNullOrEmpty(settings.Namespace))
                settings.Namespace = "GeneratedApp.Data";

            if (string.IsNullOrEmpty(settings.OutputDirectory))
                settings.OutputDirectory = "./Generated";
        }

        private static async Task InitializeConfigFiles(string outputPath)
        {
            // 生成預設配置
            var defaultConfig = new Settings
            {
                ConnectionString = "",
                ProviderName = "Microsoft.Data.SqlClient",
                Namespace = "GeneratedApp.Data",
                DbContextName = "AppDbContext",
                UseDataAnnotations = true,
                IncludeComments = true,
                IsPluralize = true,
                OutputDirectory = "./Generated"
            };

            // 生成 efrev.json
            var jsonConfig = System.Text.Json.JsonSerializer.Serialize(defaultConfig, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            var configPath = Path.Combine(outputPath, "efrev.json");
            await File.WriteAllTextAsync(configPath, jsonConfig);
            Console.WriteLine($"Custom configuration file created at: {configPath}");

            // 生成 appsettings.json
            var appsettingsContent = @"{
  ""CodeGenerator"": {
    ""ConnectionString"": """",
    ""ProviderName"": ""Microsoft.Data.SqlClient"",
    ""Namespace"": ""GeneratedApp.Data"",
    ""DbContextName"": ""AppDbContext"",
    ""UseDataAnnotations"": true,
    ""IncludeComments"": true,
    ""IsPluralize"": true,
    ""OutputDirectory"": ""./Generated""
  }
}";

            var appsettingsPath = Path.Combine(outputPath, "appsettings.json");
            await File.WriteAllTextAsync(appsettingsPath, appsettingsContent);
            Console.WriteLine($"appsettings.json file created at: {appsettingsPath}");
        }

        private static ServiceProvider ConfigureServices(Settings settings)
        {
            var services = new ServiceCollection();

            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<IDatabaseSchemaReaderFactory, DatabaseSchemaReaderFactory>();
            services.AddTransient<CodeGenerationService>();

            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(settings));

            return services.BuildServiceProvider();
        }
    }
}