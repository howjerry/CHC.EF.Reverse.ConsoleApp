using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

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

                var dt = conn.GetSchema("Tables");
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    if (row["TABLE_TYPE"].ToString().Equals("BASE TABLE", StringComparison.OrdinalIgnoreCase))
                    {
                        tables.Add(new TableDefinition
                        {
                            TableName = row["TABLE_NAME"].ToString(),
                            SchemaName = row["TABLE_SCHEMA"].ToString()
                        });
                    }
                }

                foreach (var table in tables)
                {
                    // 取得外鍵定義
                    var fkQuery = @"
                        SELECT 
                            fk.name AS ConstraintName,
                            c1.name AS ForeignKeyColumn,
                            OBJECT_NAME(fk.referenced_object_id) AS PrimaryTable,
                            c2.name AS PrimaryKeyColumn,
                            fk.delete_referential_action_desc AS DeleteRule,
                            fk.update_referential_action_desc AS UpdateRule
                        FROM sys.foreign_keys fk
                        INNER JOIN sys.foreign_key_columns fkc 
                            ON fk.object_id = fkc.constraint_object_id
                        INNER JOIN sys.columns c1 
                            ON fkc.parent_object_id = c1.object_id 
                            AND fkc.parent_column_id = c1.column_id
                        INNER JOIN sys.columns c2 
                            ON fkc.referenced_object_id = c2.object_id 
                            AND fkc.referenced_column_id = c2.column_id
                        WHERE OBJECT_NAME(fk.parent_object_id) = @tableName
                        AND SCHEMA_NAME(fk.schema_id) = @schemaName";

                    using (var fkCmd = new SqlCommand(fkQuery, conn))
                    {
                        fkCmd.Parameters.AddWithValue("@tableName", table.TableName);
                        fkCmd.Parameters.AddWithValue("@schemaName", table.SchemaName);

                        using (var fkRdr = fkCmd.ExecuteReader())
                        {
                            while (fkRdr.Read())
                            {
                                table.ForeignKeys.Add(new ForeignKeyDefinition
                                {
                                    ConstraintName = fkRdr["ConstraintName"].ToString(),
                                    ForeignKeyColumn = fkRdr["ForeignKeyColumn"].ToString(),
                                    PrimaryTable = fkRdr["PrimaryTable"].ToString(),
                                    PrimaryKeyColumn = fkRdr["PrimaryKeyColumn"].ToString(),
                                    DeleteRule = fkRdr["DeleteRule"].ToString(),
                                    UpdateRule = fkRdr["UpdateRule"].ToString()
                                });
                            }
                        }
                    }
                }
            }

            return tables;
        }

        private string MapSqlServerType(string sqlType)
        {
            sqlType = sqlType.ToLower();
            return sqlType switch
            {
                "int" => "int",
                "bigint" => "long",
                "decimal" => "decimal",
                "money" => "decimal",
                "float" => "double",
                "datetime" => "DateTime",
                "datetimeoffset" => "DateTimeOffset",
                "xml" => "string",
                "json" => "string",
                "bit" => "bool",
                _ => "string"
            };
        }
    }
}
