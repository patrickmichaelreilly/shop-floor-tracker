# Source Code

This directory will contain the ASP.NET Core application source code.

## Planned Structure
```
src/
├── ShopFloorTracker.Web/          # Main web application
├── ShopFloorTracker.Core/         # Business logic and models
├── ShopFloorTracker.Data/         # Data access layer
├── ShopFloorTracker.Tests/        # Unit and integration tests
└── ShopFloorTracker.sln           # Solution file
```

## Development Notes
- Target Framework: .NET 8.0
- Architecture: Clean Architecture pattern
- Database: Entity Framework Core with SQLite/SQL Server Express