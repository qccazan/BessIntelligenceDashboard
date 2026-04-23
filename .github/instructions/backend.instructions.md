---
description: "Use when writing C# backend code: controllers, repositories, models, DTOs, DbContext, migrations, Program.cs. Covers .NET 10 ASP.NET Core Web API patterns."
applyTo: "**/*.cs"
---
# Backend C# Conventions

## Architecture — Repository Pattern

- One repository per database table. No service layer.
- Controllers receive repositories via constructor injection and call them directly.
- Base interface: `IRepository<T>` with `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`.
- Each table gets its own interface (e.g., `IBatteryRepository : IRepository<Battery>`) for custom queries.
- Implementations inherit from `Repository<T>` base class and live in `Repositories/`.

## Controllers

- One controller per resource. Route: `[Route("api/[controller]")]`.
- Return DTOs, never EF entities. Map manually in the controller action (no AutoMapper).
- Use `ActionResult<T>` return types. Return `Ok()`, `NotFound()`, `CreatedAtAction()`, `NoContent()`, `BadRequest()`.
- Keep actions thin: validate input → call repository → map to DTO → return.
- Use `[ApiController]` attribute for automatic model validation.

## Entity Framework Core

- `AppDbContext` lives in `Data/AppDbContext.cs`.
- `Seed()` is a static method on `AppDbContext` called from `Program.cs` in development only.
- Connection string for dev: SQL Server LocalDB in `appsettings.Development.json`.
- Connection string for prod: SQLite in `appsettings.json` (PoC deployment).
- Migrations: `dotnet ef migrations add <Name>` from the API project directory.

## Models & DTOs

- EF entities in `Models/`. No data annotations for validation on entities — use Fluent API in `OnModelCreating`.
- DTOs in `DTOs/`. Use records for simple DTOs: `public record BatteryDto(int Id, string Name, ...)`.
- Request DTOs (for POST/PUT) as separate records: `CreateBatteryRequest`, `UpdateBatteryRequest`.

## Naming

- `PascalCase` for types, public members, files.
- `_camelCase` for private fields.
- Async methods end with `Async` suffix.
- Repository files: `IBatteryRepository.cs`, `BatteryRepository.cs`.
- Controller files: `BatteriesController.cs` (plural).

## Error Handling

- Let `[ApiController]` handle model validation (400 responses).
- Return `NotFound()` for missing resources. No custom exception middleware for PoC.
