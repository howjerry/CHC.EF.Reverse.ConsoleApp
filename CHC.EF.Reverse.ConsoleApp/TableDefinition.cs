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

        /// <summary>
        /// 判斷指定的外鍵欄位是否構成一對一關聯。
        /// </summary>
        /// <param name="foreignKeyColumn">要檢查的外鍵欄位名稱</param>
        /// <returns>如果構成一對一關聯返回 true，否則返回 false</returns>
        /// <remarks>
        /// 判斷條件包括：
        /// 1. 存在唯一索引約束
        /// 2. 該欄位為單一外鍵欄位
        /// 3. 不是主鍵的一部分
        /// </remarks>
        public bool IsOneToOne(string foreignKeyColumn)
        {
            // 驗證參數
            if (string.IsNullOrEmpty(foreignKeyColumn))
                return false;

            // 檢查是否為有效的外鍵欄位
            var foreignKey = ForeignKeys.FirstOrDefault(fk =>
                fk.ForeignKeyColumn == foreignKeyColumn);
            if (foreignKey == null)
                return false;

            // 檢查唯一索引約束
            var hasUniqueConstraint = Indexes.Any(idx =>
                idx.IsUnique &&
                !idx.IsPrimaryKey &&
                idx.Columns.Count == 1 &&
                idx.Columns[0].ColumnName == foreignKeyColumn);

            // 檢查是否為單一外鍵欄位
            var isSingleForeignKey = !ForeignKeys.Any(fk =>
                fk.ForeignKeyColumn == foreignKeyColumn &&
                fk.IsCompositeKey);

            // 檢查不是主鍵的一部分
            var notPartOfPrimaryKey = !Columns
                .Where(c => c.IsPrimaryKey)
                .Any(c => c.ColumnName == foreignKeyColumn);

            return hasUniqueConstraint && isSingleForeignKey && notPartOfPrimaryKey;
        }

        /// <summary>
        /// 判斷此表是否為多對多關聯的中間表。
        /// </summary>
        /// <returns>如果是多對多關聯的中間表返回 true，否則返回 false</returns>
        /// <remarks>
        /// 判斷條件包括：
        /// 1. 至少有兩個不同的外鍵關係
        /// 2. 具有複合主鍵
        /// 3. 主鍵欄位都是外鍵
        /// 4. 除了關聯欄位外只能有少量額外欄位
        /// </remarks>
        public bool IsManyToMany
        {
            get
            {
                // 檢查是否有至少兩個不同的外鍵
                var distinctForeignKeys = ForeignKeys
                    .Select(fk => fk.PrimaryTable)
                    .Distinct()
                    .Count();

                if (distinctForeignKeys < 2)
                    return false;

                // 獲取主鍵欄位
                var primaryKeyColumns = Columns
                    .Where(c => c.IsPrimaryKey)
                    .ToList();

                // 檢查複合主鍵
                if (primaryKeyColumns.Count < 2)
                    return false;

                // 檢查主鍵欄位是否都是外鍵
                var allPrimaryKeysAreForeignKeys = primaryKeyColumns
                    .All(pk => ForeignKeys
                        .Any(fk => fk.ForeignKeyColumn == pk.ColumnName));

                if (!allPrimaryKeysAreForeignKeys)
                    return false;

                // 檢查額外欄位數量
                var nonKeyColumnCount = Columns.Count - primaryKeyColumns.Count;
                const int MaxAdditionalColumns = 3; // 可配置的閾值

                return nonKeyColumnCount <= MaxAdditionalColumns;
            }
        }

        /// <summary>
        /// 檢查表是否為中間表（包含關聯資料的輔助表）。
        /// </summary>
        /// <returns>如果是中間表返回 true，否則返回 false</returns>
        /// <remarks>
        /// 判斷條件包括：
        /// 1. 至少有兩個外鍵
        /// 2. 欄位總數不超過外鍵數量加上允許的額外欄位數
        /// </remarks>
        public bool IsJunctionTable
        {
            get
            {
                if (ForeignKeys.Count < 2)
                    return false;

                const int MaxAdditionalColumns = 2;
                return Columns.Count <= ForeignKeys.Count + MaxAdditionalColumns;
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
        public int FillFactor { get; set; } = 0;
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
