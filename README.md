# EF Core Reverse Engineering Tool

## Overview
This project is a dynamic code generator designed to work with .NET 8.0 and Entity Framework 6.0. It provides a command-line interface (CLI) to automate the creation of essential EF components such as:

- **POCO Classes**: Generate C# classes based on database tables
- **Fluent API Configurations**: Provide detailed mappings for database schemas
- **DbContext**: Integrate table operations into a unified context
- **Optional UnitOfWork**: Manage database transactions and operations efficiently

## Features

### Architecture & Configuration
- **Modern .NET Architecture**:
  - Built on .NET 8.0
  - Leverages `IServiceCollection` for Dependency Injection (DI)
- **Flexible Configuration**:
  - Configure via command line arguments
  - Support for `appsettings.json` and custom configuration files
  - Optional settings for features like pluralization and PascalCase conversion

### Multi-Database Support
- Supports both **SQL Server** and **MySQL** out of the box
- Easy to extend for additional database providers using `IDatabaseSchemaReader` abstraction

### XML Safety and Documentation
- **Escaped XML Characters**: Ensures generated XML comments are valid
- **Multi-line Comment Support**: Properly formatted documentation

### Code Generation
- Generates properties with accurate data types and constraints
- Supports Data Annotations (e.g., `Required`, `StringLength`)
- Outputs well-structured class files for each database table

## Installation

### Global Tool Installation
Install the tool globally using the .NET CLI:

```bash
dotnet tool install --global dotnet-efrev
```

### Required Packages
If you're using the generated code, you'll need:
```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package MySql.EntityFrameworkCore
```

## Usage

### Initialize Configuration
Create default configuration files:
```bash
efrev --init
```

This will create:
- `appsettings.json`: Standard .NET configuration file
- `efrev.json`: Tool-specific configuration file

### Basic Usage
Generate code using default settings:
```bash
efrev
```

### Command Line Options
```bash
Options:
  -c, --connection        Database connection string
  -p, --provider         Database provider (SqlServer/MySql)
  -n, --namespace        Namespace for generated code
  -o, --output          Output directory
  --pluralize           Pluralize collection names
  --data-annotations    Use data annotations
  --config              Path to custom configuration file
  --settings            Path to appsettings.json file
  --init               Initialize configuration files
  --help               Show help message
```

### Configuration Methods

1. Using appsettings.json:
```json
{
  "CodeGenerator": {
    "ConnectionString": "Server=localhost;Database=YourDb;User Id=YourUser;Password=YourPwd;",
    "ProviderName": "Microsoft.Data.SqlClient",
    "Namespace": "MyGeneratedApp.Data",
    "DbContextName": "MyDbContext",
    "UseDataAnnotations": true,
    "IncludeComments": true,
    "IsPluralize": true,
    "OutputDirectory": "./Generated"
  }
}
```

2. Using command line:
```bash
efrev -c "Server=localhost;Database=YourDb;" -p "Microsoft.Data.SqlClient" -o "./Models"
```

3. Using custom config file:
```bash
efrev --config "path/to/efrev.json"
```

4. Mixed approach:
```bash
efrev --settings "custom-appsettings.json" --connection "NewConnectionString"
```

### Configuration Priority
1. Command line arguments (highest)
2. Custom configuration file (efrev.json)
3. appsettings.json (lowest)

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
  Implement `IDatabaseSchemaReader` for additional database providers
- **Customize Code Output**:
  Modify `EntityGenerator` to change the structure or style of generated code

## Common Issues and Troubleshooting

### Connection Issues
If you encounter connection problems:
1. Verify your connection string
2. Ensure database provider is correctly specified
3. Check database permissions

### Configuration Issues
- Make sure configuration files are in the correct location
- Verify JSON syntax in configuration files
- Check file permissions for output directory

## Contributing
Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Submit a pull request with your improvements

## License
This project is licensed under the [MIT License](LICENSE).

## Release Notes
- v1.0.0: Initial CLI release
  - Added command line interface
  - Support for multiple configuration sources
  - Improved error handling and feedback
