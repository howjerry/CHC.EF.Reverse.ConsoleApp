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
            Directory.CreateDirectory(_settings.OutputDirectory);

            foreach (var table in tables)
            {
                GenerateEntityClass(table);
                GenerateConfigurationClass(table);
            }
        }

        private void GenerateEntityClass(TableDefinition table)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine($"namespace {_settings.Namespace}.Entities");
            sb.AppendLine("{");

            if (_settings.IncludeComments && !string.IsNullOrWhiteSpace(table.Comment))
            {
                sb.AppendLine("    /// <summary>");
                AppendXmlComment(sb, table.Comment, "    ");
                sb.AppendLine("    /// </summary>");
            }

            var className = ToPascalCase(table.TableName);
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");

            foreach (var column in table.Columns)
            {
                if (_settings.IncludeComments && !string.IsNullOrWhiteSpace(column.Comment))
                {
                    sb.AppendLine("        /// <summary>");
                    AppendXmlComment(sb, column.Comment, "        ");
                    sb.AppendLine("        /// </summary>");
                }

                var propertyName = ToPascalCase(column.ColumnName);
                var propertyType = GetPropertyType(column);

                sb.AppendLine($"        public {propertyType} {propertyName} {{ get; set; }}");
            }

            // Add navigation properties for foreign keys
            foreach (var fk in table.ForeignKeys)
            {
                var navigationProperty = ToPascalCase(fk.PrimaryTable);
                sb.AppendLine($"        public {navigationProperty} {navigationProperty} {{ get; set; }}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            var filePath = Path.Combine(_settings.OutputDirectory, "Entities", className + ".cs");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, sb.ToString());
            _logger.Info($"Generated entity class: {filePath}");
        }

        private void GenerateConfigurationClass(TableDefinition table)
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

            // Map table
            sb.AppendLine($"            ToTable(\"{table.TableName}\", \"{table.SchemaName}\");");

            // Map primary keys
            var primaryKeys = table.Columns.Where(c => c.IsPrimaryKey).ToList();
            if (primaryKeys.Count == 1)
            {
                sb.AppendLine($"            HasKey(x => x.{ToPascalCase(primaryKeys[0].ColumnName)});");
            }
            else if (primaryKeys.Count > 1)
            {
                var pkProps = string.Join(", ", primaryKeys.Select(pk => $"x.{ToPascalCase(pk.ColumnName)}"));
                sb.AppendLine($"            HasKey(x => new {{ {pkProps} }});");
            }

            // Map properties
            foreach (var column in table.Columns)
            {
                var propertyName = ToPascalCase(column.ColumnName);
                sb.AppendLine($"            Property(x => x.{propertyName})");

                sb.AppendLine($"                .HasColumnName(\"{column.ColumnName}\")");

                if (column.MaxLength.HasValue && column.DataType == "string")
                {
                    sb.AppendLine($"                .HasMaxLength({column.MaxLength.Value})");
                }

                if (!column.IsNullable)
                {
                    sb.AppendLine("                .IsRequired()");
                }

                sb.AppendLine("                ;");
            }

            // Map foreign keys
            foreach (var fk in table.ForeignKeys)
            {
                var foreignKeyProperty = ToPascalCase(fk.ForeignKeyColumn);
                var navigationProperty = ToPascalCase(fk.PrimaryTable);

                sb.AppendLine($"            HasRequired(x => x.{navigationProperty})");
                sb.AppendLine($"                .WithMany() // Update as needed for navigation");
                sb.AppendLine($"                .HasForeignKey(x => x.{foreignKeyProperty});");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            var filePath = Path.Combine(_settings.OutputDirectory, "Configurations", className + "Configuration.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
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
    }
}
