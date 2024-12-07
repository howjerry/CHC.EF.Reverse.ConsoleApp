using Microsoft.Data.SqlClient;
using System.Data;

namespace CHC.EF.Reverse.ConsoleApp
{
    public class SqlServerSchemaReader : IDatabaseSchemaReader
    {
        private readonly string _connectionString;

        public SqlServerSchemaReader(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<TableDefinition> ReadTables()
        {
            var tables = new List<TableDefinition>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Read table definitions
                var dt = conn.GetSchema("Tables");
                foreach (DataRow row in dt.Rows)
                {
                    if (row["TABLE_TYPE"].ToString().Equals("BASE TABLE", StringComparison.OrdinalIgnoreCase))
                    {
                        var table = new TableDefinition
                        {
                            TableName = row["TABLE_NAME"].ToString(),
                            SchemaName = row["TABLE_SCHEMA"].ToString()
                        };
                        tables.Add(table);
                    }
                }

                foreach (var table in tables)
                {
                    // Read columns
                    var columnQuery = $@"
                        SELECT 
                            COLUMN_NAME, 
                            DATA_TYPE, 
                            IS_NULLABLE, 
                            CHARACTER_MAXIMUM_LENGTH, 
                            NUMERIC_PRECISION, 
                            NUMERIC_SCALE
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = '{table.TableName}' AND TABLE_SCHEMA = '{table.SchemaName}'";

                    using (var colCmd = new SqlCommand(columnQuery, conn))
                    using (var colRdr = colCmd.ExecuteReader())
                    {
                        while (colRdr.Read())
                        {
                            var column = new ColumnDefinition
                            {
                                ColumnName = colRdr["COLUMN_NAME"].ToString(),
                                DataType = colRdr["DATA_TYPE"].ToString(),
                                IsNullable = colRdr["IS_NULLABLE"].ToString().Equals("YES", StringComparison.OrdinalIgnoreCase),
                                MaxLength = colRdr["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value
                                    ? Convert.ToInt32(colRdr["CHARACTER_MAXIMUM_LENGTH"])
                                    : (int?)null,
                                Precision = colRdr["NUMERIC_PRECISION"] != DBNull.Value
                                    ? Convert.ToInt32(colRdr["NUMERIC_PRECISION"])
                                    : (int?)null,
                                Scale = colRdr["NUMERIC_SCALE"] != DBNull.Value
                                    ? Convert.ToInt32(colRdr["NUMERIC_SCALE"])
                                    : (int?)null
                            };
                            table.Columns.Add(column);
                        }
                    }

                    // Read foreign keys
                    var foreignKeyQuery = $@"
                        SELECT 
                            FK.COLUMN_NAME AS ForeignKey, 
                            PK.TABLE_NAME AS PrimaryTable, 
                            PK.COLUMN_NAME AS PrimaryKey 
                        FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC
                        JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE FK 
                            ON RC.CONSTRAINT_NAME = FK.CONSTRAINT_NAME
                        JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE PK 
                            ON RC.UNIQUE_CONSTRAINT_NAME = PK.CONSTRAINT_NAME
                        WHERE FK.TABLE_NAME = '{table.TableName}' AND FK.TABLE_SCHEMA = '{table.SchemaName}'";

                    using (var fkCmd = new SqlCommand(foreignKeyQuery, conn))
                    using (var fkRdr = fkCmd.ExecuteReader())
                    {
                        while (fkRdr.Read())
                        {
                            var foreignKey = new ForeignKeyDefinition
                            {
                                ForeignKeyColumn = fkRdr["ForeignKey"].ToString(),
                                PrimaryTable = fkRdr["PrimaryTable"].ToString(),
                                PrimaryKeyColumn = fkRdr["PrimaryKey"].ToString()
                            };
                            table.ForeignKeys.Add(foreignKey);
                        }
                    }
                }
            }

            return tables;
        }
    }
}