using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using CHC.EF.Reverse.ConsoleApp.Tests.TestData;
using CHC.EF.Reverse.ConsoleApp.Core.Models;
using CHC.EF.Reverse.ConsoleApp.Exceptions;
using CHC.EF.Reverse.ConsoleApp.Infrastructure.Generators;
using CHC.EF.Reverse.ConsoleApp.Core.Interfaces;

namespace CHC.EF.Reverse.ConsoleApp.Tests.EntityGeneratorTests
{
    /// <summary>
    /// 實體類別產生器的單元測試類別。
    /// </summary>
    /// <remarks>
    /// 提供全面的測試案例，確保 EntityGenerator 在各種情境下都能正確運作。
    /// 測試範圍包括：
    /// 1. 基本實體類別產生
    /// 2. 關聯處理
    /// 3. 錯誤處理
    /// 4. 特殊案例處理
    /// </remarks>
    public class EntityGeneratorTests
    {
        private readonly Mock<ILogger> _loggerMock;
        private readonly Settings _defaultSettings;

        /// <summary>
        /// 初始化測試環境，設定基本的模擬物件和測試資料。
        /// </summary>
        public EntityGeneratorTests()
        {
            _loggerMock = new Mock<ILogger>();
            _defaultSettings = new Settings
            {
                Namespace = "TestNamespace",
                OutputDirectory = "TestOutput",
                UseDataAnnotations = true,
                IncludeComments = true,
                IsPluralize = true
            };
        }

        /// <summary>
        /// 測試基本實體類別產生功能。
        /// </summary>
        /// <returns>非同步操作的工作</returns>
        [Fact]
        public async Task GenerateAsync_WithValidTable_ShouldGenerateEntityClass()
        {
            // Arrange
            var table = new TableDefinitionBuilder()
                .WithName("Customer")
                .WithColumn(c => c
                    .WithName("Id")
                    .AsPrimaryKey()
                    .AsType("int")
                    .AsIdentity())
                .WithColumn(c => c
                    .WithName("Name")
                    .AsType("nvarchar")
                    .WithMaxLength(100)
                    .AsRequired())
                .Build();

            var generator = new EntityGenerator(
                _defaultSettings,
                _loggerMock.Object,
                new List<TableDefinition> { table });

            // Act
            await generator.GenerateAsync(new List<TableDefinition> { table });

            // Assert
            _loggerMock.Verify(
                x => x.Info(It.Is<string>(s =>
                    s.Contains("已產生實體類別"))),
                Times.Once);
        }

        /// <summary>
        /// 測試處理一對多關聯的情況。
        /// </summary>
        /// <returns>非同步操作的工作</returns>
        [Fact]
        public async Task GenerateAsync_WithOneToManyRelationship_ShouldGenerateNavigationProperties()
        {
            // Arrange
            var orderTable = new TableDefinitionBuilder()
                .WithName("Order")
                .WithColumn(c => c
                    .WithName("Id")
                    .AsPrimaryKey()
                    .AsType("int"))
                .WithColumn(c => c
                    .WithName("CustomerId")
                    .AsType("int")
                    .AsRequired())
                .WithForeignKey(fk => fk
                    .WithName("FK_Order_Customer")
                    .WithColumn("CustomerId")
                    .ReferencingTable("Customer")
                    .ReferencingColumn("Id"))
                .Build();

            var generator = new EntityGenerator(
                _defaultSettings,
                _loggerMock.Object,
                new List<TableDefinition> { orderTable });

            // Act
            await generator.GenerateAsync(new List<TableDefinition> { orderTable });

            // Assert
            _loggerMock.Verify(
                x => x.Info(It.Is<string>(s =>
                    s.Contains("已產生關聯"))),
                Times.Once);
        }

        /// <summary>
        /// 測試空資料表處理。
        /// </summary>
        /// <returns>非同步操作的工作</returns>
        [Fact]
        public async Task GenerateAsync_WithEmptyTable_ShouldHandleGracefully()
        {
            // Arrange
            var emptyTable = new TableDefinitionBuilder()
                .WithName("EmptyTable")
                .Build();

            var generator = new EntityGenerator(
                _defaultSettings,
                _loggerMock.Object,
                new List<TableDefinition> { emptyTable });

            // Act & Assert
            await Assert.ThrowsAsync<CodeGenerationException>(
                () => generator.GenerateAsync(new List<TableDefinition> { emptyTable }));
        }

        /// <summary>
        /// 測試無效設定處理。
        /// </summary>
        [Fact]
        public void Constructor_WithInvalidSettings_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidSettings = new Settings();
            var tables = new List<TableDefinition>();

            // Act & Assert
            Assert.Throws<ArgumentException>(
                () => new EntityGenerator(invalidSettings, _loggerMock.Object, tables));
        }
    }
}