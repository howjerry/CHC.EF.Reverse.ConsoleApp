using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace CHC.EF.Reverse.ConsoleApp
{
    public class MySqlSchemaReader : IDatabaseSchemaReader
    {
        private readonly string _connectionString;

        public MySqlSchemaReader(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<TableDefinition> ReadTables()
        {
            var tables = new List<TableDefinition>();

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                // 取得所有資料表
                using (var cmd = new MySqlCommand("SHOW FULL TABLES WHERE Table_type = 'BASE TABLE'", conn))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        tables.Add(new TableDefinition
                        {
                            TableName = rdr.GetString(0),
                            SchemaName = conn.Database
                        });
                    }
                }

                foreach (var table in tables)
                {
                    // 取得欄位定義
                    using (var colCmd = new MySqlCommand($"SHOW FULL COLUMNS FROM `{table.TableName}`", conn))
                    using (var colRdr = colCmd.ExecuteReader())
                    {
                        while (colRdr.Read())
                        {
                            table.Columns.Add(new ColumnDefinition
                            {
                                ColumnName = colRdr["Field"].ToString(),
                                DataType = ParseDataType(colRdr["Type"].ToString()),
                                IsNullable = colRdr["Null"].ToString().Equals("YES", StringComparison.OrdinalIgnoreCase),
                                IsPrimaryKey = colRdr["Key"].ToString().Equals("PRI", StringComparison.OrdinalIgnoreCase),
                                Comment = colRdr["Comment"].ToString()
                            });
                        }
                    }

                    // 取得外鍵定義
                    var fkQuery = @"
                        SELECT 
                            ku.CONSTRAINT_NAME AS ConstraintName,
                            ku.COLUMN_NAME AS ForeignKeyColumn,
                            ku.REFERENCED_TABLE_NAME AS PrimaryTable,
                            ku.REFERENCED_COLUMN_NAME AS PrimaryKeyColumn,
                            rc.DELETE_RULE AS DeleteRule,
                            rc.UPDATE_RULE AS UpdateRule
                        FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                        JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                            ON rc.CONSTRAINT_SCHEMA = ku.TABLE_SCHEMA
                            AND rc.TABLE_NAME = ku.TABLE_NAME
                            AND rc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                        WHERE ku.TABLE_NAME = @tableName 
                        AND ku.TABLE_SCHEMA = @schemaName";

                    using (var fkCmd = new MySqlCommand(fkQuery, conn))
                    {
                        fkCmd.Parameters.AddWithValue("@tableName", table.TableName);
                        fkCmd.Parameters.AddWithValue("@schemaName", conn.Database);

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


        private string ParseDataType(string sqlType)
        {
            sqlType = sqlType.ToLower();
            if (sqlType.Contains("int")) return "int";
            if (sqlType.Contains("bigint")) return "long";
            if (sqlType.Contains("decimal") || sqlType.Contains("numeric") || sqlType.Contains("money"))
                return "decimal";
            if (sqlType.Contains("float") || sqlType.Contains("double")) return "double";
            if (sqlType.Contains("datetime")) return "DateTime";
            if (sqlType.Contains("datetimeoffset")) return "DateTimeOffset";
            if (sqlType.Contains("json")) return "string";
            if (sqlType.Contains("xml")) return "string";
            if (sqlType.Contains("bit")) return "bool";
            if (sqlType.Contains("char") || sqlType.Contains("text")) return "string";
            return "string";
        }
    }
}
