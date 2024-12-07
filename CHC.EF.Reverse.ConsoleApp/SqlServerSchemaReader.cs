using System;
using System.Collections.Generic;
using System.Data;
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
                    var query = $"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table.TableName}' AND TABLE_SCHEMA = '{table.SchemaName}'";
                    using (var cmd = new SqlCommand(query, conn))
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var column = new ColumnDefinition
                            {
                                ColumnName = rdr["COLUMN_NAME"].ToString(),
                                DataType = rdr["DATA_TYPE"].ToString(),
                                IsNullable = rdr["IS_NULLABLE"].ToString().Equals("YES", StringComparison.OrdinalIgnoreCase),
                                MaxLength = rdr["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value ? Convert.ToInt32(rdr["CHARACTER_MAXIMUM_LENGTH"]) : (int?)null,
                                Precision = rdr["NUMERIC_PRECISION"] != DBNull.Value ? Convert.ToInt32(rdr["NUMERIC_PRECISION"]) : (int?)null,
                                Scale = rdr["NUMERIC_SCALE"] != DBNull.Value ? Convert.ToInt32(rdr["NUMERIC_SCALE"]) : (int?)null
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