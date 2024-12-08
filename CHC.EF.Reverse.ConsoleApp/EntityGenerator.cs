﻿using System;
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
        private readonly ILogger _logger;
        private readonly List<TableDefinition> _tables;

        public EntityGenerator(Settings settings, ILogger logger, List<TableDefinition> tables)
        {
            _settings = settings;
            _logger = logger;
            _tables = tables;
        }

        public async Task GenerateAsync(List<TableDefinition> tables)
        {
            try
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
            catch (Exception ex)
            {
                _logger.Error("Error during code generation", ex);
                throw;
            }
        }

        private async Task GenerateConfigurationClassAsync(TableDefinition table, string outputDir)
        {
            var sb = new StringBuilder();
            var className = ToPascalCase(table.TableName);

            sb.AppendLine("using System.Data.Entity.ModelConfiguration;");
            sb.AppendLine($"namespace {_settings.Namespace}.Configurations");
            sb.AppendLine("{");

            // Configuration class declaration
            sb.AppendLine($"    public class {className}Configuration : EntityTypeConfiguration<{_settings.Namespace}.Entities.{className}>");
            sb.AppendLine("    {");
            sb.AppendLine($"        public {className}Configuration()");
            sb.AppendLine("        {");

            // Table mapping
            sb.AppendLine($"            ToTable(\"{table.TableName}\", \"{table.SchemaName}\");");
            sb.AppendLine();

            // Primary Key Configuration
            ConfigurePrimaryKeys(sb, table);

            // Property Configurations
            ConfigureColumns(sb, table);

            // Relationship Configurations
            ConfigureRelationships(sb, table);

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            var filePath = Path.Combine(outputDir, $"{className}Configuration.cs");
            await File.WriteAllTextAsync(filePath, sb.ToString());
            _logger.Info($"Generated configuration class: {filePath}");
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
                sb.AppendLine();
            }
        }

        private void ConfigurePrimaryKeys(StringBuilder sb, TableDefinition table)
        {
            var primaryKeys = table.Columns.Where(c => c.IsPrimaryKey).ToList();
            if (primaryKeys.Any())
            {
                if (primaryKeys.Count() == 1)
                {
                    sb.AppendLine($"            HasKey(x => x.{ToPascalCase(primaryKeys[0].ColumnName)});");
                }
                else
                {
                    var pkProps = string.Join(", ", primaryKeys.Select(pk => $"x.{ToPascalCase(pk.ColumnName)}"));
                    sb.AppendLine($"            HasKey(x => new {{ {pkProps} }});");
                }
                sb.AppendLine();
            }
        }

        private void ConfigureRelationships(StringBuilder sb, TableDefinition table)
        {
            foreach (var fk in table.ForeignKeys)
            {
                var navigationProperty = ToPascalCase(fk.PrimaryTable);
                var foreignKeyProperty = ToPascalCase(fk.ForeignKeyColumn);

                // One-to-One relationship
                if (table.IsOneToOne(fk.ForeignKeyColumn))
                {
                    sb.AppendLine($"            HasRequired(x => x.{navigationProperty})");
                    sb.AppendLine($"                .WithOptional(x => x.{ToPascalCase(table.TableName)})");
                }
                // One-to-Many relationship
                else
                {
                    var isRequired = !table.Columns.First(c => c.ColumnName == fk.ForeignKeyColumn).IsNullable;
                    var inverseCollection = Pluralize(ToPascalCase(table.TableName));

                    if (isRequired)
                    {
                        sb.AppendLine($"            HasRequired(x => x.{navigationProperty})");
                    }
                    else
                    {
                        sb.AppendLine($"            HasOptional(x => x.{navigationProperty})");
                    }

                    sb.AppendLine($"                .WithMany(x => x.{inverseCollection})");
                    sb.AppendLine($"                .HasForeignKey(x => x.{foreignKeyProperty})");

                    if (fk.DeleteRule?.ToUpper() == "CASCADE")
                    {
                        sb.AppendLine("                .WillCascadeOnDelete(true)");
                    }
                    else
                    {
                        sb.AppendLine("                .WillCascadeOnDelete(false)");
                    }
                }
                sb.AppendLine("                ;");
                sb.AppendLine();
            }

            // Many-to-Many relationships
            if (!table.IsManyToMany)
            {
                var manyToManyRelationships = GetManyToManyRelationships(table);
                foreach (var rel in manyToManyRelationships)
                {
                    var otherEntity = ToPascalCase(rel.RelatedTable);
                    var thisCollection = Pluralize(otherEntity);
                    var otherCollection = Pluralize(ToPascalCase(table.TableName));

                    sb.AppendLine($"            HasMany(x => x.{thisCollection})");
                    sb.AppendLine($"                .WithMany(x => x.{otherCollection})");
                    sb.AppendLine($"                .Map(m =>");
                    sb.AppendLine($"                {{");
                    sb.AppendLine($"                    m.ToTable(\"{rel.JunctionTable}\");");
                    sb.AppendLine($"                    m.MapLeftKey(\"{table.TableName}Id\");");
                    sb.AppendLine($"                    m.MapRightKey(\"{rel.RelatedTable}Id\");");
                    sb.AppendLine($"                }});");
                    sb.AppendLine();
                }
            }
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
                    sb.AppendLine("                .WillCascadeOnDelete(false)");
                    break;
            }
        }

        private async Task GenerateEntityClassAsync(TableDefinition table, string outputDir)
        {
            var sb = new StringBuilder();
            var className = ToPascalCase(table.TableName);
            var hasCollectionProperties = false;

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");

            if (_settings.UseDataAnnotations)
            {
                sb.AppendLine("using System.ComponentModel.DataAnnotations;");
                sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            }

            sb.AppendLine();
            sb.AppendLine($"namespace {_settings.Namespace}.Entities");
            sb.AppendLine("{");

            // Add table comment if exists
            if (!string.IsNullOrEmpty(table.Comment) && _settings.IncludeComments)
            {
                sb.AppendLine("    /// <summary>");
                AppendXmlComment(sb, table.Comment, "    ");
                sb.AppendLine("    /// </summary>");
            }

            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");

            // Properties
            foreach (var column in table.Columns)
            {
                if (_settings.IncludeComments && !string.IsNullOrEmpty(column.Comment))
                {
                    sb.AppendLine("        /// <summary>");
                    AppendXmlComment(sb, column.Comment, "        ");
                    sb.AppendLine("        /// </summary>");
                }

                if (_settings.UseDataAnnotations)
                {
                    if (column.IsPrimaryKey)
                        sb.AppendLine("        [Key]");

                    if (!column.IsNullable)
                        sb.AppendLine("        [Required]");

                    if (column.MaxLength.HasValue && column.DataType.ToLower() == "string")
                        sb.AppendLine($"        [StringLength({column.MaxLength.Value})]");

                    if (column.DataType.ToLower() == "decimal" || column.DataType.ToLower() == "numeric")
                        sb.AppendLine($"        [Column(TypeName = \"decimal({column.Precision},{column.Scale})\")]");

                    if (column.ColumnName.ToLower().Contains("email"))
                        sb.AppendLine("        [EmailAddress]");
                }

                var propertyType = GetPropertyType(column);
                if (column.IsNullable && IsValueType(propertyType))
                {
                    propertyType += "?";
                }

                sb.AppendLine($"        public {propertyType} {ToPascalCase(column.ColumnName)} {{ get; set; }}");
                sb.AppendLine();
            }

            // Navigation Properties
            if (_settings.IncludeForeignKeys)
            {
                // One-to-One and One-to-Many navigation properties
                foreach (var fk in table.ForeignKeys)
                {
                    var refTableName = ToPascalCase(fk.PrimaryTable);
                    sb.AppendLine($"        public virtual {refTableName} {refTableName} {{ get; set; }}");
                    sb.AppendLine();
                }

                // Inverse navigation properties for tables referencing this one
                foreach (var referencingTable in _tables.Where(t => !t.IsManyToMany &&
                    t.ForeignKeys.Any(fk => fk.PrimaryTable == table.TableName)))
                {
                    var referencingClassName = ToPascalCase(referencingTable.TableName);
                    var collectionName = Pluralize(referencingClassName);

                    sb.AppendLine($"        public virtual ICollection<{referencingClassName}> {collectionName} {{ get; set; }}");
                    sb.AppendLine();
                }

                // Many-to-Many collection properties
                if (!table.IsManyToMany)
                {
                    var manyToManyRelationships = GetManyToManyRelationships(table);
                    foreach (var rel in manyToManyRelationships)
                    {
                        var relatedEntity = ToPascalCase(rel.RelatedTable);
                        sb.AppendLine($"        public virtual ICollection<{relatedEntity}> {Pluralize(relatedEntity)} {{ get; set; }}");
                        sb.AppendLine();
                    }
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            var filePath = Path.Combine(outputDir, $"{className}.cs");
            await File.WriteAllTextAsync(filePath, sb.ToString());
            _logger.Info($"Generated entity class: {filePath}");
        }

        private List<ManyToManyRelationship> GetManyToManyRelationships(TableDefinition table)
        {
            var relationships = new List<ManyToManyRelationship>();

            foreach (var junctionTable in _tables.Where(t => t.IsManyToMany))
            {
                var fks = junctionTable.ForeignKeys;
                if (fks.Count != 2) continue;

                if (fks[0].PrimaryTable == table.TableName)
                {
                    relationships.Add(new ManyToManyRelationship
                    {
                        JunctionTable = junctionTable.TableName,
                        RelatedTable = fks[1].PrimaryTable
                    });
                }
                else if (fks[1].PrimaryTable == table.TableName)
                {
                    relationships.Add(new ManyToManyRelationship
                    {
                        JunctionTable = junctionTable.TableName,
                        RelatedTable = fks[0].PrimaryTable
                    });
                }
            }
            return relationships;
        }

        private string ToPascalCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return Regex.Replace(text, "(^|_)([a-z])", m => m.Groups[2].Value.ToUpper());
        }

        private bool IsCollectionNavigation(ForeignKeyDefinition fk, TableDefinition table)
        {
            return table.ForeignKeys.Count(x => x.PrimaryTable == fk.PrimaryTable) > 1;
        }

        private string Pluralize(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase) && !IsVowel(name[name.Length - 2]))
            {
                return name.Substring(0, name.Length - 1) + "ies";
            }
            if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("sh", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("z", StringComparison.OrdinalIgnoreCase))
            {
                return name + "es";
            }
            return name + "s";
        }

        private bool IsVowel(char c)
        {
            return "aeiouAEIOU".IndexOf(c) >= 0;
        }

        private string GetPropertyType(ColumnDefinition column)
        {
            return column.DataType.ToLower() switch
            {
                "datetime" or "datetime2" or "smalldatetime" => "DateTime",
                "datetimeoffset" => "DateTimeOffset",
                "date" => "DateTime",
                "time" => "TimeSpan",
                "int" => "int",
                "bigint" => "long",
                "smallint" => "short",
                "tinyint" => "byte",
                "bit" => "bool",
                "decimal" or "money" or "smallmoney" => "decimal",
                "float" => "double",
                "real" => "float",
                "uniqueidentifier" => "Guid",
                _ => "string"
            };
        }

        private bool IsValueType(string typeName)
        {
            return typeName switch
            {
                "int" or "long" or "short" or "byte" or "bool" or "decimal"
                or "float" or "double" or "DateTime" or "DateTimeOffset" or "Guid" => true,
                _ => false
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
