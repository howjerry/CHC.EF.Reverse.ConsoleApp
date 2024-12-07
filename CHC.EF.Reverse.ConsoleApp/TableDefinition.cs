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
    /// Represents a column definition in a table.
    /// </summary>
    public class ColumnDefinition
    {
        /// <summary>
        /// The name of the column.
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// The data type of the column.
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Indicates if the column allows null values.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Indicates if the column is part of the primary key.
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// The comment or description of the column.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// The maximum length of the column (if applicable).
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// The precision of the column (if applicable, e.g., for decimal types).
        /// </summary>
        public int? Precision { get; set; }

        /// <summary>
        /// The scale of the column (if applicable, e.g., for decimal types).
        /// </summary>
        public int? Scale { get; set; }
    }

    /// <summary>
    /// Represents a foreign key definition in a table.
    /// </summary>
    public class ForeignKeyDefinition
    {
        /// <summary>
        /// The column in the current table that is the foreign key.
        /// </summary>
        public string ForeignKeyColumn { get; set; }

        /// <summary>
        /// The name of the primary table referenced by the foreign key.
        /// </summary>
        public string PrimaryTable { get; set; }

        /// <summary>
        /// The column in the primary table referenced by the foreign key.
        /// </summary>
        public string PrimaryKeyColumn { get; set; }
    }
}