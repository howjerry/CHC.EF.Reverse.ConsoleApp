using System.Collections.Generic;
using System.Linq;

namespace CHC.EF.Reverse.ConsoleApp
{
    public class TableDefinition
    {
        public string TableName { get; set; }
        public string SchemaName { get; set; }
        public List<ColumnDefinition> Columns { get; set; } = new List<ColumnDefinition>();
        public List<ForeignKeyDefinition> ForeignKeys { get; set; } = new List<ForeignKeyDefinition>();
        public List<IndexDefinition> Indexes { get; set; } = new List<IndexDefinition>();
        public string Comment { get; set; }

        // Many-to-Many 判斷
        public bool IsManyToMany
        {
            get
            {
                // 檢查是否有兩個不同的外鍵關係
                var hasTwoDistinctForeignKeys = ForeignKeys.Count >= 2 &&
                    ForeignKeys.Select(fk => fk.PrimaryTable).Distinct().Count() >= 2;

                // 檢查複合主鍵
                var compositePkColumns = Columns.Where(c => c.IsPrimaryKey).ToList();
                var hasCompositePrimaryKey = compositePkColumns.Count >= 2;

                // 檢查這些主鍵是否都是外鍵
                var pkColumnsAreForeignKeys = compositePkColumns
                    .All(pk => ForeignKeys.Any(fk => fk.ForeignKeyColumn == pk.ColumnName));

                return hasTwoDistinctForeignKeys && hasCompositePrimaryKey && pkColumnsAreForeignKeys;
            }
        }

        // One-to-One 判斷
        public bool IsOneToOne(string foreignKeyColumn)
        {
            // 檢查是否為唯一外鍵
            return Indexes
                .Where(idx => idx.IsUnique && !idx.IsPrimaryKey)
                .Any(idx => idx.Columns.Count == 1 &&
                           idx.Columns[0].ColumnName == foreignKeyColumn);
        }

        // 檢查是否為中間表
        public bool IsJunctionTable
        {
            get
            {
                return ForeignKeys.Count >= 2 &&
                       Columns.Count <= ForeignKeys.Count + 2; // 允許少量額外欄位
            }
        }
    }

    public class ColumnDefinition
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }
        public bool IsComputed { get; set; }
        public string DefaultValue { get; set; }
        public int? MaxLength { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
        public string Comment { get; set; }
        public bool IsUnique { get; set; }
        public List<IndexDefinition> ParticipatingIndexes { get; set; } = new List<IndexDefinition>();

        // 新增的擴展屬性
        public string CollationType { get; set; }
        public bool IsRowVersion { get; set; }
        public string GeneratedType { get; set; } // ALWAYS/BY DEFAULT for generated columns
        public string ComputedColumnDefinition { get; set; }
    }

    public class ForeignKeyDefinition
    {
        public string ConstraintName { get; set; }
        public string ForeignKeyColumn { get; set; }
        public string PrimaryTable { get; set; }
        public string PrimaryKeyColumn { get; set; }
        public string DeleteRule { get; set; }
        public string UpdateRule { get; set; }
        public bool IsCompositeKey { get; set; }
        public string Comment { get; set; }
        public List<ForeignKeyColumnPair> ColumnPairs { get; set; } = new List<ForeignKeyColumnPair>();

        // 新增的擴展屬性
        public bool IsEnabled { get; set; } = true;
        public bool IsNotForReplication { get; set; }
    }

    public class ForeignKeyColumnPair
    {
        public string ForeignKeyColumn { get; set; }
        public string PrimaryKeyColumn { get; set; }
    }

    public class IndexDefinition
    {
        public string IndexName { get; set; }
        public bool IsUnique { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsDisabled { get; set; }
        public string IndexType { get; set; } // CLUSTERED/NONCLUSTERED
        public List<IndexColumnDefinition> Columns { get; set; } = new List<IndexColumnDefinition>();
        public bool IsClustered { get; set; }
        public string FilterDefinition { get; set; }
        public int FillFactor { get; set; }
        public bool IsPadded { get; set; }
    }

    public class IndexColumnDefinition
    {
        public string ColumnName { get; set; }
        public bool IsDescending { get; set; }
        public int KeyOrdinal { get; set; }
        public bool IsIncluded { get; set; }
    }
}