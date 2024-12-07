using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

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

        public void Generate(List<TableDefinition> tables)
        {
            // 檢查並建立輸出目錄
            var entityOutputDir = Path.Combine(_settings.OutputDirectory, "Entities");
            var configOutputDir = Path.Combine(_settings.OutputDirectory, "Configurations");

            Directory.CreateDirectory(entityOutputDir);
            Directory.CreateDirectory(configOutputDir);

            // 開始生成代碼
            foreach (var table in tables)
            {
                // 生成實體類
                GenerateEntityClass(table, entityOutputDir);

                // 生成 Fluent API 配置類
                GenerateConfigurationClass(table, configOutputDir);
            }

            _logger.Info("Code generation completed successfully.");
        }


        private void GenerateEntityClass(TableDefinition table, string outputDir)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine($"namespace {_settings.Namespace}.Entities");
            sb.AppendLine("{");

            var className = ToPascalCase(table.TableName);
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");

            // 屬性生成
            foreach (var column in table.Columns)
            {
                var propertyType = GetPropertyType(column);
                sb.AppendLine($"        public {propertyType} {ToPascalCase(column.ColumnName)} {{ get; set; }}");
            }

            // 外鍵導航屬性
            foreach (var fk in table.ForeignKeys)
            {
                var navigationProperty = ToPascalCase(fk.PrimaryTable);
                sb.AppendLine($"        public {navigationProperty} {navigationProperty} {{ get; set; }}");
            }

            // 多對多關係導航屬性
            if (table.IsManyToMany)
            {
                foreach (var fk in table.ForeignKeys)
                {
                    var otherTable = fk.PrimaryTable;
                    if (!string.Equals(otherTable, table.TableName, StringComparison.OrdinalIgnoreCase))
                    {
                        var collectionName = Pluralize(ToPascalCase(otherTable));
                        sb.AppendLine($"        public ICollection<{ToPascalCase(otherTable)}> {collectionName} {{ get; set; }}");
                    }
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            var filePath = Path.Combine(outputDir, $"{className}.cs");
            File.WriteAllText(filePath, sb.ToString());
            _logger.Info($"Generated entity class: {filePath}");
        }

        private void GenerateConfigurationClass(TableDefinition table, string outputDir)
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

            // 主鍵
            var primaryKeys = table.Columns.Where(c => c.IsPrimaryKey).ToList();
            if (primaryKeys.Count > 0)
            {
                var pkProps = string.Join(", ", primaryKeys.Select(pk => $"x.{ToPascalCase(pk.ColumnName)}"));
                sb.AppendLine($"            HasKey(x => new {{ {pkProps} }});");
            }

            // 多對多 Fluent API 映射
            if (table.IsManyToMany)
            {
                var fk1 = table.ForeignKeys[0];
                var fk2 = table.ForeignKeys[1];

                var table1 = ToPascalCase(fk1.PrimaryTable);
                var table2 = ToPascalCase(fk2.PrimaryTable);

                sb.AppendLine($"            HasMany(x => x.{Pluralize(table1)})");
                sb.AppendLine($"                .WithMany(x => x.{Pluralize(table2)})");
                sb.AppendLine($"                .Map(m =>");
                sb.AppendLine($"                {{");
                sb.AppendLine($"                    m.ToTable(\"{table.TableName}\");");
                sb.AppendLine($"                    m.MapLeftKey(\"{fk1.ForeignKeyColumn}\");");
                sb.AppendLine($"                    m.MapRightKey(\"{fk2.ForeignKeyColumn}\");");
                sb.AppendLine($"                }});");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            var filePath = Path.Combine(outputDir, $"{className}Configuration.cs");
            File.WriteAllText(filePath, sb.ToString());
            _logger.Info($"Generated configuration class: {filePath}");
        }

        private string ToPascalCase(string text)
        {
            return Regex.Replace(text, "(^|_)([a-z])", m => m.Groups[2].Value.ToUpper());
        }

        private string GetPropertyType(ColumnDefinition column)
        {
            var type = column.DataType switch
            {
                "int" => "int",
                "bigint" => "long",
                "nvarchar" => "string",
                "varchar" => "string",
                "datetime" => "DateTime",
                "bit" => "bool",
                _ => "string"
            };

            if (column.IsNullable && type != "string")
            {
                type += "?";
            }

            return type;
        }

        private void AppendXmlComment(StringBuilder sb, string comment, string indent)
        {
            var lines = comment.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                sb.AppendLine($"{indent}/// {SecurityElement.Escape(line.Trim())}");
            }
        }

        private string Pluralize(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            // 簡單的規則判斷
            if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase) &&
                name.Length > 1 &&
                !IsVowel(name[name.Length - 2]))
            {
                // 以非元音+y結尾，改為ies
                return name.Substring(0, name.Length - 1) + "ies";
            }
            else if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
                     name.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
                     name.EndsWith("z", StringComparison.OrdinalIgnoreCase) ||
                     name.EndsWith("sh", StringComparison.OrdinalIgnoreCase) ||
                     name.EndsWith("ch", StringComparison.OrdinalIgnoreCase))
            {
                // 特殊結尾，直接加es
                return name + "es";
            }
            else
            {
                // 一般情況，加s
                return name + "s";
            }
        }

        private bool IsVowel(char c)
        {
            return "aeiouAEIOU".IndexOf(c) >= 0;
        }

    }
}
