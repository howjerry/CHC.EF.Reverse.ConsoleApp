// Continuing SqlServerSchemaReader...
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection.PortableExecutable;
using CHC.EF.Reverse.ConsoleApp.Core.Interfaces;
using CHC.EF.Reverse.ConsoleApp.Core.Models;
namespace CHC.EF.Reverse.ConsoleApp.Infrastructure.Database
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

                // 讀取資料表及其描述
                using (var cmd = new MySqlCommand(@"
                SELECT 
                    TABLE_NAME,
                    TABLE_SCHEMA,
                    TABLE_COMMENT
                FROM information_schema.TABLES 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_TYPE = 'BASE TABLE'", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tables.Add(new TableDefinition
                            {
                                TableName = reader["TABLE_NAME"].ToString(),
                                SchemaName = reader["TABLE_SCHEMA"].ToString(),
                                Comment = reader["TABLE_COMMENT"].ToString()
                            });
                        }

                        reader.Close();
                    }
                }

                // 為每個資料表讀取詳細資訊
                foreach (var table in tables)
                {
                    ReadColumns(conn, table);
                    ReadIndexes(conn, table);
                    ReadForeignKeys(conn, table);
                    UpdateOneToOneRelationships(table);
                }
            }

            return tables;
        }

        private void ReadColumns(MySqlConnection conn, TableDefinition table)
        {
            using (var cmd = new MySqlCommand(@"
            SELECT 
                COLUMN_NAME,
                DATA_TYPE,
                IS_NULLABLE,
                CHARACTER_MAXIMUM_LENGTH,
                NUMERIC_PRECISION,
                NUMERIC_SCALE,
                COLUMN_DEFAULT,
                EXTRA,
                COLUMN_COMMENT,
                COLUMN_TYPE,
                COLLATION_NAME
            FROM information_schema.COLUMNS 
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @tableName
            ORDER BY ORDINAL_POSITION", conn))
            {
                cmd.Parameters.AddWithValue("@tableName", table.TableName);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var column = new ColumnDefinition
                        {
                            ColumnName = reader["COLUMN_NAME"].ToString(),
                            DataType = reader["DATA_TYPE"].ToString(),
                            IsNullable = reader["IS_NULLABLE"].ToString() == "YES",
                            Comment = reader["COLUMN_COMMENT"].ToString(),
                            DefaultValue = reader["COLUMN_DEFAULT"].ToString(),
                            CollationType = reader["COLLATION_NAME"].ToString(),
                            IsIdentity = reader["EXTRA"].ToString().Contains("auto_increment"),
                            IsComputed = reader["EXTRA"].ToString().Contains("VIRTUAL") ||
                                       reader["EXTRA"].ToString().Contains("STORED"),
                            GeneratedType = reader["EXTRA"].ToString().Contains("STORED") ? "STORED" :
                                          reader["EXTRA"].ToString().Contains("VIRTUAL") ? "VIRTUAL" : null
                        };

                        // 處理最大長度
                        if (reader["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value)
                        {
                            column.MaxLength = Convert.ToInt64(reader["CHARACTER_MAXIMUM_LENGTH"]);
                        }

                        // 處理數值精確度
                        if (reader["NUMERIC_PRECISION"] != DBNull.Value)
                        {
                            column.Precision = Convert.ToInt32(reader["NUMERIC_PRECISION"]);
                            if (reader["NUMERIC_SCALE"] != DBNull.Value)
                            {
                                column.Scale = Convert.ToInt32(reader["NUMERIC_SCALE"]);
                            }
                        }

                        table.Columns.Add(column);
                    }

                    reader.Close();
                }
            }

            // 讀取主鍵資訊
            using (var cmd = new MySqlCommand(@"
            SELECT COLUMN_NAME
            FROM information_schema.KEY_COLUMN_USAGE
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @tableName
            AND CONSTRAINT_NAME = 'PRIMARY'", conn))
            {
                cmd.Parameters.AddWithValue("@tableName", table.TableName);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var columnName = reader["COLUMN_NAME"].ToString();
                        var column = table.Columns.First(c => c.ColumnName == columnName);
                        column.IsPrimaryKey = true;
                    }

                    reader.Close();
                }
            }
        }

        private void ReadIndexes(MySqlConnection conn, TableDefinition table)
        {
            using (var cmd = new MySqlCommand(@"
            SELECT 
                INDEX_NAME,
                NON_UNIQUE,
                COLUMN_NAME,
                SEQ_IN_INDEX,
                COLLATION AS SORT_DIRECTION
            FROM information_schema.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @tableName
            ORDER BY INDEX_NAME, SEQ_IN_INDEX", conn))
            {
                cmd.Parameters.AddWithValue("@tableName", table.TableName);

                var currentIndexName = string.Empty;
                IndexDefinition currentIndex = null;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var indexName = reader["INDEX_NAME"].ToString();

                        if (indexName != currentIndexName)
                        {
                            currentIndex = new IndexDefinition
                            {
                                IndexName = indexName,
                                IsUnique = !Convert.ToBoolean(reader["NON_UNIQUE"]),
                                IsPrimaryKey = indexName == "PRIMARY",
                                IsDisabled = false,
                                Columns = new List<IndexColumnDefinition>()
                            };

                            table.Indexes.Add(currentIndex);
                            currentIndexName = indexName;
                        }

                        currentIndex.Columns.Add(new IndexColumnDefinition
                        {
                            ColumnName = reader["COLUMN_NAME"].ToString(),
                            IsDescending = reader["SORT_DIRECTION"].ToString() == "D",
                            KeyOrdinal = Convert.ToInt32(reader["SEQ_IN_INDEX"]),
                            IsIncluded = false // MySQL 不支援包含的欄位
                        });
                    }

                    reader.Close();
                }
            }
        }

        private void ReadForeignKeys(MySqlConnection conn, TableDefinition table)
        {
            using (var cmd = new MySqlCommand(@"
            SELECT 
                CONSTRAINT_NAME,
                COLUMN_NAME,
                REFERENCED_TABLE_NAME,
                REFERENCED_COLUMN_NAME
            FROM information_schema.KEY_COLUMN_USAGE
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = @tableName
            AND REFERENCED_TABLE_NAME IS NOT NULL
            ORDER BY CONSTRAINT_NAME, ORDINAL_POSITION", conn))
            {
                cmd.Parameters.AddWithValue("@tableName", table.TableName);

                var currentFkName = string.Empty;
                ForeignKeyDefinition currentFk = null;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var constraintName = reader["CONSTRAINT_NAME"].ToString();

                        if (constraintName != currentFkName)
                        {
                            currentFk = new ForeignKeyDefinition
                            {
                                ConstraintName = constraintName,
                                PrimaryTable = reader["REFERENCED_TABLE_NAME"].ToString(),
                                ColumnPairs = new List<ForeignKeyColumnPair>(),
                                IsEnabled = true
                            };

                            // 讀取刪除和更新規則
                            ReadForeignKeyRules(table.TableName, constraintName, currentFk);

                            table.ForeignKeys.Add(currentFk);
                            currentFkName = constraintName;
                        }

                        currentFk.ColumnPairs.Add(new ForeignKeyColumnPair
                        {
                            ForeignKeyColumn = reader["COLUMN_NAME"].ToString(),
                            PrimaryKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString()
                        });

                        if (currentFk.ColumnPairs.Count == 1)
                        {
                            currentFk.ForeignKeyColumn = reader["COLUMN_NAME"].ToString();
                            currentFk.PrimaryKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                        }

                        currentFk.IsCompositeKey = currentFk.ColumnPairs.Count > 1;
                    }

                    reader.Close();
                }
            }
        }

        private void ReadForeignKeyRules(string tableName, string constraintName, ForeignKeyDefinition fk)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(@"
            SELECT 
                DELETE_RULE,
                UPDATE_RULE
            FROM information_schema.REFERENTIAL_CONSTRAINTS
            WHERE CONSTRAINT_SCHEMA = DATABASE()
            AND TABLE_NAME = @tableName
            AND CONSTRAINT_NAME = @constraintName", conn))
                {
                    cmd.Parameters.AddWithValue("@tableName", tableName);
                    cmd.Parameters.AddWithValue("@constraintName", constraintName);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            fk.DeleteRule = reader["DELETE_RULE"].ToString();
                            fk.UpdateRule = reader["UPDATE_RULE"].ToString();
                        }

                        reader.Close();
                    }
                }
            }
        }

        private void UpdateOneToOneRelationships(TableDefinition table)
        {
            foreach (var fk in table.ForeignKeys)
            {
                // 檢查是否存在唯一索引只包含這個外鍵列
                var hasUniqueConstraint = table.Indexes
                    .Where(idx => idx.IsUnique && !idx.IsPrimaryKey)
                    .Any(idx => idx.Columns.Count == 1 &&
                               idx.Columns[0].ColumnName == fk.ForeignKeyColumn);

                if (hasUniqueConstraint)
                {
                    fk.Comment = (fk.Comment ?? "") + " [One-to-One Relationship]";
                }
            }
        }
    }
}