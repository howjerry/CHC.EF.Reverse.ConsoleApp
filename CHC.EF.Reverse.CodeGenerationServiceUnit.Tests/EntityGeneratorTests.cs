using Xunit;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Moq;
using System.Text;

namespace CHC.EF.Reverse.ConsoleApp.Tests
{
    public class EntityGeneratorTests
    {
        private readonly Settings _settings;
        private readonly Mock<ILogger> _mockLogger;
        private readonly string _testOutputPath;
        private readonly List<TableDefinition> _testTables;

        public EntityGeneratorTests()
        {
            _testOutputPath = Path.Combine(Path.GetTempPath(), "EntityGenTests");
            _testTables = new List<TableDefinition>();

            _settings = new Settings
            {
                OutputDirectory = _testOutputPath,
                Namespace = "TestNamespace",
                UseDataAnnotations = true,
                IncludeComments = true
            };

            _mockLogger = new Mock<ILogger>();

            Directory.CreateDirectory(_testOutputPath);
            Directory.CreateDirectory(Path.Combine(_testOutputPath, "Entities"));
            Directory.CreateDirectory(Path.Combine(_testOutputPath, "Configurations"));
        }

        [Fact]
        public async Task GenerateAsync_WithValidTable_GeneratesEntityAndConfiguration()
        {
            var table = new TableDefinition
            {
                TableName = "Product",
                SchemaName = "dbo",
                Comment = "產品資料表",
                Columns = new List<ColumnDefinition>
           {
               new ColumnDefinition
               {
                   ColumnName = "ProductId",
                   DataType = "int",
                   IsPrimaryKey = true,
                   IsIdentity = true,
                   Comment = "產品編號"
               },
               new ColumnDefinition
               {
                   ColumnName = "ProductName",
                   DataType = "string",
                   MaxLength = 200,
                   IsNullable = false,
                   Comment = "產品名稱"
               }
           }
            };
            _testTables.Add(table);

            var generator = new EntityGenerator(_settings, _mockLogger.Object, _testTables);

            await generator.GenerateAsync(_testTables);

            var configPath = Path.Combine(_testOutputPath, "Configurations", "ProductConfiguration.cs");
            Assert.True(File.Exists(configPath));
            var configContent = await File.ReadAllTextAsync(configPath);

            _mockLogger.Object.Info($"實際生成的設定檔內容: \n{configContent}");

            Assert.Contains("using System.Data.Entity.ModelConfiguration;", configContent);
            Assert.Contains("namespace TestNamespace.Configurations", configContent);
            Assert.Contains("public class ProductConfiguration : EntityTypeConfiguration<TestNamespace.Entities.Product>", configContent);
            Assert.Contains("ToTable(\"Product\", \"dbo\")", configContent);
            Assert.Contains("Property(x => x.ProductId)", configContent);
            Assert.Contains("HasKey(x => x.ProductId)", configContent);
            Assert.Contains("Property(x => x.ProductName)", configContent);
            Assert.Contains("HasMaxLength(200)", configContent);
            Assert.Contains("IsRequired()", configContent);
        }

        [Fact]
        public async Task GenerateAsync_WithForeignKey_GeneratesNavigationProperties()
        {
            var table = new TableDefinition
            {
                TableName = "Order",
                SchemaName = "dbo",
                Columns = new List<ColumnDefinition>
           {
               new ColumnDefinition
               {
                   ColumnName = "OrderId",
                   DataType = "int",
                   IsPrimaryKey = true
               },
               new ColumnDefinition
               {
                   ColumnName = "CustomerId",
                   DataType = "int",
                   IsNullable = false
               }
           },
                ForeignKeys = new List<ForeignKeyDefinition>
           {
               new ForeignKeyDefinition
               {
                   ForeignKeyColumn = "CustomerId",
                   PrimaryTable = "Customer",
                   PrimaryKeyColumn = "CustomerId",
                   DeleteRule = "CASCADE"
               }
           }
            };
            _testTables.Add(table);

            var generator = new EntityGenerator(_settings, _mockLogger.Object, _testTables);

            await generator.GenerateAsync(_testTables);

            var entityPath = Path.Combine(_testOutputPath, "Entities", "Order.cs");
            Assert.True(File.Exists(entityPath));
            var entityContent = await File.ReadAllTextAsync(entityPath);
            Assert.Contains("public virtual Customer Customer { get; set; }", entityContent);
        }

