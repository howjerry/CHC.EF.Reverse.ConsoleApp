namespace CHC.EF.Reverse.ConsoleApp
{
    /// <summary>
    /// Represents a table definition in the database.
    /// </summary>
    public class TableDefinition
    {
        /// <summary>
        /// The name of the table.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The schema of the table.
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// The list of columns in the table.
        /// </summary>
        public List<ColumnDefinition> Columns { get; set; } = new List<ColumnDefinition>();

        /// <summary>
        /// The list of foreign keys in the table.
        /// </summary>
        public List<ForeignKeyDefinition> ForeignKeys { get; set; } = new List<ForeignKeyDefinition>();

        /// <summary>
        /// The comment or description of the table.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Determines if the table is a many-to-many junction table.
        /// </summary>
        public bool IsManyToMany
        {
            get
            {
                return Columns.Count == 2 &&
                       Columns.All(c => c.IsPrimaryKey) &&
                       ForeignKeys.Count == 2 &&
                       ForeignKeys.Select(fk => fk.PrimaryTable).Distinct().Count() == 2;
            }
        }
    }

    /// <summary>
    /// 代表資料庫表格中的欄位定義。
    /// 包含欄位的所有基本屬性和特性。
    /// </summary>
    public class ColumnDefinition
    {
        /// <summary>
        /// 取得或設定欄位名稱。
        /// </summary>
        /// <example>
        /// CustomerID, FirstName, OrderDate 等
        /// </example>
        public string ColumnName { get; set; }

        /// <summary>
        /// 取得或設定欄位的資料類型。
        /// 這是資料庫原生的資料類型名稱。
        /// </summary>
        /// <example>
        /// varchar, int, datetime, decimal 等
        /// </example>
        public string DataType { get; set; }

        /// <summary>
        /// 取得或設定欄位是否允許 NULL 值。
        /// </summary>
        /// <value>
        /// true 表示欄位允許 NULL 值；
        /// false 表示欄位為 NOT NULL。
        /// </value>
        public bool IsNullable { get; set; }

        /// <summary>
        /// 取得或設定欄位是否為主鍵的一部分。
        /// </summary>
        /// <value>
        /// true 表示此欄位是主鍵；
        /// false 表示此欄位不是主鍵。
        /// </value>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// 取得或設定欄位是否為識別欄位（自動遞增）。
        /// </summary>
        /// <value>
        /// true 表示此欄位是識別欄位；
        /// false 表示此欄位不是識別欄位。
        /// </value>
        public bool IsIdentity { get; set; }

        /// <summary>
        /// 取得或設定欄位是否為計算欄位。
        /// </summary>
        /// <value>
        /// true 表示此欄位是計算欄位；
        /// false 表示此欄位不是計算欄位。
        /// </value>
        public bool IsComputed { get; set; }

        /// <summary>
        /// 取得或設定欄位的預設值。
        /// </summary>
        /// <value>
        /// 欄位的預設值表達式，如果沒有預設值則為 null。
        /// </value>
        /// <example>
        /// GETDATE(), 0, 'N/A' 等
        /// </example>
        public string DefaultValue { get; set; }

        /// <summary>
        /// 取得或設定字串類型欄位的最大長度。
        /// 僅適用於字串或二進位資料類型。
        /// </summary>
        /// <value>
        /// 欄位的最大長度，如果不適用則為 null。
        /// -1 表示 MAX。
        /// </value>
        public int? MaxLength { get; set; }

        /// <summary>
        /// 取得或設定數值類型的精確度。
        /// 主要用於 decimal/numeric 類型。
        /// </summary>
        /// <value>
        /// 數值的總位數，如果不適用則為 null。
        /// </value>
        public int? Precision { get; set; }

        /// <summary>
        /// 取得或設定數值類型的小數位數。
        /// 主要用於 decimal/numeric 類型。
        /// </summary>
        /// <value>
        /// 小數點後的位數，如果不適用則為 null。
        /// </value>
        public int? Scale { get; set; }

        /// <summary>
        /// 取得或設定欄位的描述或註解。
        /// 通常來自資料庫的欄位註解。
        /// </summary>
        public string Comment { get; set; }
    }


    /// <summary>
    /// Represents a foreign key definition in a table.
    /// </summary>
    public class ForeignKeyDefinition
    {
        /// <summary>外鍵約束名稱</summary>
        public string ConstraintName { get; set; }

        /// <summary>外鍵欄位名稱</summary>
        public string ForeignKeyColumn { get; set; }

        /// <summary>參考的主表名稱</summary>
        public string PrimaryTable { get; set; }

        /// <summary>參考的主鍵欄位名稱</summary>
        public string PrimaryKeyColumn { get; set; }

        /// <summary>刪除規則（CASCADE, SET NULL, NO ACTION 等）</summary>
        public string DeleteRule { get; set; }

        /// <summary>更新規則（CASCADE, SET NULL, NO ACTION 等）</summary>
        public string UpdateRule { get; set; }

        /// <summary>是否為複合外鍵</summary>
        public bool IsCompositeKey { get; set; }

        /// <summary>附加備註</summary>
        public string Comment { get; set; }
    }

}