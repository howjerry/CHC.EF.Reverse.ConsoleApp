using CHC.EF.Reverse.ConsoleApp;
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
            }
        }

        private void GenerateEntityClass(TableDefinition table)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            if (_settings.UseDataAnnotations)
            {
                sb.AppendLine("using System.ComponentModel.DataAnnotations;");
                sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            }
            sb.AppendLine($"namespace {_settings.Namespace}");
            sb.AppendLine("{");

            if (_settings.IncludeComments && !string.IsNullOrWhiteSpace(table.Comment))
            {
                sb.AppendLine("    /// <summary>");
                AppendXmlComment(sb, table.Comment, "    ");
                sb.AppendLine("    /// </summary>");
            }

            if (_settings.UseDataAnnotations)
            {
                sb.AppendLine($"    [Table(\"{table.TableName}\", Schema = \"{table.SchemaName}\")]");
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

                if (_settings.UseDataAnnotations)
                {
                    if (column.IsPrimaryKey) sb.AppendLine("        [Key]");
                    if (!column.IsNullable) sb.AppendLine("        [Required]");
                    if (column.MaxLength.HasValue) sb.AppendLine($"        [StringLength({column.MaxLength.Value})]");
                }

                var propertyName = ToPascalCase(column.ColumnName);
                var propertyType = GetPropertyType(column);

                sb.AppendLine($"        public {propertyType} {propertyName} {{ get; set; }}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            var filePath = Path.Combine(_settings.OutputDirectory, "Entities", className + ".cs");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, sb.ToString());
            _logger.Info($"Generated entity class: {filePath}");
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