        [Fact]
        public async Task GenerateAsync_OneToOne_GeneratesNavigationPropertiesAndConfiguration()
        {
            var studentTable = new TableDefinition
            {
                TableName = "Student",
                SchemaName = "dbo",
                Columns = new List<ColumnDefinition>
       {
           new ColumnDefinition { ColumnName = "StudentId", DataType = "int", IsPrimaryKey = true },
           new ColumnDefinition { ColumnName = "Name", DataType = "string" }
       }
            };

            var profileTable = new TableDefinition
            {
                TableName = "StudentProfile",
                SchemaName = "dbo",
                Columns = new List<ColumnDefinition>
       {
           new ColumnDefinition { ColumnName = "ProfileId", DataType = "int", IsPrimaryKey = true },
           new ColumnDefinition { ColumnName = "StudentId", DataType = "int", IsNullable = false }
       },
                ForeignKeys = new List<ForeignKeyDefinition>
       {
           new ForeignKeyDefinition
           {
               ForeignKeyColumn = "StudentId",
               PrimaryTable = "Student",
               PrimaryKeyColumn = "StudentId"
           }
       }
            };

            _testTables.AddRange(new[] { studentTable, profileTable });
            var generator = new EntityGenerator(_settings, _mockLogger.Object, _testTables);
            await generator.GenerateAsync(_testTables);

            var studentPath = Path.Combine(_testOutputPath, "Entities", "Student.cs");
            var profilePath = Path.Combine(_testOutputPath, "Entities", "StudentProfile.cs");

            Assert.True(File.Exists(studentPath));
            Assert.True(File.Exists(profilePath));

            var studentContent = await File.ReadAllTextAsync(studentPath);
            var profileContent = await File.ReadAllTextAsync(profilePath);

            Assert.Contains("public virtual StudentProfile StudentProfile { get; set; }", studentContent);
            Assert.Contains("public virtual Student Student { get; set; }", profileContent);

            var studentConfigPath = Path.Combine(_testOutputPath, "Configurations", "StudentConfiguration.cs");
            var profileConfigPath = Path.Combine(_testOutputPath, "Configurations", "StudentProfileConfiguration.cs");

            var studentConfigContent = await File.ReadAllTextAsync(studentConfigPath);
            var profileConfigContent = await File.ReadAllTextAsync(profileConfigPath);

            Assert.Contains(".HasOptional(s => s.StudentProfile)", studentConfigContent);
            Assert.Contains(".WithRequired(p => p.Student)", studentConfigContent);
        }

        [Fact]
        public async Task GenerateAsync_OneToMany_GeneratesNavigationPropertiesAndConfiguration()
        {
            var customerTable = new TableDefinition
            {
                TableName = "Customer",
                SchemaName = "dbo",
                Columns = new List<ColumnDefinition>
       {
           new ColumnDefinition { ColumnName = "CustomerId", DataType = "int", IsPrimaryKey = true }
       }
            };

            var orderTable = new TableDefinition
            {
                TableName = "Order",
                SchemaName = "dbo",
                Columns = new List<ColumnDefinition>
       {
           new ColumnDefinition { ColumnName = "OrderId", DataType = "int", IsPrimaryKey = true },
           new ColumnDefinition { ColumnName = "CustomerId", DataType = "int", IsNullable = false }
       },
                ForeignKeys = new List<ForeignKeyDefinition>
       {
           new ForeignKeyDefinition
           {
               ForeignKeyColumn = "CustomerId",
               PrimaryTable = "Customer",
               PrimaryKeyColumn = "CustomerId",
               DeleteRule = "CASCADE"
           }
       }
            };

            _testTables.AddRange(new[] { customerTable, orderTable });
            var generator = new EntityGenerator(_settings, _mockLogger.Object, _testTables);
            await generator.GenerateAsync(_testTables);

            var customerPath = Path.Combine(_testOutputPath, "Entities", "Customer.cs");
            var orderPath = Path.Combine(_testOutputPath, "Entities", "Order.cs");

            Assert.True(File.Exists(customerPath));
            Assert.True(File.Exists(orderPath));

            var customerContent = await File.ReadAllTextAsync(customerPath);
            var orderContent = await File.ReadAllTextAsync(orderPath);

            Assert.Contains("public virtual ICollection<Order> Orders { get; set; }", customerContent);
            Assert.Contains("public virtual Customer Customer { get; set; }", orderContent);

            var customerConfigPath = Path.Combine(_testOutputPath, "Configurations", "CustomerConfiguration.cs");
            var orderConfigPath = Path.Combine(_testOutputPath, "Configurations", "OrderConfiguration.cs");

            var customerConfigContent = await File.ReadAllTextAsync(customerConfigPath);
            var orderConfigContent = await File.ReadAllTextAsync(orderConfigPath);

            Assert.Contains(".HasMany(c => c.Orders)", customerConfigContent);
            Assert.Contains(".WithRequired(o => o.Customer)", orderConfigContent);
            Assert.Contains(".WillCascadeOnDelete(true)", orderConfigContent);
        }

