# EF Core Reverse Engineering Tool

.NET 8.0 code generator for Entity Framework 6.0, creating POCO classes, configurations, and DbContext from existing databases.

## Features

### Database Schema Support
- Comprehensive schema reading for SQL Server and MySQL
- Advanced relationship mapping:
  - One-to-One with unique constraint detection
  - One-to-Many with collection navigation
  - Many-to-Many with junction table support
- Column property handling:
  - Custom data types and constraints
  - Computed columns and generated values
  - Collation and encoding settings
- Index and key management:
  - Composite primary/foreign keys
  - Unique and clustered indexes
  - Filtered indexes (SQL Server)

### Code Generation
- Clean POCO entities with relationship navigation
- Fluent API configurations
- Documented DbContext
- XML documentation from schema comments

### Configuration Options
- Multiple config sources (CLI, JSON files)
- Provider selection (SQL Server/MySQL)
- Output customization
- Pluralization options

## Installation

```bash
dotnet tool install --global CHC.EF.Reverse.Poco
```

Required packages:
```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer # For SQL Server
dotnet add package MySql.EntityFrameworkCore # For MySQL
```

## Usage

Initialize config:
```bash
efrev --init
```

Basic usage:
```bash
efrev -c "connection-string" -p "provider-name"
```

### Options
```bash
-c, --connection        Connection string
-p, --provider         Provider (SqlServer/MySql)
-n, --namespace        Target namespace
-o, --output           Output directory
--pluralize            Pluralize collection names
--data-annotations     Use data annotations
--config              Custom config path
--settings            Settings file path
--init                Create config files
```

### Configuration

appsettings.json:
```json
{
  "CodeGenerator": {
    "ConnectionString": "Server=localhost;Database=YourDb;",
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

## Output Examples

Entity:
```csharp
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    
    public virtual Customer Customer { get; set; }
    public virtual ICollection<OrderDetail> Details { get; set; }
}
```

Configuration:
```csharp
modelBuilder.Entity<Order>(entity =>
{
    entity.ToTable("Orders");
    entity.HasKey(e => e.Id);
    
    entity.HasOne(e => e.Customer)
          .WithMany(e => e.Orders)
          .HasForeignKey(e => e.CustomerId);
});
```

## License
[MIT License](LICENSE)
