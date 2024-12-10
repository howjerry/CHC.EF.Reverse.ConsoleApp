using CHC.EF.Reverse.ConsoleApp;
using System.Collections.Generic;

using System;
using System.Linq;

/// <summary>
/// 提供資料庫關聯分析的核心功能。
/// </summary>
public class RelationshipAnalyzer
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, HashSet<string>> _analyzedRelationships;

    /// <summary>
    /// 初始化關聯分析器的新實例。
    /// </summary>
    /// <param name="logger">日誌記錄服務</param>
    public RelationshipAnalyzer(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _analyzedRelationships = new Dictionary<string, HashSet<string>>();
    }

    /// <summary>
    /// 分析指定資料表間的關聯類型。
    /// </summary>
    /// <param name="sourceTable">來源資料表定義</param>
    /// <param name="targetTable">目標資料表定義</param>
    /// <returns>關聯類型定義</returns>
    public RelationshipType AnalyzeRelationship(TableDefinition sourceTable, TableDefinition targetTable)
    {
        ValidateInput(sourceTable, targetTable);

        try
        {
            // 避免重複分析相同關聯
            if (HasAnalyzedRelationship(sourceTable.TableName, targetTable.TableName))
            {
                return new RelationshipType { Type = RelationType.Unknown };
            }

            _logger.Info($"分析資料表關聯: {sourceTable.TableName} -> {targetTable.TableName}");

            // 尋找外鍵關係
            var foreignKeys = GetValidForeignKeys(sourceTable, targetTable);
            if (!foreignKeys.Any())
            {
                return new RelationshipType { Type = RelationType.Unknown };
            }

            var relationship = DetectRelationshipType(sourceTable, targetTable, foreignKeys);
            MarkRelationshipAsAnalyzed(sourceTable.TableName, targetTable.TableName);

            return relationship;
        }
        catch (Exception ex)
        {
            _logger.Error($"關聯分析錯誤: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 檢測關聯類型。
    /// </summary>
    private RelationshipType DetectRelationshipType(
        TableDefinition sourceTable,
        TableDefinition targetTable,
        List<ForeignKeyDefinition> foreignKeys)
    {
        // 檢查是否為多對多關聯的中間表
        if (IsJunctionTable(sourceTable))
        {
            return CreateManyToManyRelationship(sourceTable);
        }

        var firstForeignKey = foreignKeys.First();

        // 檢查一對一關聯
        if (HasUniqueConstraint(sourceTable, firstForeignKey.ForeignKeyColumn))
        {
            return new RelationshipType
            {
                Type = RelationType.OneToOne,
                SourceTable = sourceTable.TableName,
                TargetTable = targetTable.TableName,
                ForeignKeyColumns = MapForeignKeyInfo(foreignKeys)
            };
        }

        // 一對多關聯
        return new RelationshipType
        {
            Type = RelationType.OneToMany,
            SourceTable = firstForeignKey.PrimaryTable,
            TargetTable = sourceTable.TableName,
            ForeignKeyColumns = MapForeignKeyInfo(foreignKeys)
        };
    }

    /// <summary>
    /// 檢查資料表是否為多對多關聯的中間表。
    /// </summary>
    private bool IsJunctionTable(TableDefinition table)
    {
        if (table.ForeignKeys.Count != 2) return false;

        var primaryKeyColumns = table.Columns
            .Where(c => c.IsPrimaryKey)
            .Select(c => c.ColumnName)
            .ToList();

        var foreignKeyColumns = table.ForeignKeys
            .Select(fk => fk.ForeignKeyColumn)
            .ToList();

        return primaryKeyColumns.Count == 2 &&
               primaryKeyColumns.All(pk => foreignKeyColumns.Contains(pk));
    }

    /// <summary>
    /// 建立多對多關聯定義。
    /// </summary>
    private RelationshipType CreateManyToManyRelationship(TableDefinition junctionTable)
    {
        return new RelationshipType
        {
            Type = RelationType.ManyToMany,
            SourceTable = junctionTable.ForeignKeys[0].PrimaryTable,
            TargetTable = junctionTable.ForeignKeys[1].PrimaryTable,
            JunctionTableInfo = new JunctionTableInfo
            {
                TableName = junctionTable.TableName,
                SourceKeyColumns = junctionTable.ForeignKeys
                    .Select(fk => fk.ForeignKeyColumn)
                    .ToList()
            },
            ForeignKeyColumns = MapForeignKeyInfo(junctionTable.ForeignKeys)
        };
    }

    /// <summary>
    /// 檢查欄位是否具有唯一性約束。
    /// </summary>
    private bool HasUniqueConstraint(TableDefinition table, string columnName)
    {
        return table.Indexes.Any(idx =>
            idx.IsUnique &&
            idx.Columns.Count == 1 &&
            idx.Columns[0].ColumnName == columnName);
    }

    /// <summary>
    /// 取得有效的外鍵定義。
    /// </summary>
    private List<ForeignKeyDefinition> GetValidForeignKeys(
        TableDefinition sourceTable,
        TableDefinition targetTable)
    {
        return sourceTable.ForeignKeys
            .Where(fk => fk.PrimaryTable == targetTable.TableName)
            .ToList();
    }

    /// <summary>
    /// 映射外鍵資訊。
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

    /// <summary>
    /// 檢查是否已分析過指定的關聯。
    /// </summary>
    private bool HasAnalyzedRelationship(string sourceTable, string targetTable)
    {
        var key = GetRelationshipKey(sourceTable, targetTable);
        return _analyzedRelationships.ContainsKey(key) &&
               _analyzedRelationships[key].Contains(targetTable);
    }

    /// <summary>
    /// 標記關聯已被分析。
    /// </summary>
    private void MarkRelationshipAsAnalyzed(string sourceTable, string targetTable)
    {
        var key = GetRelationshipKey(sourceTable, targetTable);
        if (!_analyzedRelationships.ContainsKey(key))
        {
            _analyzedRelationships[key] = new HashSet<string>();
        }
        _analyzedRelationships[key].Add(targetTable);
    }

    /// <summary>
    /// 取得關聯的唯一識別碼。
    /// </summary>
    private string GetRelationshipKey(string sourceTable, string targetTable)
    {
        var tables = new[] { sourceTable, targetTable }.OrderBy(t => t);
        return string.Join("_", tables);
    }

    /// <summary>
    /// 驗證輸入參數。
    /// </summary>
    private void ValidateInput(TableDefinition sourceTable, TableDefinition targetTable)
    {
        if (sourceTable == null) throw new ArgumentNullException(nameof(sourceTable));
        if (targetTable == null) throw new ArgumentNullException(nameof(targetTable));
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