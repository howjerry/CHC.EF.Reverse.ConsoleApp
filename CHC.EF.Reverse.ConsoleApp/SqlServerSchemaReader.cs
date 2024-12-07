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

                // 讀取資料表
                var dt = conn.GetSchema("Tables");
                foreach (DataRow row in dt.Rows)
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

                // 讀取欄位定義
                foreach (var table in tables)
                {
                    using (var colCmd = new SqlCommand($@"
                        SELECT 
                            c.name AS ColumnName,
                            t.name AS DataType,
                            c.is_nullable AS IsNullable,
                            c.max_length AS MaxLength,
                            c.precision AS Precision,
                            c.scale AS Scale,
                            CAST(CASE WHEN pk.column_id IS NOT NULL THEN 1 ELSE 0 END AS bit) AS IsPrimaryKey,
                            c.is_identity AS IsIdentity,
                            ep.value AS Comment,
                            COLUMNPROPERTY(c.object_id, c.name, 'IsComputed') AS IsComputed,
                            ISNULL(d.definition, '') AS DefaultValue
                        FROM sys.columns c
                        JOIN sys.types t ON c.user_type_id = t.user_type_id
                        LEFT JOIN sys.extended_properties ep ON ep.major_id = c.object_id AND ep.minor_id = c.column_id
                        LEFT JOIN sys.default_constraints d ON c.default_object_id = d.object_id
                        LEFT JOIN 
                        (
                            SELECT ic.column_id 
                            FROM sys.index_columns ic 
                            JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                            WHERE i.is_primary_key = 1
                        ) pk ON c.column_id = pk.column_id
                        WHERE c.object_id = OBJECT_ID(@tableName)", conn))
                    {
                        colCmd.Parameters.AddWithValue("@tableName", $"{table.SchemaName}.{table.TableName}");

                        using (var rdr = colCmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                table.Columns.Add(new ColumnDefinition
                                {
                                    ColumnName = rdr["ColumnName"].ToString(),
                                    DataType = rdr["DataType"].ToString(),
                                    IsNullable = Convert.ToBoolean(rdr["IsNullable"]),
                                    MaxLength = rdr["MaxLength"] != DBNull.Value ? Convert.ToInt32(rdr["MaxLength"]) : (int?)null,
                                    Precision = rdr["Precision"] != DBNull.Value ? Convert.ToInt32(rdr["Precision"]) : (int?)null,
                                    Scale = rdr["Scale"] != DBNull.Value ? Convert.ToInt32(rdr["Scale"]) : (int?)null,
                                    IsPrimaryKey = Convert.ToBoolean(rdr["IsPrimaryKey"]),
                                    IsIdentity = Convert.ToBoolean(rdr["IsIdentity"]),
                                    IsComputed = Convert.ToBoolean(rdr["IsComputed"]),
                                    DefaultValue = rdr["DefaultValue"].ToString(),
                                    Comment = rdr["Comment"].ToString()
                                });
                            }
                        }
                    }

                    // 讀取外鍵
                    using (var fkCmd = new SqlCommand($@"
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
                        AND SCHEMA_NAME(fk.schema_id) = @schemaName", conn))
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
    }
}