        [Fact]
        public async Task GenerateAsync_ManyToMany_GeneratesNavigationPropertiesAndConfiguration()
        {
            var studentTable = new TableDefinition
            {
                TableName = "Student",
                SchemaName = "dbo",
                Columns = new List<ColumnDefinition>
       {
           new ColumnDefinition { ColumnName = "StudentId", DataType = "int", IsPrimaryKey = true }
       }
            };

            var courseTable = new TableDefinition
            {
                TableName = "Course",
                SchemaName = "dbo",
                Columns = new List<ColumnDefinition>
       {
           new ColumnDefinition { ColumnName = "CourseId", DataType = "int", IsPrimaryKey = true }
       }
            };

            var enrollmentTable = new TableDefinition
            {
                TableName = "Enrollment",
                SchemaName = "dbo",
                Columns = new List<ColumnDefinition>
       {
           new ColumnDefinition { ColumnName = "StudentId", DataType = "int", IsPrimaryKey = true },
           new ColumnDefinition { ColumnName = "CourseId", DataType = "int", IsPrimaryKey = true }
       },
                ForeignKeys = new List<ForeignKeyDefinition>
       {
           new ForeignKeyDefinition
           {
               ForeignKeyColumn = "StudentId",
               PrimaryTable = "Student",
               PrimaryKeyColumn = "StudentId"
           },
           new ForeignKeyDefinition
           {
               ForeignKeyColumn = "CourseId",
               PrimaryTable = "Course",
               PrimaryKeyColumn = "CourseId"
           }
       }
            };

            _testTables.AddRange(new[] { studentTable, courseTable, enrollmentTable });
            var generator = new EntityGenerator(_settings, _mockLogger.Object, _testTables);
            await generator.GenerateAsync(_testTables);

            var studentPath = Path.Combine(_testOutputPath, "Entities", "Student.cs");
            var coursePath = Path.Combine(_testOutputPath, "Entities", "Course.cs");

            Assert.True(File.Exists(studentPath));
            Assert.True(File.Exists(coursePath));

            var studentContent = await File.ReadAllTextAsync(studentPath);
            var courseContent = await File.ReadAllTextAsync(coursePath);

            Assert.Contains("public virtual ICollection<Course> Courses { get; set; }", studentContent);
            Assert.Contains("public virtual ICollection<Student> Students { get; set; }", courseContent);

            var studentConfigPath = Path.Combine(_testOutputPath, "Configurations", "StudentConfiguration.cs");
            var courseConfigPath = Path.Combine(_testOutputPath, "Configurations", "CourseConfiguration.cs");

            var studentConfigContent = await File.ReadAllTextAsync(studentConfigPath);
            var courseConfigContent = await File.ReadAllTextAsync(courseConfigPath);

            Assert.Contains(".HasMany(s => s.Courses).WithMany(c => c.Students)", studentConfigContent);
            Assert.Contains(".Map(m => { m.ToTable(\"Enrollment\"); m.MapLeftKey(\"StudentId\"); m.MapRightKey(\"CourseId\"); })", studentConfigContent);
        }
        public void Dispose()
        {
            if (Directory.Exists(_testOutputPath))
            {
                Directory.Delete(_testOutputPath, true);
            }
        }
    }
}