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

                // 讀取資料表
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

                // 讀取欄位和外鍵
                foreach (var table in tables)
                {
                    ReadColumns(conn, table);
                    ReadForeignKeys(conn, table);
                }
            }

            return tables;
        }

        private void ReadColumns(MySqlConnection conn, TableDefinition table)
        {
            using (var cmd = new MySqlCommand($"SHOW FULL COLUMNS FROM `{table.TableName}`", conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    var column = new ColumnDefinition
                    {
                        ColumnName = rdr["Field"].ToString(),
                        DataType = ParseDataType(rdr["Type"].ToString()),
                        IsNullable = rdr["Null"].ToString().Equals("YES", StringComparison.OrdinalIgnoreCase),
                        IsPrimaryKey = rdr["Key"].ToString().Equals("PRI", StringComparison.OrdinalIgnoreCase),
                        Comment = rdr["Comment"].ToString(),
                        DefaultValue = rdr["Default"].ToString()
                    };

                    // 處理自動遞增
                    column.IsIdentity = rdr["Extra"].ToString().Contains("auto_increment");

                    // 處理欄位長度
                    if (column.DataType == "string" && rdr["Type"].ToString().Contains("("))
                    {
                        var length = rdr["Type"].ToString()
                            .Split('(', ')')[1]
                            .Split(',')[0];

                        if (int.TryParse(length, out var maxLength))
                        {
                            column.MaxLength = maxLength;
                        }
                    }

                    table.Columns.Add(column);
                }
            }
        }

        private void ReadForeignKeys(MySqlConnection conn, TableDefinition table)
        {
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

            using (var cmd = new MySqlCommand(fkQuery, conn))
            {
                cmd.Parameters.AddWithValue("@tableName", table.TableName);
                cmd.Parameters.AddWithValue("@schemaName", conn.Database);

                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        table.ForeignKeys.Add(new ForeignKeyDefinition
                        {
                            ConstraintName = rdr["ConstraintName"].ToString(),
                            ForeignKeyColumn = rdr["ForeignKeyColumn"].ToString(),
                            PrimaryTable = rdr["PrimaryTable"].ToString(),
                            PrimaryKeyColumn = rdr["PrimaryKeyColumn"].ToString(),
                            DeleteRule = rdr["DeleteRule"].ToString(),
                            UpdateRule = rdr["UpdateRule"].ToString()
                        });
                    }
                }
            }
        }

        private string ParseDataType(string sqlType)
        {
            sqlType = sqlType.ToLower();
            if (sqlType.Contains("int")) return "int";
            if (sqlType.Contains("bigint")) return "long";
            if (sqlType.Contains("decimal") || sqlType.Contains("numeric"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(sqlType, @"decimal\((\d+),(\d+)\)");
                if (match.Success)
                {
                    var precision = int.Parse(match.Groups[1].Value);
                    var scale = int.Parse(match.Groups[2].Value);
                    return $"decimal({precision}, {scale})";
                }
                return "decimal";
            }
            if (sqlType.Contains("datetime")) return "DateTime";
            if (sqlType.Contains("json")) return "string";
            if (sqlType.Contains("char") || sqlType.Contains("text")) return "string";
            if (sqlType.Contains("bit")) return "bool";
            return "string";
        }
    }
}
