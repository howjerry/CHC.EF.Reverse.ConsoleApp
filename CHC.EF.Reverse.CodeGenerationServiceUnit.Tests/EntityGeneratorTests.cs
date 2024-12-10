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
    /// �������O���;����椸�������O�C
    /// </summary>
    /// <remarks>
    /// ���ѥ��������ծרҡA�T�O EntityGenerator �b�U�ر��ҤU���ॿ�T�B�@�C
    /// ���սd��]�A�G
    /// 1. �򥻹������O����
    /// 2. ���p�B�z
    /// 3. ���~�B�z
    /// 4. �S��רҳB�z
    /// </remarks>
    public class EntityGeneratorTests
    {
        private readonly Mock<ILogger> _loggerMock;
        private readonly Settings _defaultSettings;

        /// <summary>
        /// ��l�ƴ������ҡA�]�w�򥻪���������M���ո�ơC
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
        /// ���հ򥻹������O���ͥ\��C
        /// </summary>
        /// <returns>�D�P�B�ާ@���u�@</returns>
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
                    s.Contains("�w���͹������O"))),
                Times.Once);
        }

        /// <summary>
        /// ���ճB�z�@��h���p�����p�C
        /// </summary>
        /// <returns>�D�P�B�ާ@���u�@</returns>
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
                    s.Contains("�w�������p"))),
                Times.Once);
        }

        /// <summary>
        /// ���ժŸ�ƪ�B�z�C
        /// </summary>
        /// <returns>�D�P�B�ާ@���u�@</returns>
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
        /// ���յL�ĳ]�w�B�z�C
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