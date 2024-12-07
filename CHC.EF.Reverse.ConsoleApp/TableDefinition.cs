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