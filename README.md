## Overview
This project is a dynamic code generator designed to work with .NET 8.0 and Entity Framework (EF) Core 8. It automates the creation of essential EF components such as:

- **POCO Classes**: Generate C# classes based on database tables.
- **Fluent API Configurations**: Provide detailed mappings for database schemas.
- **DbContext**: Integrate table operations into a unified context.
- **Optional UnitOfWork**: Manage database transactions and operations efficiently.

## Features

### Architecture & Configuration
- **Modern .NET Architecture**:
  - Built on .NET 8.0.
  - Leverages `IServiceCollection` for Dependency Injection (DI).
- **Flexible Configuration**:
  - Configure namespaces, database connections, and output directories via `appsettings.json`.
  - Optional settings for features like pluralization and PascalCase conversion.

### Multi-Database Support
- Supports both **SQL Server** and **MySQL** out of the box.
- Easy to extend for additional database providers using `IDatabaseSchemaReader` abstraction.

### XML Safety and Documentation
- **Escaped XML Characters**: Ensures generated XML comments are valid by escaping special characters.
- **Multi-line Comment Support**: Automatically formats multi-line comments with proper indentation and alignment.

### Code Generation
- Generates properties with accurate data types, constraints, and annotations (e.g., `Required`, `StringLength`).
- Outputs well-structured class files for each database table.

## Usage

### Prerequisites
- **Environment**:
  - .NET 8.0 SDK
  - EF Core 8
- **Packages**:
  Install the required NuGet packages:
  ```bash
  dotnet add package Microsoft.EntityFrameworkCore
  dotnet add package Microsoft.EntityFrameworkCore.SqlServer
  dotnet add package MySql.EntityFrameworkCore
  dotnet add package Microsoft.Extensions.DependencyInjection
  dotnet add package Microsoft.Extensions.Configuration.Json
  ```

### Configuration
Set up the `appsettings.json` file to match your environment:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=YourDb;User Id=YourUser;Password=YourPwd;"
  },
  "CodeGenerator": {
    "ProviderName": "Microsoft.Data.SqlClient",
    "Namespace": "MyGeneratedApp.Data",
    "DbContextName": "MyDbContext",
    "UseDataAnnotations": true,
    "IncludeComments": true,
    "IsPluralize": true,
    "UsePascalCase": true,
    "GenerateSeparateFiles": true,
    "OutputDirectory": "C:\\CodeGenOutput"
  }
}
```

### How to Run
1. Build and run the project.
2. The generator will:
   - Connect to the database.
   - Read schema information.
   - Generate entity classes, Fluent API configurations, and `DbContext` files in the specified output directory.
3. Generated files are ready to integrate into your EF Core project.

### Example Output
Given a database table `Users`, the generator will produce:

**POCO Class:**
```csharp
[Table("Users")]
public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    public DateTime CreatedAt { get; set; }
}
```

**Fluent API Configuration:**
```csharp
modelBuilder.Entity<User>(entity => {
    entity.ToTable("Users");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
});
```

## Extensibility
- **Add New Database Support**:
  Implement `IDatabaseSchemaReader` for additional database providers.
- **Customize Code Output**:
  Modify `EntityGenerator` to change the structure or style of generated code.

## Contributing
Contributions are welcome! Please fork the repository and submit a pull request with your improvements.

## License
This project is licensed under the [MIT License](LICENSE).

