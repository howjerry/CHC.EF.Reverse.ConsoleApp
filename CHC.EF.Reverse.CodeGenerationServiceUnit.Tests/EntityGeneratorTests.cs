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

            // �T�O��X�ؿ��s�b
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
                Comment = "���~��ƪ�",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition
                    {
                        ColumnName = "ProductId",
                        DataType = "int",
                        IsPrimaryKey = true,
                        IsIdentity = true,
                        Comment = "���~�s��"
                    },
                    new ColumnDefinition
                    {
                        ColumnName = "ProductName",
                        DataType = "string",
                        MaxLength = 200,
                        IsNullable = false,
                        Comment = "���~�W��"
                    }
                }
            };

            var generator = new EntityGenerator(_settings, _mockLogger.Object);

            // Act
            await generator.GenerateAsync(new List<TableDefinition> { table });

            // Assert
            // �ˬd�]�w���ɮ�
            var configPath = Path.Combine(_testOutputPath, "Configurations", "ProductConfiguration.cs");
            Assert.True(File.Exists(configPath), $"�]�w���ɮפ��s�b: {configPath}");
            var configContent = await File.ReadAllTextAsync(configPath);

            // ��X��ڤ��e�H�K����
            _mockLogger.Object.Info($"��ڥͦ����]�w�ɤ��e: \n{configContent}");

            // ���Ұ� using �y�y
            Assert.Contains("using System.Data.Entity.ModelConfiguration;", configContent);

            // ���ҩR�W�Ŷ��M���O�w�q
            Assert.Contains("namespace TestNamespace.Configurations", configContent);
            Assert.Contains("public class ProductConfiguration : EntityTypeConfiguration<TestNamespace.Entities.Product>", configContent);

            // ���Ҫ��M�g
            Assert.Contains("ToTable(\"Product\", \"dbo\")", configContent);

            // �����ݩʰt�m
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

        // �M�z�����ɮ�
        public void Dispose()
        {
            if (Directory.Exists(_testOutputPath))
            {
                Directory.Delete(_testOutputPath, true);
            }
        }


    }
}