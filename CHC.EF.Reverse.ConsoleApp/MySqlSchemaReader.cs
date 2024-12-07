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

                using (var cmd = new MySqlCommand("SHOW FULL TABLES WHERE Table_type='BASE TABLE'", conn))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var table = new TableDefinition
                        {
                            TableName = rdr.GetString(0),
                            SchemaName = conn.Database
                        };
                        tables.Add(table);
                    }
                }

                foreach (var table in tables)
                {
                    using (var colCmd = new MySqlCommand($"SHOW FULL COLUMNS FROM `{table.TableName}`", conn))
                    using (var colRdr = colCmd.ExecuteReader())
                    {
                        while (colRdr.Read())
                        {
                            var column = new ColumnDefinition
                            {
                                ColumnName = colRdr["Field"].ToString(),
                                DataType = colRdr["Type"].ToString(),
                                IsNullable = colRdr["Null"].ToString().Equals("YES", StringComparison.OrdinalIgnoreCase),
                                IsPrimaryKey = colRdr["Key"].ToString().Equals("PRI", StringComparison.OrdinalIgnoreCase),
                                Comment = colRdr["Comment"].ToString()
                            };
                            table.Columns.Add(column);
                        }
                    }
                }
            }

            return tables;
        }
    }
}