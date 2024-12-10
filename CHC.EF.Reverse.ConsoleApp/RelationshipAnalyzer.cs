using CHC.EF.Reverse.ConsoleApp;
using System.Collections.Generic;

using System;
using System.Linq;

/// <summary>
/// 提供資料庫表格關聯分析的核心功能。
/// </summary>
/// <remarks>
/// 此類別負責分析資料庫表格之間的關聯類型，包括：
/// 1. 一對一關聯分析
/// 2. 一對多關聯分析
/// 3. 多對多關聯分析
/// 分析過程會考慮：外鍵約束、唯一性約束、參考完整性等因素。
/// </remarks>
public class RelationshipAnalyzer
{
    private readonly ILogger _logger;

    /// <summary>
    /// 初始化關聯分析器的新實例。
    /// </summary>
    /// <param name="logger">日誌記錄器</param>
    public RelationshipAnalyzer(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 分析指定資料表之間的關聯類型。
    /// </summary>
    /// <param name="sourceTable">來源資料表定義</param>
    /// <param name="targetTable">目標資料表定義</param>
    /// <returns>關聯類型和相關資訊</returns>
    public RelationshipType AnalyzeRelationship(TableDefinition sourceTable, TableDefinition targetTable)
    {
        try
        {
            _logger.Info($"開始分析資料表 {sourceTable.TableName} 和 {targetTable.TableName} 之間的關聯");

            // 檢查一對一關聯
            if (IsOneToOneRelationship(sourceTable, targetTable))
            {
                return new RelationshipType
                {
                    Type = RelationType.OneToOne,
                    SourceTable = sourceTable.TableName,
                    TargetTable = targetTable.TableName,
                    ForeignKeyColumns = GetRelatedForeignKeys(sourceTable, targetTable)
                };
            }

            // 檢查一對多關聯
            if (IsOneToManyRelationship(sourceTable, targetTable))
            {
                return new RelationshipType
                {
                    Type = RelationType.OneToMany,
                    SourceTable = sourceTable.TableName,
                    TargetTable = targetTable.TableName,
                    ForeignKeyColumns = GetRelatedForeignKeys(sourceTable, targetTable)
                };
            }

            // 檢查多對多關聯
            if (IsManyToManyRelationship(sourceTable, targetTable))
            {
                return new RelationshipType
                {
                    Type = RelationType.ManyToMany,
                    SourceTable = sourceTable.TableName,
                    TargetTable = targetTable.TableName,
                    JunctionTableInfo = GetJunctionTableInfo(sourceTable, targetTable)
                };
            }

            return new RelationshipType { Type = RelationType.Unknown };
        }
        catch (Exception ex)
        {
            _logger.Error($"分析表格關聯時發生錯誤: {ex.Message}", ex);
            throw new RelationshipAnalysisException("分析表格關聯時發生錯誤", ex);
        }
    }

    /// <summary>
    /// 檢查兩個資料表之間是否為一對一關聯。
    /// </summary>
    /// <param name="sourceTable">來源資料表</param>
    /// <param name="targetTable">目標資料表</param>
    /// <returns>如果是一對一關聯返回 true，否則返回 false</returns>
    private bool IsOneToOneRelationship(TableDefinition sourceTable, TableDefinition targetTable)
    {
        var foreignKeys = sourceTable.ForeignKeys
            .Where(fk => fk.PrimaryTable == targetTable.TableName)
            .ToList();

        return foreignKeys.Any(fk =>
            sourceTable.IsOneToOne(fk.ForeignKeyColumn) &&
            ValidateReferentialIntegrity(fk, sourceTable, targetTable));
    }

    /// <summary>
    /// 檢查兩個資料表之間是否為一對多關聯。
    /// </summary>
    /// <param name="sourceTable">來源資料表</param>
    /// <param name="targetTable">目標資料表</param>
    /// <returns>如果是一對多關聯返回 true，否則返回 false</returns>
    private bool IsOneToManyRelationship(TableDefinition sourceTable, TableDefinition targetTable)
    {
        var foreignKeys = sourceTable.ForeignKeys
            .Where(fk => fk.PrimaryTable == targetTable.TableName)
            .ToList();

        return foreignKeys.Any(fk =>
            !sourceTable.IsOneToOne(fk.ForeignKeyColumn) &&
            !fk.IsCompositeKey &&
            ValidateReferentialIntegrity(fk, sourceTable, targetTable));
    }

    /// <summary>
    /// 檢查兩個資料表之間是否為多對多關聯。
    /// </summary>
    /// <param name="sourceTable">來源資料表</param>
    /// <param name="targetTable">目標資料表</param>
    /// <returns>如果是多對多關聯返回 true，否則返回 false</returns>
    private bool IsManyToManyRelationship(TableDefinition sourceTable, TableDefinition targetTable)
    {
        // 檢查是否存在連接這兩個表的中間表
        return sourceTable.ForeignKeys
            .Where(fk => fk.PrimaryTable == targetTable.TableName)
            .Any(fk => sourceTable.IsManyToMany);
    }

    /// <summary>
    /// 驗證外鍵約束的參考完整性。
    /// </summary>
    /// <param name="foreignKey">外鍵定義</param>
    /// <param name="sourceTable">來源資料表</param>
    /// <param name="targetTable">目標資料表</param>
    /// <returns>如果參考完整性有效返回 true，否則返回 false</returns>
    private bool ValidateReferentialIntegrity(
        ForeignKeyDefinition foreignKey,
        TableDefinition sourceTable,
        TableDefinition targetTable)
    {
        try
        {
            // 檢查外鍵欄位是否存在於來源表
            var foreignKeyColumn = sourceTable.Columns
                .FirstOrDefault(c => c.ColumnName == foreignKey.ForeignKeyColumn);
            if (foreignKeyColumn == null)
                return false;

            // 檢查參考的主鍵欄位是否存在於目標表
            var primaryKeyColumn = targetTable.Columns
                .FirstOrDefault(c => c.ColumnName == foreignKey.PrimaryKeyColumn);
            if (primaryKeyColumn == null)
                return false;

            // 檢查資料類型是否匹配
            if (foreignKeyColumn.DataType != primaryKeyColumn.DataType)
                return false;

            // 檢查是否啟用了外鍵約束
            return foreignKey.IsEnabled;
        }
        catch (Exception ex)
        {
            _logger.Error($"驗證參考完整性時發生錯誤: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 獲取相關的外鍵欄位資訊。
    /// </summary>
    private List<ForeignKeyInfo> GetRelatedForeignKeys(TableDefinition sourceTable, TableDefinition targetTable)
    {
        // 實作取得外鍵資訊的邏輯
        return sourceTable.ForeignKeys
            .Where(fk => fk.PrimaryTable == targetTable.TableName)
            .Select(fk => new ForeignKeyInfo
            {
                ForeignKeyColumn = fk.ForeignKeyColumn,
                PrimaryKeyColumn = fk.PrimaryKeyColumn,
                DeleteRule = fk.DeleteRule,
                UpdateRule = fk.UpdateRule
            })
            .ToList();
    }

    /// <summary>
    /// 獲取多對多關聯的中間表資訊。
    /// </summary>
    private JunctionTableInfo GetJunctionTableInfo(TableDefinition sourceTable, TableDefinition targetTable)
    {
        // 實作取得中間表資訊的邏輯
        return new JunctionTableInfo
        {
            TableName = sourceTable.TableName,
            SourceKeyColumns = sourceTable.ForeignKeys
                .Where(fk => fk.PrimaryTable == targetTable.TableName)
                .Select(fk => fk.ForeignKeyColumn)
                .ToList()
        };
    }
}
/// <summary>
/// 定義資料庫表格之間可能的關聯類型。
/// </summary>
public enum RelationType
{
    /// <summary>
    /// 未知或無法確定的關聯類型
    /// </summary>
    Unknown,

    /// <summary>
    /// 一對一關聯
    /// </summary>
    OneToOne,

    /// <summary>
    /// 一對多關聯
    /// </summary>
    OneToMany,

    /// <summary>
    /// 多對多關聯
    /// </summary>
    ManyToMany
}
/// <summary>
/// 定義關聯分析的結果類型。
/// </summary>
public class RelationshipType
{
    public RelationType Type { get; set; }
    public string SourceTable { get; set; }
    public string TargetTable { get; set; }
    public List<ForeignKeyInfo> ForeignKeyColumns { get; set; }
    public JunctionTableInfo JunctionTableInfo { get; set; }
}

/// <summary>
/// 定義外鍵資訊。
/// </summary>
public class ForeignKeyInfo
{
    public string ForeignKeyColumn { get; set; }
    public string PrimaryKeyColumn { get; set; }
    public string DeleteRule { get; set; }
    public string UpdateRule { get; set; }
}

/// <summary>
/// 定義多對多關聯中間表的資訊。
/// </summary>
public class JunctionTableInfo
{
    public string TableName { get; set; }
    public List<string> SourceKeyColumns { get; set; }
}

/// <summary>
/// 關聯分析過程中可能發生的異常。
/// </summary>
public class RelationshipAnalysisException : Exception
{
    public RelationshipAnalysisException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}