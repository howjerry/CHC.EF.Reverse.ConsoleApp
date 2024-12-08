using Xunit;
using Moq;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;

namespace CHC.EF.Reverse.ConsoleApp.Tests
{
    public class CodeGenerationServiceTests
    {
        private readonly Mock<IOptions<Settings>> _mockSettings;
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IDatabaseSchemaReaderFactory> _mockSchemaReaderFactory;
        private readonly Mock<IDatabaseSchemaReader> _mockSchemaReader;
        private readonly string _testOutputPath;

        public CodeGenerationServiceTests()
        {
            // 設置測試環境
            _testOutputPath = Path.Combine(Path.GetTempPath(), "CodeGenTests");

            // 初始化 Mocks
            _mockSettings = new Mock<IOptions<Settings>>();
            _mockLogger = new Mock<ILogger>();
            _mockSchemaReader = new Mock<IDatabaseSchemaReader>();
            _mockSchemaReaderFactory = new Mock<IDatabaseSchemaReaderFactory>();

            // 設定 Settings
            var settings = new Settings
            {
                OutputDirectory = _testOutputPath,
                Namespace = "TestNamespace",
                DbContextName = "TestDbContext",
                UseDataAnnotations = true,
                IncludeComments = true,
                ElementsToGenerate = new List<string> { "POCO", "Configuration", "DbContext" }
            };

            _mockSettings.Setup(x => x.Value).Returns(settings);

            // 設置 SchemaReaderFactory
            _mockSchemaReaderFactory
                .Setup(x => x.Create())
                .Returns(_mockSchemaReader.Object);
        }

        [Fact]
        public async Task Run_WithDatabaseConnectionError_HandlesException()
        {
            // Arrange
            var expectedMessage = "無法連接到資料庫";
            var exception = new InvalidOperationException(expectedMessage);

            _mockSchemaReader
                .Setup(x => x.ReadTables())
                .Throws(exception);

            var service = new CodeGenerationService(
                _mockSettings.Object,
                _mockLogger.Object,
                _mockSchemaReaderFactory.Object);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.Run()
            );

            Assert.Equal(expectedMessage, thrownException.Message);

            _mockLogger.Verify(
                x => x.Error(
                    It.Is<string>(s => s.Contains("產生程式碼時發生錯誤")),
                    It.Is<Exception>(e => e.Message == expectedMessage)
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task Run_WithEmptyTables_LogsAppropriateMessage()
        {
            // Arrange
            _mockSchemaReader
                .Setup(x => x.ReadTables())
                .Returns(new List<TableDefinition>());

            var service = new CodeGenerationService(
                _mockSettings.Object,
                _mockLogger.Object,
                _mockSchemaReaderFactory.Object);

            // Act
            await service.Run();

            // Assert
            _mockLogger.Verify(
                x => x.Info(It.Is<string>(s => s.Contains("讀取到 0 個資料表"))),
                Times.Once
            );
        }

        [Fact]
        public async Task Run_WithValidSettings_GeneratesAllFiles()
        {
            // Arrange
            var sampleTables = new List<TableDefinition>
            {
                new TableDefinition
                {
                    TableName = "Customer",
                    SchemaName = "dbo",
                    Columns = new List<ColumnDefinition>
                    {
                        new ColumnDefinition
                        {
                            ColumnName = "Id",
                            DataType = "int",
                            IsPrimaryKey = true,
                            IsIdentity = true
                        },
                        new ColumnDefinition
                        {
                            ColumnName = "Name",
                            DataType = "string",
                            MaxLength = 100,
                            IsNullable = false
                        }
                    }
                }
            };

            _mockSchemaReader
                .Setup(x => x.ReadTables())
                .Returns(sampleTables);

            var service = new CodeGenerationService(
                _mockSettings.Object,
                _mockLogger.Object,
                _mockSchemaReaderFactory.Object);

            // Act
            await service.Run();

            // Assert
            _mockLogger.Verify(
                x => x.Info(It.Is<string>(s => s.Contains("讀取到 1 個資料表"))),
                Times.Once
            );
            Assert.True(Directory.Exists(Path.Combine(_testOutputPath, "Entities")));
            Assert.True(Directory.Exists(Path.Combine(_testOutputPath, "Configurations")));
            Assert.True(File.Exists(Path.Combine(_testOutputPath, "TestDbContext.cs")));
        }

        public void Dispose()
        {
            if (Directory.Exists(_testOutputPath))
            {
                Directory.Delete(_testOutputPath, true);
            }
        }
    }
}