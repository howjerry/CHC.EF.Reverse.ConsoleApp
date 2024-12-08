using Xunit;
using Microsoft.Extensions.Options;
using System;
using Moq;

namespace CHC.EF.Reverse.ConsoleApp.Tests
{
    public class DatabaseSchemaReaderFactoryTests
    {
        private readonly Mock<IOptions<Settings>> _mockSettings;

        public DatabaseSchemaReaderFactoryTests()
        {
            _mockSettings = new Mock<IOptions<Settings>>();
        }

        [Fact]
        public void Create_WithMySqlProvider_ReturnsMySqlReader()
        {
            // Arrange
            _mockSettings.Setup(x => x.Value).Returns(new Settings
            {
                ProviderName = "MySql.Data.MySqlClient",
                ConnectionString = "Server=localhost;Database=test;Uid=root;Pwd=password;"
            });

            var factory = new DatabaseSchemaReaderFactory(_mockSettings.Object);

            // Act
            var reader = factory.Create();

            // Assert
            Assert.IsType<MySqlSchemaReader>(reader);
        }

        [Fact]
        public void Create_WithSqlServerProvider_ReturnsSqlServerReader()
        {
            // Arrange
            _mockSettings.Setup(x => x.Value).Returns(new Settings
            {
                ProviderName = "Microsoft.Data.SqlClient",
                ConnectionString = "Server=localhost;Database=test;Trusted_Connection=True;"
            });

            var factory = new DatabaseSchemaReaderFactory(_mockSettings.Object);

            // Act
            var reader = factory.Create();

            // Assert
            Assert.IsType<SqlServerSchemaReader>(reader);
        }

        [Fact]
        public void Create_WithUnsupportedProvider_ThrowsNotSupportedException()
        {
            // Arrange
            _mockSettings.Setup(x => x.Value).Returns(new Settings
            {
                ProviderName = "UnsupportedProvider",
                ConnectionString = "connection string"
            });

            var factory = new DatabaseSchemaReaderFactory(_mockSettings.Object);

            // Act & Assert
            var exception = Assert.Throws<NotSupportedException>(() => factory.Create());
            Assert.Contains("¤£¤ä´©ªº Provider", exception.Message);
        }
    }
}