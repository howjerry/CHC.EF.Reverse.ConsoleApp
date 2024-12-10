# CHC.EF.Reverse - Database Reverse Engineering Code Generation Tool

This tool is a code generator based on .NET 8.0 and supports EF6.0. It automatically generates entity classes (POCOs), Fluent API configuration classes, and `DbContext` classes from an existing database schema. By analyzing the database structure, relationships, indexes, and other attributes, this tool generates code that can be used in Entity Framework 6, accelerating initial project development and reducing the burden of manually writing large amounts of boilerplate code.

## Features

- **Multiple Database Support**: Supports SQL Server and MySQL database reading. Switch between different providers through configuration options.
- **Automatic Relationship Identification**:
  - One-to-One: Identified and configured based on unique indexes and foreign keys.
  - One-to-Many: Automatically generates collection-type navigation properties and foreign key configurations.
  - Many-to-Many: Supports the identification of intermediate join tables and automatically generates bi-directional collection properties and corresponding configurations.
- **Field and Type Handling**:
  - Automatically generates corresponding .NET types based on database types.
  - Supports computed columns, identity columns, and default value mapping.
  - Supports maximum length, precision, and scale attribute settings.
- **Index and Key Management**:
  - Automatically detects primary keys and composite primary keys.
  - Supports unique indexes, clustered indexes, non-clustered indexes, and their attribute settings.
- **Code Generation and Configuration File Management**:
  - Generates POCO entity classes and their corresponding `Configuration` classes.
  - Automatically generates corresponding `DbContext` classes and `OnModelCreating` configurations.
  - Optionally use Data Annotations or only Fluent API.
  - Supports customization of namespaces, output directories, and class names.
- **Documentation and Comments**:
  - Automatically generates XML comments from the comment fields in the database.
  - Clear log records of the generation process for debugging and review.

## Installation and Prerequisites

This tool can be installed as a .NET Global Tool. Make sure you have .NET 6 (or later, .NET 8 recommended) SDK installed:

```bash
dotnet tool install --global CHC.EF.Reverse.Poco
```

After generating the code, you need to add the Entity Framework 6 package (not EF Core) to your project:

```
Install-Package EntityFramework
```

Or via .NET CLI:

```
dotnet add package EntityFramework
```

## Usage

**Basic Command Example**

```bash
efrev -c "Server=localhost;Database=YourDb;User Id=xxx;Password=xxx;" -p "Microsoft.Data.SqlClient" -n "YourApp.Data" -o "./Generated"
```

The above command explanation:

- `-c` or `--connection`: Database connection string
- `-p` or `--provider`: Database provider, such as `Microsoft.Data.SqlClient` (SQL Server) or `MySql.Data.MySqlClient` (MySQL)
- `-n` or `--namespace`: Namespace for the generated code
- `-o` or `--output`: Output directory path

**Commonly Used Parameters**

- `-c, --connection`: Set the database connection string
- `-p, --provider`: Set the database provider (SqlServer/MySql)
- `-n, --namespace`: Set the namespace for generated code
- `-o, --output`: Set the output directory
- `--pluralize`: Enable pluralization of collection names
- `--data-annotations`: Enable Data Annotations attributes
- `--config`: Specify a custom configuration file path (default is appsettings.json)

You can combine these parameters to suit your project needs.

## Configuration File

By default, the tool attempts to read configuration values from the `CodeGenerator` section in `appsettings.json`. Here is an example:

```json
{
  "CodeGenerator": {
    "ConnectionString": "Server=localhost;Database=YourDb;User Id=xxx;Password=xxx;",
    "ProviderName": "Microsoft.Data.SqlClient",
    "Namespace": "YourApp.Data",
    "DbContextName": "AppDbContext",
    "UseDataAnnotations": true,
    "IncludeComments": true,
    "IsPluralize": true,
    "OutputDirectory": "./Generated"
  }
}
```

You can adjust various settings in this file to customize the code generation behavior.

## Output Example

Example of generated entity class (POCO):

```csharp
public class Order
{
    /// <summary>
    /// Primary key of the order
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Customer Id (foreign key)
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Corresponding customer entity
    /// </summary>
    public virtual Customer Customer { get; set; }

    /// <summary>
    /// Order details collection
    /// </summary>
    public virtual ICollection<OrderDetail> OrderDetails { get; set; }

    public Order()
    {
        OrderDetails = new HashSet<OrderDetail>();
    }
}
```

Example of generated Configuration class:

```csharp
public class OrderConfiguration : EntityTypeConfiguration<Order>
{
    public OrderConfiguration()
    {
        ToTable("Order", "dbo");
        HasKey(t => t.Id);

        // Foreign key configuration
        HasRequired(t => t.Customer)
            .WithMany(t => t.Orders)
            .HasForeignKey(t => t.CustomerId);

        // Property configuration
        Property(t => t.Id)
            .HasColumnName("Id")
            .IsRequired()
            .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

        Property(t => t.CustomerId)
            .HasColumnName("CustomerId")
            .IsRequired();
    }
}
```

## Project Management Considerations

**Integration with CI/CD**:

This tool can be automatically executed in CI to automatically update the code whenever the database schema changes, ensuring that the code is synchronized with the database structure.

**Version Control**:

Use Git to track the generated code. Once the database schema changes, you can view the corresponding code updates through diffs.

**Adaptation and Customization**:

You can customize by modifying the configuration or source code, such as customizing name conversion rules, adding specific Data Annotations, or advanced Fluent API configurations.

## License

This project is licensed under the MIT License. Feel free to use and modify it.

## Release History

**v1.0.0**: Initial release, supporting SQL Server/MySQL database reverse engineering.
Added CLI parameters and configuration file reading mechanism.
Supports entity class, configuration class, and DbContext generation.

For updated information, please refer to the corresponding release page and records.
