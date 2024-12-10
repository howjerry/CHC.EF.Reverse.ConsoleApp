using System.Collections.Generic;

using System;
using System.Linq;
using CHC.EF.Reverse.ConsoleApp.Exceptions;
using CHC.EF.Reverse.ConsoleApp.Core.Interfaces;
using CHC.EF.Reverse.ConsoleApp.Core.Models;

/// <summary>
/// 提供資料庫表格間關聯關係的分析功能。
/// </summary>
/// <remarks>
/// 支援分析以下類型的關聯：
/// - 一對一 (One-to-One)
/// - 一對多 (One-to-Many)
/// - 多對多 (Many-to-Many)
/// 
/// 分析過程會考慮：
/// - 主鍵配置
/// - 外鍵約束
/// - 唯一索引
/// - 可為空性
/// </remarks>
public class RelationshipAnalyzer
{
    private readonly ILogger _logger;

    /// <summary>
    /// 初始化關聯分析器的新實例。
    /// </summary>
    /// <param name="logger">日誌記錄服務</param>
    /// <exception cref="ArgumentNullException">當 logger 為 null 時擲出</exception>
    public RelationshipAnalyzer(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 分析兩個資料表之間的關聯類型。
    /// </summary>
    /// <param name="sourceTable">來源資料表</param>
    /// <param name="targetTable">目標資料表</param>
    /// <returns>關聯類型定義</returns>
    /// <exception cref="ArgumentNullException">當任一參數為 null 時擲出</exception>
    /// <exception cref="RelationshipAnalysisException">分析過程發生錯誤時擲出</exception>
    public RelationshipType AnalyzeRelationship(TableDefinition sourceTable, TableDefinition targetTable)
    {
        ValidateParameters(sourceTable, targetTable);

        try
        {
            _logger.Info($"開始分析資料表關聯: {sourceTable.TableName} -> {targetTable.TableName}");

            // 獲取有效的外鍵定義
            var foreignKeys = GetValidForeignKeys(sourceTable, targetTable);
            if (!foreignKeys.Any())
            {
                return CreateUnknownRelationship();
            }

            // 檢查是否為中介表
            if (IsJunctionTable(sourceTable, foreignKeys))
            {
                return CreateManyToManyRelationship(sourceTable);
            }

            // 分析一般關聯類型
            return AnalyzeStandardRelationship(sourceTable, targetTable, foreignKeys);
        }
        catch (Exception ex)
        {
            var message = $"分析關聯時發生錯誤: {sourceTable.TableName} -> {targetTable.TableName}";
            _logger.Error(message, ex);
            throw new RelationshipAnalysisException(message, ex);
        }
    }

    /// <summary>
    /// 驗證輸入參數的有效性。
    /// </summary>
    private void ValidateParameters(TableDefinition sourceTable, TableDefinition targetTable)
    {
        if (sourceTable == null) throw new ArgumentNullException(nameof(sourceTable));
        if (targetTable == null) throw new ArgumentNullException(nameof(targetTable));
        if (string.IsNullOrEmpty(sourceTable.TableName))
            throw new ArgumentException("來源表格名稱不可為空", nameof(sourceTable));
        if (string.IsNullOrEmpty(targetTable.TableName))
            throw new ArgumentException("目標表格名稱不可為空", nameof(targetTable));
    }

    /// <summary>
    /// 獲取兩個表格間有效的外鍵定義。
    /// </summary>
    private List<ForeignKeyDefinition> GetValidForeignKeys(
        TableDefinition sourceTable,
        TableDefinition targetTable)
    {
        return sourceTable.ForeignKeys
            .Where(fk => fk.PrimaryTable == targetTable.TableName && fk.IsEnabled)
            .ToList();
    }

    /// <summary>
    /// 檢查表格是否為多對多關聯的中介表。
    /// </summary>
    private bool IsJunctionTable(
        TableDefinition table,
        List<ForeignKeyDefinition> foreignKeys)
    {
        // 基本條件檢查
        if (table.Columns.Count > 4 || foreignKeys.Count != 2)
            return false;

        // 檢查是否所有主鍵都是外鍵
        var primaryKeyColumns = table.Columns
            .Where(c => c.IsPrimaryKey)
            .Select(c => c.ColumnName)
            .ToList();

        if (primaryKeyColumns.Count != 2)
            return false;

        var foreignKeyColumns = foreignKeys
            .Select(fk => fk.ForeignKeyColumn)
            .ToList();

        return primaryKeyColumns.All(pk => foreignKeyColumns.Contains(pk));
    }

    /// <summary>
    /// 分析標準的一對一或一對多關聯。
    /// </summary>
    private RelationshipType AnalyzeStandardRelationship(
        TableDefinition sourceTable,
        TableDefinition targetTable,
        List<ForeignKeyDefinition> foreignKeys)
    {
        var sourceKeyColumns = GetSourceKeyColumns(sourceTable, foreignKeys);
        var targetKeyColumns = GetTargetKeyColumns(targetTable, foreignKeys);

        if (IsOneToOneRelationship(sourceKeyColumns, targetKeyColumns))
        {
            return CreateOneToOneRelationship(sourceTable, targetTable, foreignKeys);
        }

        return CreateOneToManyRelationship(sourceTable, targetTable, foreignKeys);
    }

    /// <summary>
    /// 獲取來源表格的關聯欄位。
    /// </summary>
    private List<ColumnDefinition> GetSourceKeyColumns(
        TableDefinition table,
        List<ForeignKeyDefinition> foreignKeys)
    {
        var foreignKeyColumns = foreignKeys
            .Select(fk => fk.ForeignKeyColumn)
            .ToList();

        return table.Columns
            .Where(c => foreignKeyColumns.Contains(c.ColumnName))
            .ToList();
    }

    /// <summary>
    /// 獲取目標表格的關聯欄位。
    /// </summary>
    private List<ColumnDefinition> GetTargetKeyColumns(
        TableDefinition table,
        List<ForeignKeyDefinition> foreignKeys)
    {
        var primaryKeyColumns = foreignKeys
            .Select(fk => fk.PrimaryKeyColumn)
            .ToList();

        return table.Columns
            .Where(c => primaryKeyColumns.Contains(c.ColumnName))
            .ToList();
    }

    /// <summary>
    /// 判斷是否為一對一關聯。
    /// </summary>
    private bool IsOneToOneRelationship(
        List<ColumnDefinition> sourceColumns,
        List<ColumnDefinition> targetColumns)
    {
        // 檢查欄位數量是否相同
        if (sourceColumns.Count != targetColumns.Count)
            return false;

        // 確認所有來源欄位都是主鍵
        if (!sourceColumns.All(c => c.IsPrimaryKey))
            return false;

        // 確認所有目標欄位都是主鍵
        return targetColumns.All(c => c.IsPrimaryKey);
    }

    /// <summary>
    /// 建立未知關聯類型的結果。
    /// </summary>
    private RelationshipType CreateUnknownRelationship()
    {
        return new RelationshipType { Type = RelationType.Unknown };
    }

    /// <summary>
    /// 建立多對多關聯的結果。
    /// </summary>
    private RelationshipType CreateManyToManyRelationship(TableDefinition junctionTable)
    {
        var foreignKeys = junctionTable.ForeignKeys.ToList();
        return new RelationshipType
        {
            Type = RelationType.ManyToMany,
            SourceTable = foreignKeys[0].PrimaryTable,
            TargetTable = foreignKeys[1].PrimaryTable,
            JunctionTableInfo = new JunctionTableInfo
            {
                TableName = junctionTable.TableName,
                SourceKeyColumns = foreignKeys
                    .Select(fk => fk.ForeignKeyColumn)
                    .ToList()
            },
            ForeignKeyColumns = MapForeignKeyInfo(foreignKeys)
        };
    }

    /// <summary>
    /// 建立一對一關聯的結果。
    /// </summary>
    private RelationshipType CreateOneToOneRelationship(
        TableDefinition sourceTable,
        TableDefinition targetTable,
        List<ForeignKeyDefinition> foreignKeys)
    {
        return new RelationshipType
        {
            Type = RelationType.OneToOne,
            SourceTable = sourceTable.TableName,
            TargetTable = targetTable.TableName,
            ForeignKeyColumns = MapForeignKeyInfo(foreignKeys)
        };
    }

    /// <summary>
    /// 建立一對多關聯的結果。
    /// </summary>
    private RelationshipType CreateOneToManyRelationship(
        TableDefinition sourceTable,
        TableDefinition targetTable,
        List<ForeignKeyDefinition> foreignKeys)
    {
        return new RelationshipType
        {
            Type = RelationType.OneToMany,
            SourceTable = foreignKeys[0].PrimaryTable,
            TargetTable = sourceTable.TableName,
            ForeignKeyColumns = MapForeignKeyInfo(foreignKeys)
        };
    }

    /// <summary>
    /// 映射外鍵資訊到標準格式。
    /// </summary>
    private List<ForeignKeyInfo> MapForeignKeyInfo(List<ForeignKeyDefinition> foreignKeys)
    {
        return foreignKeys.Select(fk => new ForeignKeyInfo
        {
            ForeignKeyColumn = fk.ForeignKeyColumn,
            PrimaryKeyColumn = fk.PrimaryKeyColumn,
            DeleteRule = fk.DeleteRule,
            UpdateRule = fk.UpdateRule
        }).ToList();
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
    /// <summary>
    /// 取得或設定中間表名稱。
    /// </summary>
    public string TableName { get; set; }

    /// <summary>
    /// 取得或設定來源鍵欄位清單。
    /// </summary>
    public List<string> SourceKeyColumns { get; set; }

    /// <summary>
    /// 取得或設定額外欄位定義清單。
    /// </summary>
    public List<ColumnDefinition> AdditionalColumns { get; set; }
}
