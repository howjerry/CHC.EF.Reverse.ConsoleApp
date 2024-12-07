using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CHC.EF.Reverse.ConsoleApp
{
    public class EntityGenerator
    {
        private readonly Settings _settings;
        private readonly Logger _logger;

        public EntityGenerator(Settings settings, Logger logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public async Task GenerateAsync(List<TableDefinition> tables)
        {
            // 檢查並建立輸出目錄
            var entityOutputDir = Path.Combine(_settings.OutputDirectory, "Entities");
            var configOutputDir = Path.Combine(_settings.OutputDirectory, "Configurations");

            Directory.CreateDirectory(entityOutputDir);
            Directory.CreateDirectory(configOutputDir);

            // 並行生成代碼
            var tasks = tables.Select(table => Task.WhenAll(
                GenerateEntityClassAsync(table, entityOutputDir),
                GenerateConfigurationClassAsync(table, configOutputDir)
            ));
            await Task.WhenAll(tasks);

            _logger.Info("Code generation completed successfully.");
        }
        /// <summary>
        /// 生成實體類別程式碼。
        /// </summary>
        /// <param name="table">資料表定義。</param>
        /// <param name="outputDir">輸出目錄。</param>
        private async Task GenerateEntityClassAsync(TableDefinition table, string outputDir)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            if (_settings.UseDataAnnotations)
            {
                sb.AppendLine("using System.ComponentModel.DataAnnotations;");
                sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            }
            sb.AppendLine($"namespace {_settings.Namespace}.Entities");
            sb.AppendLine("{");

            var className = ToPascalCase(table.TableName);
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");

            foreach (var column in table.Columns)
            {
                // 屬性註解
                if (_settings.IncludeComments && !string.IsNullOrWhiteSpace(column.Comment))
                {
                    sb.AppendLine("        /// <summary>");
                    AppendXmlComment(sb, column.Comment, "        ");
                    sb.AppendLine("        /// </summary>");
                }

                // 資料註解特性
                if (_settings.UseDataAnnotations)
                {
                    if (column.IsPrimaryKey) sb.AppendLine("        [Key]");
                    if (!column.IsNullable) sb.AppendLine("        [Required]");
                    if (column.MaxLength.HasValue) sb.AppendLine($"        [MaxLength({column.MaxLength.Value})]");
                   // if (column.IsIndexed) sb.AppendLine("        [Index]");
                }

                var propertyType = GetPropertyType(column);
                sb.AppendLine($"        public {propertyType} {ToPascalCase(column.ColumnName)} {{ get; set; }}");
            }

            // 外鍵導航屬性
            foreach (var fk in table.ForeignKeys)
            {
                var navigationProperty = ToPascalCase(fk.PrimaryTable);
                sb.AppendLine($"        public virtual {navigationProperty} {navigationProperty} {{ get; set; }}");
            }

            // 多對多導航屬性
            if (table.IsManyToMany)
            {
                foreach (var fk in table.ForeignKeys)
                {
                    var otherTable = fk.PrimaryTable;
                    if (!string.Equals(otherTable, table.TableName, StringComparison.OrdinalIgnoreCase))
                    {
                        var collectionName = Pluralize(ToPascalCase(otherTable));
                        sb.AppendLine($"        public virtual ICollection<{ToPascalCase(otherTable)}> {collectionName} {{ get; set; }}");
                    }
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            var filePath = Path.Combine(outputDir, $"{className}.cs");
            await File.WriteAllTextAsync(filePath, sb.ToString());
            _logger.Info($"Generated entity class: {filePath}");
        }
        /// <summary>
        /// 生成 Entity Framework Configuration 類別程式碼。
        /// </summary>
        /// <param name="table">資料表定義。</param>
        /// <param name="outputDir">輸出目錄。</param>
        private async Task GenerateConfigurationClassAsync(TableDefinition table, string outputDir)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System.Data.Entity.ModelConfiguration;");
            sb.AppendLine($"namespace {_settings.Namespace}.Configurations");
            sb.AppendLine("{");

            var className = ToPascalCase(table.TableName);
            sb.AppendLine($"    public class {className}Configuration : EntityTypeConfiguration<{_settings.Namespace}.Entities.{className}>");
            sb.AppendLine("    {");
            sb.AppendLine($"        public {className}Configuration()");
            sb.AppendLine("        {");

            sb.AppendLine($"            ToTable(\"{table.TableName}\", \"{table.SchemaName}\");");

            ConfigurePrimaryKeys(sb, table);
            ConfigureColumns(sb, table);
            ConfigureRelationships(sb, table);

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            var filePath = Path.Combine(outputDir, $"{className}Configuration.cs");
            await File.WriteAllTextAsync(filePath, sb.ToString());
            _logger.Info($"Generated configuration class: {filePath}");
        }

        private void ConfigureRelationships(StringBuilder sb, TableDefinition table)
        {
            foreach (var fk in table.ForeignKeys)
            {
                var foreignKeyProperty = ToPascalCase(fk.ForeignKeyColumn);
                var navigationProperty = ToPascalCase(fk.PrimaryTable);
                var inverseNavigationProperty = Pluralize(ToPascalCase(table.TableName));

                // 決定關係類型
                var isRequired = table.Columns
                    .First(c => c.ColumnName == fk.ForeignKeyColumn)
                    .IsNullable == false;

                sb.AppendLine();
                if (isRequired)
                {
                    sb.AppendLine($"            HasRequired(x => x.{navigationProperty})");
                }
                else
                {
                    sb.AppendLine($"            HasOptional(x => x.{navigationProperty})");
                }

                // 配置反向導航屬性
                if (table.IsManyToMany)
                {
                    // 多對多關係
                    sb.AppendLine($"                .WithMany(x => x.{inverseNavigationProperty})");
                }
                else if (IsCollectionNavigation(fk, table))
                {
                    // 一對多關係
                    sb.AppendLine($"                .WithMany(x => x.{inverseNavigationProperty})");
                }
                else
                {
                    // 一對一關係
                    sb.AppendLine($"                .WithOptional(x => x.{ToPascalCase(table.TableName)})");
                }

                // 配置外鍵
                sb.AppendLine($"                .HasForeignKey(x => x.{foreignKeyProperty})");

                // 配置刪除行為
                ConfigureDeleteBehavior(sb, fk);

                sb.AppendLine("                ;");
            }
        }

        private bool IsCollectionNavigation(ForeignKeyDefinition fk, TableDefinition table)
        {
            // 檢查是否有多個相同的外鍵指向同一個主表
            return table.ForeignKeys.Count(x => x.PrimaryTable == fk.PrimaryTable) > 1;
        }

        private void ConfigureDeleteBehavior(StringBuilder sb, ForeignKeyDefinition fk)
        {
            switch (fk.DeleteRule?.ToUpper())
            {
                case "CASCADE":
                    sb.AppendLine("                .WillCascadeOnDelete(true)");
                    break;
                case "NO ACTION":
                case "RESTRICT":
                    sb.AppendLine("                .WillCascadeOnDelete(false)");
                    break;
                case "SET NULL":
                    // Entity Framework 不直接支援 SET NULL，需要在應用程式層面處理
                    sb.AppendLine("                .WillCascadeOnDelete(false)");
                    break;
            }
        }

        private void ConfigurePrimaryKeys(StringBuilder sb, TableDefinition table)
        {
            var primaryKeys = table.Columns.Where(c => c.IsPrimaryKey).ToList();
            if (primaryKeys.Count > 0)
            {
                var pkProps = string.Join(", ", primaryKeys.Select(pk => $"x.{ToPascalCase(pk.ColumnName)}"));
                if (primaryKeys.Count == 1)
                {
                    sb.AppendLine($"            HasKey(x => x.{pkProps});");
                }
                else
                {
                    sb.AppendLine($"            HasKey(x => new {{ {pkProps} }});");
                }
            }
        }

        private void ConfigureColumns(StringBuilder sb, TableDefinition table)
        {
            foreach (var column in table.Columns)
            {
                sb.AppendLine($"            Property(x => x.{ToPascalCase(column.ColumnName)})");
                sb.AppendLine($"                .HasColumnName(\"{column.ColumnName}\")");

                if (column.MaxLength.HasValue && column.DataType == "string")
                {
                    sb.AppendLine($"                .HasMaxLength({column.MaxLength.Value})");
                }

                if (!column.IsNullable)
                {
                    sb.AppendLine("                .IsRequired()");
                }

                if (column.IsIdentity)
                {
                    sb.AppendLine("                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)");
                }
                else if (column.IsComputed)
                {
                    sb.AppendLine("                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed)");
                }

                sb.AppendLine("                ;");
            }
        }


        private string ToPascalCase(string text)
        {
            return Regex.Replace(text, "(^|_)([a-z])", m => m.Groups[2].Value.ToUpper());
        }

        private string Pluralize(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase) && !IsVowel(name[name.Length - 2]))
            {
                return name.Substring(0, name.Length - 1) + "ies";
            }
            if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase)) return name;
            return name + "s";
        }

        private bool IsVowel(char c) => "aeiouAEIOU".IndexOf(c) >= 0;

        private string GetPropertyType(ColumnDefinition column)
        {
            return column.DataType switch
            {
                "int" => "int",
                "bigint" => "long",
                "decimal" => "decimal",
                "money" => "decimal",
                "float" => "double",
                "datetime" => "DateTime",
                "datetimeoffset" => "DateTimeOffset",
                "bit" => "bool",
                "xml" => "string",
                "json" => "string",
                _ => "string"
            };
        }

        private void AppendXmlComment(StringBuilder sb, string comment, string indent)
        {
            var lines = comment.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                sb.AppendLine($"{indent}/// {SecurityElement.Escape(line.Trim())}");
            }
        }
    }
}
