using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace CHC.EF.Reverse.ConsoleApp.Tests
{
    /// <summary>
    /// ���� EntityGenerator ���O������椸���աC
    /// </summary>
    /// <remarks>
    /// ���սd��]�t�G
    /// 1. �@��@���p���]�w����
    /// 2. �@��h���p���]�w����
    /// 3. �h��h���p���]�w����
    /// 4. ���~�B�z�M�Ѽ�����
    /// </remarks>
    public class EntityGeneratorTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Settings _testSettings;
        private const string TestOutputPath = "TestOutput";

        /// <summary>
        /// ��l�ƴ������ҩM�@�θ귽�C
        /// </summary>
        public EntityGeneratorTests()
        {
            _mockLogger = new Mock<ILogger>();
            _testSettings = new Settings
            {
                OutputDirectory = TestOutputPath,
                Namespace = "TestNamespace",
                UseDataAnnotations = true,
                IncludeComments = true,
                IncludeForeignKeys = true
            };

            // �T�O���տ�X�ؿ��s�b
            Directory.CreateDirectory(TestOutputPath);
        }

        /// <summary>
        /// ���իغc�禡���Ѽ����ҡC
        /// </summary>
        [Fact]
        public void Constructor_WithNullParameters_ThrowsArgumentNullException()
        {
            // Arrange
            Settings settings = null;
            ILogger logger = null;
            List<TableDefinition> tables = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new EntityGenerator(settings, _mockLogger.Object, new List<TableDefinition>()));
            Assert.Throws<ArgumentNullException>(() =>
                new EntityGenerator(_testSettings, logger, new List<TableDefinition>()));
            Assert.Throws<ArgumentNullException>(() =>
                new EntityGenerator(_testSettings, _mockLogger.Object, tables));
        }

        /// <summary>
        /// ���դ@��@���p���]�w�ͦ��C
        /// </summary>
        [Fact]
        public async Task GenerateAsync_WithOneToOneRelationship_GeneratesCorrectConfiguration()
        {
            // Arrange
            var tables = CreateOneToOneRelationshipTables();
            var generator = new EntityGenerator(_testSettings, _mockLogger.Object, tables);

            // Act
            await generator.GenerateAsync(tables);

            // Assert
            var userEntityPath = Path.Combine(TestOutputPath, "Entities", "User.cs");
            var userProfileEntityPath = Path.Combine(TestOutputPath, "Entities", "UserProfile.cs");
            var userConfigPath = Path.Combine(TestOutputPath, "Configurations", "UserConfiguration.cs");
            var userProfileConfigPath = Path.Combine(TestOutputPath, "Configurations", "UserProfileConfiguration.cs");

            Assert.True(File.Exists(userEntityPath));
            Assert.True(File.Exists(userProfileEntityPath));
            Assert.True(File.Exists(userConfigPath));
            Assert.True(File.Exists(userProfileConfigPath));

            // ���ҹ������O���e
            var userEntityContent = await File.ReadAllTextAsync(userEntityPath);
            Assert.Contains("public virtual UserProfile UserProfile { get; set; }", userEntityContent);

            var userProfileEntityContent = await File.ReadAllTextAsync(userProfileEntityPath);
            Assert.Contains("public virtual User User { get; set; }", userProfileEntityContent);

            // ���ҳ]�w���O���e
            var userConfigContent = await File.ReadAllTextAsync(userConfigPath);
            Assert.Contains(".HasRequired(t => t.UserProfile)", userConfigContent);
            Assert.Contains(".WithRequiredPrincipal()", userConfigContent);

            var userProfileConfigContent = await File.ReadAllTextAsync(userProfileConfigPath);
            Assert.Contains(".HasRequired(t => t.User)", userProfileConfigContent);
            Assert.Contains(".WithRequiredDependent()", userProfileConfigContent);
        }

        /// <summary>
        /// ���դ@��h���p���]�w�ͦ��C
        /// </summary>
        [Fact]
        public async Task GenerateAsync_WithOneToManyRelationship_GeneratesCorrectConfiguration()
        {
            // Arrange
            var tables = CreateOneToManyRelationshipTables();
            var generator = new EntityGenerator(_testSettings, _mockLogger.Object, tables);

            // Act
            await generator.GenerateAsync(tables);

            // Assert
            var orderEntityPath = Path.Combine(TestOutputPath, "Entities", "Order.cs");
            var orderItemEntityPath = Path.Combine(TestOutputPath, "Entities", "OrderItem.cs");
            var orderConfigPath = Path.Combine(TestOutputPath, "Configurations", "OrderConfiguration.cs");
            var orderItemConfigPath = Path.Combine(TestOutputPath, "Configurations", "OrderItemConfiguration.cs");

            Assert.True(File.Exists(orderEntityPath));
            Assert.True(File.Exists(orderItemEntityPath));
            Assert.True(File.Exists(orderConfigPath));
            Assert.True(File.Exists(orderItemConfigPath));

            // ���ҹ������O���e
            var orderEntityContent = await File.ReadAllTextAsync(orderEntityPath);
            Assert.Contains("public virtual ICollection<OrderItem> OrderItems { get; set; }", orderEntityContent);
            Assert.Contains("OrderItems = new HashSet<OrderItem>();", orderEntityContent);

            var orderItemEntityContent = await File.ReadAllTextAsync(orderItemEntityPath);
            Assert.Contains("public virtual Order Order { get; set; }", orderItemEntityContent);

            // ���ҳ]�w���O���e
            var orderConfigContent = await File.ReadAllTextAsync(orderConfigPath);
            Assert.Contains(".HasMany(t => t.OrderItems)", orderConfigContent);
            Assert.Contains(".WithRequired(t => t.Order)", orderConfigContent);

            var orderItemConfigContent = await File.ReadAllTextAsync(orderItemConfigPath);
            Assert.Contains(".HasRequired(t => t.Order)", orderItemConfigContent);
            Assert.Contains(".WithMany()", orderItemConfigContent);
        }

        /// <summary>
        /// ���զh��h���p���]�w�ͦ��C
        /// </summary>
        [Fact]
        public async Task GenerateAsync_WithManyToManyRelationship_GeneratesCorrectConfiguration()
        {
            // Arrange
            var tables = CreateManyToManyRelationshipTables();
            var generator = new EntityGenerator(_testSettings, _mockLogger.Object, tables);

            // Act
            await generator.GenerateAsync(tables);

            // Assert
            var studentEntityPath = Path.Combine(TestOutputPath, "Entities", "Student.cs");
            var courseEntityPath = Path.Combine(TestOutputPath, "Entities", "Course.cs");
            var enrollmentEntityPath = Path.Combine(TestOutputPath, "Entities", "StudentCourseEnrollment.cs");

            Assert.True(File.Exists(studentEntityPath));
            Assert.True(File.Exists(courseEntityPath));
            Assert.True(File.Exists(enrollmentEntityPath));

            // ���ҹ������O���e
            var studentEntityContent = await File.ReadAllTextAsync(studentEntityPath);
            Assert.Contains("public virtual ICollection<Course> Courses { get; set; }", studentEntityContent);
            Assert.Contains("Courses = new HashSet<Course>();", studentEntityContent);

            var courseEntityContent = await File.ReadAllTextAsync(courseEntityPath);
            Assert.Contains("public virtual ICollection<Student> Students { get; set; }", courseEntityContent);
            Assert.Contains("Students = new HashSet<Student>();", courseEntityContent);

            // ���ҳ]�w���O���e
            var studentConfigContent = await File.ReadAllTextAsync(Path.Combine(TestOutputPath, "Configurations", "StudentConfiguration.cs"));
            Assert.Contains(".HasMany(t => t.Courses)", studentConfigContent);
            Assert.Contains(".WithMany(t => t.Students)", studentConfigContent);
            Assert.Contains(".Map(m =>", studentConfigContent);
            Assert.Contains("m.ToTable(\"StudentCourseEnrollment\")", studentConfigContent);
        }

        /// <summary>
        /// ���տ��~�B�z���p�C
        /// </summary>
        [Fact]
        public async Task GenerateAsync_WithInvalidConfiguration_HandlesErrorsGracefully()
        {
            // Arrange
            var invalidTables = new List<TableDefinition>
            {
                new TableDefinition
                {
                    TableName = "InvalidTable",
                    Columns = new List<ColumnDefinition>(),
                    ForeignKeys = new List<ForeignKeyDefinition>
                    {
                        new ForeignKeyDefinition
                        {
                            // �L�Ī��~��]�w
                            ForeignKeyColumn = "NonExistentColumn",
                            PrimaryTable = "NonExistentTable"
                        }
                    }
                }
            };

            var generator = new EntityGenerator(_testSettings, _mockLogger.Object, invalidTables);

            // Act & Assert
            await generator.GenerateAsync(invalidTables);

            // ���ҿ��~��x�O��
            _mockLogger.Verify(
                x => x.Warning(It.IsAny<string>()),
                Times.AtLeastOnce,
                "���ӰO��ĵ�i�T��"
            );
        }

        #region Helper Methods

        /// <summary>
        /// �إߴ��եΪ��@��@���p���w�q�C
        /// </summary>
        private List<TableDefinition> CreateOneToOneRelationshipTables()
        {
            return new List<TableDefinition>
            {
                new TableDefinition
                {
                    TableName = "User",
                    Columns = new List<ColumnDefinition>
                    {
                        new ColumnDefinition
                        {
                            ColumnName = "UserId",
                            DataType = "int",
                            IsPrimaryKey = true,
                            IsIdentity = true
                        },
                        new ColumnDefinition
                        {
                            ColumnName = "UserProfileId",
                            DataType = "int",
                            IsNullable = false
                        }
                    },
                    ForeignKeys = new List<ForeignKeyDefinition>
                    {
                        new ForeignKeyDefinition
                        {
                            ForeignKeyColumn = "UserProfileId",
                            PrimaryTable = "UserProfile",
                            PrimaryKeyColumn = "ProfileId",
                            IsEnabled = true
                        }
                    }
                },
                new TableDefinition
                {
                    TableName = "UserProfile",
                    Columns = new List<ColumnDefinition>
                    {
                        new ColumnDefinition
                        {
                            ColumnName = "ProfileId",
                            DataType = "int",
                            IsPrimaryKey = true,
                            IsIdentity = true
                        }
                    }
                }
            };
        }

        /// <summary>
        /// �إߴ��եΪ��@��h���p���w�q�C
        /// </summary>
        private List<TableDefinition> CreateOneToManyRelationshipTables()
        {
            return new List<TableDefinition>
            {
                new TableDefinition
                {
                    TableName = "Order",
                    Columns = new List<ColumnDefinition>
                    {
                        new ColumnDefinition
                        {
                            ColumnName = "OrderId",
                            DataType = "int",
                            IsPrimaryKey = true,
                            IsIdentity = true
                        }
                    }
                },
                new TableDefinition
                {
                    TableName = "OrderItem",
                    Columns = new List<ColumnDefinition>
                    {
                        new ColumnDefinition
                        {
                            ColumnName = "OrderItemId",
                            DataType = "int",
                            IsPrimaryKey = true,
                            IsIdentity = true
                        },
                        new ColumnDefinition
                        {
                            ColumnName = "OrderId",
                            DataType = "int",
                            IsNullable = false
                        }
                    },
                    ForeignKeys = new List<ForeignKeyDefinition>
                    {
                        new ForeignKeyDefinition
                        {
                            ForeignKeyColumn = "OrderId",
                            PrimaryTable = "Order",
                            PrimaryKeyColumn = "OrderId",
                            IsEnabled = true
                        }
                    }
                }
            };
        }

        /// <summary>
        /// �إߴ��եΪ��h��h���p���w�q�C
        /// </summary>
        private List<TableDefinition> CreateManyToManyRelationshipTables()
        {
            return new List<TableDefinition>
            {
                new TableDefinition
                {
                    TableName = "Student",
                    Columns = new List<ColumnDefinition>
                    {
                        new ColumnDefinition
                        {
                            ColumnName = "StudentId",
                            DataType = "int",
                            IsPrimaryKey = true,
                            IsIdentity = true
                        }
                    }
                },
                new TableDefinition
                {
                    TableName = "Course",
                    Columns = new List<ColumnDefinition>
                    {
                        new ColumnDefinition
                        {
                            ColumnName = "CourseId",
                            DataType = "int",
                            IsPrimaryKey = true,
                            IsIdentity = true
                        }
                    }
                },
                new TableDefinition
                {
                    TableName = "StudentCourseEnrollment",
                    
                    Columns = new List<ColumnDefinition>
                    {
                        new ColumnDefinition
                        {
                            ColumnName = "StudentId",
                            DataType = "int",
                            IsPrimaryKey = true
                        },
                        new ColumnDefinition
                        {
                            ColumnName = "CourseId",
                            DataType = "int",
                            IsPrimaryKey = true
                        }
                    },
                    ForeignKeys = new List<ForeignKeyDefinition>
                    {
                        new ForeignKeyDefinition
                        {
                            ForeignKeyColumn = "StudentId",
                            PrimaryTable = "Student",
                            PrimaryKeyColumn = "StudentId",
                            IsEnabled = true
                        },
                        new ForeignKeyDefinition
                        {
                            ForeignKeyColumn = "CourseId",
                            PrimaryTable = "Course",
                            PrimaryKeyColumn = "CourseId",
                            IsEnabled = true
                        }
                    }
                }
            };
        }

        #endregion
    }
}