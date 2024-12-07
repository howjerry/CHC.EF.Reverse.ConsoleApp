using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CHC.EF.Reverse.ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 設定 Configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            // 註冊 DI 容器
            var services = new ServiceCollection();
            ConfigureServices(services, configuration);

            var serviceProvider = services.BuildServiceProvider();

            // 執行程式碼生成
            using (var scope = serviceProvider.CreateScope())
            {
                var codeGenService = scope.ServiceProvider.GetRequiredService<CodeGenerationService>();
                codeGenService.Run();
            }
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // 註冊設定
            services.Configure<Settings>(configuration.GetSection("CodeGenerator"));

            // 註冊 Logger
            services.AddSingleton<Logger>();

            // 註冊 SchemaReader 工廠
            services.AddSingleton<DatabaseSchemaReaderFactory>();

            // 註冊代碼生成服務
            services.AddTransient<CodeGenerationService>();
        }
    }
}
