namespace CHC.EF.Reverse.CodeGenerationServiceUnit.Tests;

using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using FluentAssertions;
using CHC.EF.Reverse.ConsoleApp.Core.Interfaces;
using CHC.EF.Reverse.ConsoleApp.Core.Models;
using CHC.EF.Reverse.ConsoleApp.Exceptions;

/// <summary>
/// 提供 RelationshipAnalyzer 類別的單元測試。
/// </summary>
/// <remarks>
/// 測試涵蓋：
/// 1. 基本關聯類型判斷
/// 2. 邊界條件處理
/// 3. 錯誤條件檢查
/// 4. 複合主鍵情況
/// </remarks>
public class RelationshipAnalyzerTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly RelationshipAnalyzer _analyzer;

    /// <summary>
    /// 初始化測試類別的新實例。
    /// </summary>
    public RelationshipAnalyzerTests()
    {
        _loggerMock = new Mock<ILogger>();
        _analyzer = new RelationshipAnalyzer(_loggerMock.Object);
    }

    /// <summary>
    /// 測試建構函式參數驗證。
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new RelationshipAnalyzer(null);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .And.ParamName.Should().Be("logger");
    }

    /// <summary>
    /// 測試分析方法的參數驗證。
    /// </summary>
    /// <param name="sourceTable">來源表格</param>
    /// <param name="targetTable">目標表格</param>
    [Theory]
    [MemberData(nameof(GetInvalidTableParameters))]
    public void AnalyzeRelationship_WithInvalidParameters_ThrowsArgumentException(
        TableDefinition sourceTable,
        TableDefinition targetTable)
    {
        // Act
        Action act = () => _analyzer.AnalyzeRelationship(sourceTable, targetTable);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// 測試一對一關聯的識別。
    /// </summary>
    [Fact]
    public void AnalyzeRelationship_WithOneToOneRelation_ReturnsOneToOneType()
    {
        // Arrange
        var sourceTable = CreateTableWithOneToOneRelation("Order", "CustomerId");
        var targetTable = CreateCustomerTable();

        // Act
        var result = _analyzer.AnalyzeRelationship(sourceTable, targetTable);

        // Assert
        result.Type.Should().Be(RelationType.OneToOne);
        result.SourceTable.Should().Be("Order");
        result.TargetTable.Should().Be("Customer");
        result.ForeignKeyColumns.Should().HaveCount(1);

        _loggerMock.Verify(
            x => x.Info(It.IsAny<string>()),
            Times.Once);
    }

    /// <summary>
    /// 測試一對多關聯的識別。
    /// </summary>
    [Fact]
    public void AnalyzeRelationship_WithOneToManyRelation_ReturnsOneToManyType()
    {
        // Arrange
        var sourceTable = CreateTableWithOneToManyRelation("Order", "CustomerId");
        var targetTable = CreateCustomerTable();

        // Act
        var result = _analyzer.AnalyzeRelationship(sourceTable, targetTable);

        // Assert
        result.Type.Should().Be(RelationType.OneToMany);
        result.SourceTable.Should().Be("Customer");
        result.TargetTable.Should().Be("Order");
    }

    /// <summary>
    /// 測試多對多關聯的識別。
    /// </summary>
    [Fact]
    public void AnalyzeRelationship_WithManyToManyRelation_ReturnsManyToManyType()
    {
        // Arrange
        var junctionTable = CreateJunctionTable();
        var targetTable = CreateCustomerTable();

        // Act
        var result = _analyzer.AnalyzeRelationship(junctionTable, targetTable);

        // Assert
        result.Type.Should().Be(RelationType.ManyToMany);
        result.JunctionTableInfo.Should().NotBeNull();
        result.JunctionTableInfo.TableName.Should().Be("OrderCustomer");
    }

    /// <summary>
    /// 測試無關聯情況的處理。
    /// </summary>
    [Fact]
    public void AnalyzeRelationship_WithNoRelation_ReturnsUnknownType()
    {
        // Arrange
        var sourceTable = CreateTableWithNoRelation("Product");
        var targetTable = CreateCustomerTable();

        // Act
        var result = _analyzer.AnalyzeRelationship(sourceTable, targetTable);

        // Assert
        result.Type.Should().Be(RelationType.Unknown);
    }

    /// <summary>
    /// 測試異常情況的處理。
    /// </summary>
    [Fact]
    public void AnalyzeRelationship_WhenExceptionOccurs_ThrowsRelationshipAnalysisException()
    {
        // Arrange
        var sourceTable = CreateTableThatThrowsException();
        var targetTable = CreateCustomerTable();

        // Act
        Action act = () => _analyzer.AnalyzeRelationship(sourceTable, targetTable);

        // Assert
        act.Should().Throw<RelationshipAnalysisException>()
           .WithMessage("分析關聯時發生錯誤*");
    }

    #region Helper Methods

    /// <summary>
    /// 提供無效的表格參數組合。
    /// </summary>
    public static IEnumerable<object[]> GetInvalidTableParameters()
    {
        yield return new object[] { null, new TableDefinition { TableName = "Test" } };
        yield return new object[] { new TableDefinition { TableName = "Test" }, null };
        yield return new object[] {
            new TableDefinition { TableName = "" },
            new TableDefinition { TableName = "Test" }
        };
    }

    /// <summary>
    /// 建立具有一對一關聯的測試表格。
    /// </summary>
    private TableDefinition CreateTableWithOneToOneRelation(string tableName, string foreignKeyColumn)
    {
        return new TableDefinition
        {
            TableName = tableName,
            Columns = new List<ColumnDefinition>
            {
                new ColumnDefinition
                {
                    ColumnName = "Id",
                    IsPrimaryKey = true
                },
                new ColumnDefinition
                {
                    ColumnName = foreignKeyColumn,
                    IsPrimaryKey = true
                }
            },
            ForeignKeys = new List<ForeignKeyDefinition>
            {
                new ForeignKeyDefinition
                {
                    ForeignKeyColumn = foreignKeyColumn,
                    PrimaryTable = "Customer",
                    PrimaryKeyColumn = "Id",
                    IsEnabled = true
                }
            }
        };
    }

    /// <summary>
    /// 建立具有一對多關聯的測試表格。
    /// </summary>
    private TableDefinition CreateTableWithOneToManyRelation(string tableName, string foreignKeyColumn)
    {
        return new TableDefinition
        {
            TableName = tableName,
            Columns = new List<ColumnDefinition>
            {
                new ColumnDefinition
                {
                    ColumnName = "Id",
                    IsPrimaryKey = true
                },
                new ColumnDefinition
                {
                    ColumnName = foreignKeyColumn
                }
            },
            ForeignKeys = new List<ForeignKeyDefinition>
            {
                new ForeignKeyDefinition
                {
                    ForeignKeyColumn = foreignKeyColumn,
                    PrimaryTable = "Customer",
                    PrimaryKeyColumn = "Id",
                    IsEnabled = true
                }
            }
        };
    }

    /// <summary>
    /// 建立多對多關聯的中介表。
    /// </summary>
    private TableDefinition CreateJunctionTable()
    {
        return new TableDefinition
        {
            TableName = "OrderCustomer",
            Columns = new List<ColumnDefinition>
            {
                new ColumnDefinition
                {
                    ColumnName = "OrderId",
                    IsPrimaryKey = true
                },
                new ColumnDefinition
                {
                    ColumnName = "CustomerId",
                    IsPrimaryKey = true
                }
            },
            ForeignKeys = new List<ForeignKeyDefinition>
            {
                new ForeignKeyDefinition
                {
                    ForeignKeyColumn = "OrderId",
                    PrimaryTable = "Order",
                    PrimaryKeyColumn = "Id",
                    IsEnabled = true
                },
                new ForeignKeyDefinition
                {
                    ForeignKeyColumn = "CustomerId",
                    PrimaryTable = "Customer",
                    PrimaryKeyColumn = "Id",
                    IsEnabled = true
                }
            }
        };
    }

    /// <summary>
    /// 建立客戶資料表定義。
    /// </summary>
    private TableDefinition CreateCustomerTable()
    {
        return new TableDefinition
        {
            TableName = "Customer",
            Columns = new List<ColumnDefinition>
            {
                new ColumnDefinition
                {
                    ColumnName = "Id",
                    IsPrimaryKey = true
                },
                new ColumnDefinition
                {
                    ColumnName = "Name"
                }
            }
        };
    }

    /// <summary>
    /// 建立無關聯的測試表格。
    /// </summary>
    private TableDefinition CreateTableWithNoRelation(string tableName)
    {
        return new TableDefinition
        {
            TableName = tableName,
            Columns = new List<ColumnDefinition>
            {
                new ColumnDefinition
                {
                    ColumnName = "Id",
                    IsPrimaryKey = true
                }
            },
            ForeignKeys = new List<ForeignKeyDefinition>()
        };
    }

    /// <summary>
    /// 建立會拋出異常的測試表格。
    /// </summary>
    private TableDefinition CreateTableThatThrowsException()
    {
        return new TableDefinition
        {
            TableName = "ErrorTable",
            Columns = null  // 將觸發 NullReferenceException
        };
    }

    #endregion
}
