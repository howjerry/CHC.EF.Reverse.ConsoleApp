using Xunit;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Moq;
using System.Text;

namespace CHC.EF.Reverse.ConsoleApp.Tests
{
    public class EntityGeneratorTests
    {
        private readonly Settings _settings;
        private readonly Mock<ILogger> _mockLogger;
        private readonly string _testOutputPath;

        public EntityGeneratorTests()
        {
            _testOutputPath = Path.Combine(Path.GetTempPath(), "EntityGenTests");

            _settings = new Settings
            {
                OutputDirectory = _testOutputPath,
                Namespace = "TestNamespace",
                UseDataAnnotations = true,
                IncludeComments = true
            };

            _mockLogger = new Mock<ILogger>();

            // 確保輸出目錄存在
            Directory.CreateDirectory(_testOutputPath);
            Directory.CreateDirectory(Path.Combine(_testOutputPath, "Entities"));
            Directory.CreateDirectory(Path.Combine(_testOutputPath, "Configurations"));
        }

        [Fact]
        public async Task GenerateAsync_WithValidTable_GeneratesEntityAndConfiguration()
        {
            // Arrange
            var table = new TableDefinition
            {
                TableName = "Product",
                SchemaName = "dbo",
                Comment = "產品資料表",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition
                    {
                        ColumnName = "ProductId",
                        DataType = "int",
                        IsPrimaryKey = true,
                        IsIdentity = true,
                        Comment = "產品編號"
                    },
                    new ColumnDefinition
                    {
                        ColumnName = "ProductName",
                        DataType = "string",
                        MaxLength = 200,
                        IsNullable = false,
                        Comment = "產品名稱"
                    }
                }
            };

            var generator = new EntityGenerator(_settings, _mockLogger.Object);

            // Act
            await generator.GenerateAsync(new List<TableDefinition> { table });

            // Assert
            // 檢查設定類檔案
            var configPath = Path.Combine(_testOutputPath, "Configurations", "ProductConfiguration.cs");
            Assert.True(File.Exists(configPath), $"設定類檔案不存在: {configPath}");
            var configContent = await File.ReadAllTextAsync(configPath);

            // 輸出實際內容以便偵錯
            _mockLogger.Object.Info($"實際生成的設定檔內容: \n{configContent}");

            // 驗證基本 using 語句
            Assert.Contains("using System.Data.Entity.ModelConfiguration;", configContent);

            // 驗證命名空間和類別定義
            Assert.Contains("namespace TestNamespace.Configurations", configContent);
            Assert.Contains("public class ProductConfiguration : EntityTypeConfiguration<TestNamespace.Entities.Product>", configContent);

            // 驗證表格映射
            Assert.Contains("ToTable(\"Product\", \"dbo\")", configContent);

            // 驗證屬性配置
            Assert.Contains("Property(x => x.ProductId)", configContent);
            Assert.Contains("HasKey(x => x.ProductId)", configContent);
            Assert.Contains("Property(x => x.ProductName)", configContent);
            Assert.Contains("HasMaxLength(200)", configContent);
            Assert.Contains("IsRequired()", configContent);
        }
        [Fact]
        public async Task GenerateAsync_WithForeignKey_GeneratesNavigationProperties()
        {
            // Arrange
            var table = new TableDefinition
            {
                TableName = "Order",
                SchemaName = "dbo",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition
                    {
                        ColumnName = "OrderId",
                        DataType = "int",
                        IsPrimaryKey = true
                    },
                    new ColumnDefinition
                    {
                        ColumnName = "CustomerId",
                        DataType = "int",
                        IsNullable = false
                    }
                },
                ForeignKeys = new List<ForeignKeyDefinition>
                {
                    new ForeignKeyDefinition
                    {
                        ForeignKeyColumn = "CustomerId",
                        PrimaryTable = "Customer",
                        PrimaryKeyColumn = "CustomerId",
                        DeleteRule = "CASCADE"
                    }
                }
            };

            var generator = new EntityGenerator(_settings, _mockLogger.Object);

            // Act
            await generator.GenerateAsync(new List<TableDefinition> { table });

            // Assert
            var entityPath = Path.Combine(_testOutputPath, "Entities", "Order.cs");
            Assert.True(File.Exists(entityPath));
            var entityContent = await File.ReadAllTextAsync(entityPath);
            Assert.Contains("public virtual Customer Customer { get; set; }", entityContent);
        }

        // 清理測試檔案
        public void Dispose()
        {
            if (Directory.Exists(_testOutputPath))
            {
                Directory.Delete(_testOutputPath, true);
            }
        }


    }
}