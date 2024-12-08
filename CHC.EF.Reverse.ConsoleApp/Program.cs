using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace CHC.EF.Reverse.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // 初始化配置和依賴注入
                var serviceProvider = ConfigureServices();

                // 執行代碼生成
                using (var scope = serviceProvider.CreateScope())
                {
                    var codeGenService = scope.ServiceProvider.GetRequiredService<CodeGenerationService>();
                    await codeGenService.Run();
                }

                Console.WriteLine("Code generation completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static ServiceProvider ConfigureServices()
        {
            // 加載配置文件
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // 註冊 DI 容器
            var services = new ServiceCollection();

            // 設定注入
            services.Configure<Settings>(configuration.GetSection("CodeGenerator"));

            // 註冊核心服務
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<IDatabaseSchemaReaderFactory, DatabaseSchemaReaderFactory>();
            services.AddTransient<CodeGenerationService>();

            return services.BuildServiceProvider();
        }
    }
}
