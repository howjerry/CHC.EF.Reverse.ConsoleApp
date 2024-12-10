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
/// ���� RelationshipAnalyzer ���O���椸���աC
/// </summary>
/// <remarks>
/// ���ղ[�\�G
/// 1. �����p�����P�_
/// 2. ��ɱ���B�z
/// 3. ���~�����ˬd
/// 4. �ƦX�D�䱡�p
/// </remarks>
public class RelationshipAnalyzerTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly RelationshipAnalyzer _analyzer;

    /// <summary>
    /// ��l�ƴ������O���s��ҡC
    /// </summary>
    public RelationshipAnalyzerTests()
    {
        _loggerMock = new Mock<ILogger>();
        _analyzer = new RelationshipAnalyzer(_loggerMock.Object);
    }

    /// <summary>
    /// ���իغc�禡�Ѽ����ҡC
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
    /// ���դ��R��k���Ѽ����ҡC
    /// </summary>
    /// <param name="sourceTable">�ӷ����</param>
    /// <param name="targetTable">�ؼЪ��</param>
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
    /// ���դ@��@���p���ѧO�C
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
    /// ���դ@��h���p���ѧO�C
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
    /// ���զh��h���p���ѧO�C
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
    /// ���յL���p���p���B�z�C
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
    /// ���ղ��`���p���B�z�C
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
           .WithMessage("���R���p�ɵo�Ϳ��~*");
    }

    #region Helper Methods

    /// <summary>
    /// ���ѵL�Ī����ѼƲզX�C
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
    /// �إߨ㦳�@��@���p�����ժ��C
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
    /// �إߨ㦳�@��h���p�����ժ��C
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
    /// �إߦh��h���p��������C
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
    /// �إ߫Ȥ��ƪ�w�q�C
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
    /// �إߵL���p�����ժ��C
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
    /// �إ߷|�ߥX���`�����ժ��C
    /// </summary>
    private TableDefinition CreateTableThatThrowsException()
    {
        return new TableDefinition
        {
            TableName = "ErrorTable",
            Columns = null  // �NĲ�o NullReferenceException
        };
    }

    #endregion
}
