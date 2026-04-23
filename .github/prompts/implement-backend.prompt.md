---
description: "Implement the .NET backend (model, repository, controller, migration, seed) for a specific user story."
agent: "backend-dev"
argument-hint: "User story ID (e.g., US-01-01)"
---
Implement the backend for user story **{{input}}**.

1. Read the spec at `specs/features/` for this user story.
2. Create the required EF model, DTOs, repository (interface + implementation), and controller.
3. Register the repository in DI and add the DbSet to AppDbContext.
4. Update the Seed() method with sample data.
5. Create an EF migration.
6. Run `dotnet build` to validate.

Follow the repository pattern strictly — no service layer. Controllers call repositories directly.
