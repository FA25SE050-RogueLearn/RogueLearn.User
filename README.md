# RogueLearn Clean Architecture Microservice Template

A comprehensive .NET 9 microservice template following Clean Architecture principles with Domain-Driven Design patterns. This template provides a solid foundation for building scalable, maintainable microservices with pre-configured dependencies and best practices.

## 🏗️ Architecture Overview

This template implements Clean Architecture with the following layers:

- **API Layer** (`RogueLearn.User.Api`) - Web API controllers, middleware, and configuration
- **Application Layer** (`RogueLearn.User.Application`) - Business logic, CQRS handlers, and application services
- **Domain Layer** (`RogueLearn.User.Domain`) - Domain entities, value objects, and business rules
- **Infrastructure Layer** (`RogueLearn.User.Infrastructure`) - Data access, external services, and infrastructure concerns
- **Building Blocks** (`BuildingBlocks.Shared`) - Shared utilities and common interfaces

## 🚀 Features

- ✅ Clean Architecture with 4 distinct layers
- ✅ Domain-Driven Design patterns
- ✅ CQRS with MediatR
- ✅ AutoMapper for object mapping
- ✅ FluentValidation for input validation
- ✅ MassTransit for messaging with RabbitMQ
- ✅ Supabase integration for database operations
- ✅ Serilog for structured logging
- ✅ Swagger/OpenAPI documentation
- ✅ Health checks
- ✅ Repository pattern implementation
- ✅ Dependency injection setup

## 📋 Prerequisites

- .NET 9.0 SDK or higher
- Visual Studio 2022, VS Code, or JetBrains Rider
- Git (for version control)

## 🛠️ Template Installation

### 1. Install the Template

Navigate to the template directory and install it:

```bash
# Navigate to the template directory
cd "d:\University Stuffs\Semester 9\SEP490\Code Files\RogueLearn.CleanArchitecuterV2"

# Install the template
dotnet new install .
```

### 2. Verify Installation

Check if the template is installed successfully:

```bash
dotnet new list roguelearn-microservice
```

## 🎯 Creating a New Microservice

### Basic Usage

Create a new microservice with default settings:

```bash
# Create a new directory for your microservice
mkdir MyNewService
cd MyNewService

# Create the microservice from template
dotnet new roguelearn-microservice
```

### Advanced Usage with Parameters

Customize your microservice with specific parameters:

```bash
dotnet new roguelearn-microservice \
  --ServiceName "UserService" \
  --SolutionName "UserManagement" \
  --CompanyName "YourCompany" \
  --ArchitectureName "CleanArchV2" \
  --Port 5001 \
  --EnableSwagger true \
  --EnableHealthChecks true
```

### Parameter Reference

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ServiceName` | string | RogueLearn.User | The name of the microservice (e.g., UserService, ProductService) |
| `SolutionName` | string | MySolution | The name of the solution file |
| `CompanyName` | string | RogueLearn | The company or organization name |
| `ArchitectureName` | string | CleanArchitecuterV2 | The architecture version name |
| `Port` | integer | 5000 | The port number for the API service |
| `EnableSwagger` | bool | true | Enable Swagger/OpenAPI documentation |
| `EnableHealthChecks` | bool | true | Enable health check endpoints |

## 📁 Generated Project Structure

After creating a new microservice, you'll get the following structure:

```
YourServiceName/
├── YourServiceName.sln
├── building_blocks/
│   └── BuildingBlocks.Shared/
│       ├── Common/
│       ├── Extensions/
│       ├── Interfaces/
│       ├── Repositories/
│       └── Utilities/
└── src/
    ├── YourServiceName.Api/
    │   ├── Controllers/
    │   ├── Extensions/
    │   ├── Middleware/
    │   └── Program.cs
    ├── YourServiceName.Application/
    │   ├── Behaviours/
    │   ├── Features/
    │   ├── Interfaces/
    │   └── Mappings/
    ├── YourServiceName.Domain/
    │   ├── Entities/
    │   ├── Enums/
    │   ├── Events/
    │   ├── Interfaces/
    │   └── ValueObjects/
    └── YourServiceName.Infrastructure/
        ├── Extensions/
        ├── Messaging/
        ├── Persistence/
        └── Services/
```

## 🔧 Post-Creation Setup

### 1. Build the Solution

```bash
dotnet build
```

### 2. Restore NuGet Packages

```bash
dotnet restore
```

### 3. Update Configuration

Edit the `appsettings.json` and `appsettings.Development.json` files in the API project to configure:
- Database connection strings
- RabbitMQ settings
- Supabase configuration
- Logging settings

### 4. Run the Application

```bash
cd src/YourServiceName.Api
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:{Port}`
- HTTPS: `https://localhost:{Port+1}`
- Swagger UI: `https://localhost:{Port+1}/swagger`

## 📚 Usage Examples

### Example 1: Creating a User Service

```bash
mkdir UserService
cd UserService
dotnet new roguelearn-microservice --ServiceName "UserService" --Port 5001
```

### Example 2: Creating a Product Service

```bash
mkdir ProductService
cd ProductService
dotnet new roguelearn-microservice \
  --ServiceName "ProductService" \
  --SolutionName "ECommerce" \
  --CompanyName "MyCompany" \
  --Port 5002
```

### Example 3: Creating a Notification Service without Swagger

```bash
mkdir NotificationService
cd NotificationService
dotnet new roguelearn-microservice \
  --ServiceName "NotificationService" \
  --EnableSwagger false \
  --Port 5003
```

## 🔄 Template Management

### Update Template

To update the template with new changes:

```bash
# Uninstall the old version
dotnet new uninstall RogueLearn.CleanArchitecture.Microservice

# Install the updated version
dotnet new install .
```

### Uninstall Template

To remove the template:

```bash
dotnet new uninstall RogueLearn.CleanArchitecture.Microservice
```

### List All Templates

To see all installed templates:

```bash
dotnet new list
```

## 🏃‍♂️ Quick Start Guide

1. **Install the template**:
   ```bash
   dotnet new install .
   ```

2. **Create a new microservice**:
   ```bash
   mkdir MyService && cd MyService
   dotnet new roguelearn-microservice --ServiceName "MyService"
   ```

3. **Build and run**:
   ```bash
   dotnet build
   cd src/MyService.Api
   dotnet run
   ```

4. **Open Swagger UI**:
   Navigate to `https://localhost:5001/swagger`

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test the template
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

For issues and questions:
- Create an issue in the repository
- Contact the RogueLearn team
- Check the documentation

---

**Happy coding with RogueLearn Clean Architecture! 🚀**