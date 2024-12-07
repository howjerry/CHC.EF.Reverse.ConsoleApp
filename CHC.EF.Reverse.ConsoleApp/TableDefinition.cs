using System.Collections.Generic;

namespace CHC.EF.Reverse.ConsoleApp
{
    public class TableDefinition
    {
        public string TableName { get; set; }
        public string SchemaName { get; set; }
        public string Comment { get; set; }
        public List<ColumnDefinition> Columns { get; set; } = new List<ColumnDefinition>();
        public List<ForeignKeyDefinition> ForeignKeys { get; set; } = new List<ForeignKeyDefinition>();
    }

    public class ColumnDefinition
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public string Comment { get; set; }
        public int? MaxLength { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
    }

    public class ForeignKeyDefinition
    {
        public string ForeignKeyColumn { get; set; }
        public string PrimaryTable { get; set; }
        public string PrimaryKeyColumn { get; set; }
    }
}