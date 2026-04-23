# BESS Intelligence Dashboard — Project Guidelines

## Tech Stack

- **Backend**: .NET 10, ASP.NET Core Web API, Entity Framework Core, SQL Server LocalDB (dev), SQLite (deployed PoC)
- **Frontend**: Vite + React 19 + TypeScript, Tailwind CSS v4, React Router
- **E2E Tests**: Playwright (TypeScript)
- **CI/CD**: GitHub Actions → Azure App Service

## Architecture

### Backend (`backend/`)

- **Repository pattern**: One repository per database table. No service layer.
- Controllers call repositories directly via constructor injection.
- Base interface `IRepository<T>` with standard CRUD. Each table gets its own `IXxxRepository` extending the base.
- EF Core `DbContext` with a `Seed()` static method called from `Program.cs` on startup (development only).
- Migrations managed via `dotnet ef migrations`.
- Controllers return DTOs, not EF entities. Map manually (no AutoMapper for PoC).
- API routes: `api/[controller]` convention.

### Frontend (`frontend/`)

- Functional components only. No class components.
- Pages live in `src/pages/`, reusable components in `src/components/`, API calls in `src/services/`.
- Tailwind utility classes for all styling. No CSS files unless absolutely necessary.
- API base URL configured via environment variable `VITE_API_URL`.
- Use `fetch` for API calls (no axios). Wrap in typed service functions in `src/services/`.

### E2E Tests (`e2e/`)

- One test file per user story: `e2e/tests/F-XX/US-XX-XX.spec.ts`.
- Each acceptance criterion (AC) becomes at least one `test()` block.
- After adding new tests, always run the **entire** test suite: `npx playwright test`.
- Use Page Object Model only if a page is referenced by 3+ test files. Otherwise, inline selectors.

### Authentication

- PoC only: hardcoded check on the frontend. Username: `admin`, Password: `admin`.
- No JWT, no backend auth middleware. Login page gates access via React state.

## Project Structure

```
BessIntelligenceDashboard/
├── .github/
│   ├── agents/            # Custom Copilot agents
│   ├── instructions/      # File-specific instructions
│   ├── prompts/           # Reusable prompt templates
│   ├── workflows/         # GitHub Actions CI/CD
│   └── copilot-instructions.md  # This file
├── backend/
│   └── BessIntelligence.Api/
│       ├── Controllers/
│       ├── Data/           # DbContext, Seed
│       ├── Models/         # EF entities
│       ├── Repositories/   # IRepository<T>, implementations
│       ├── DTOs/
│       └── Program.cs
├── frontend/
│   └── src/
│       ├── pages/
│       ├── components/
│       ├── services/
│       └── App.tsx
├── e2e/
│   ├── tests/
│   │   └── F-XX/          # Mirrors feature structure
│   └── playwright.config.ts
└── specs/
    ├── user-stories.md    # Master document (Markdown)
    └── features/          # Parsed individual feature files
        └── F-XX/
            └── US-XX-XX.md
```

## Conventions

- Use `PascalCase` for C# files, types, and public members. `camelCase` for private fields prefixed with `_`.
- Use `camelCase` for TypeScript/React. `PascalCase` for component names and files.
- Playwright test files use `kebab-case` matching the user story ID: `us-01-01.spec.ts`.
- Feature specs in `specs/features/` use uppercase IDs: `US-01-01.md`.
- All API endpoints are RESTful. Use proper HTTP verbs.
- No `any` in TypeScript. Define interfaces for all API responses.

## Build & Run Commands

```bash
# Backend
cd backend/BessIntelligence.Api
dotnet restore
dotnet build
dotnet run

# Frontend
cd frontend
npm install
npm run dev          # Dev server
npm run build        # Production build

# E2E Tests
cd e2e
npm install
npx playwright install
npx playwright test  # Run ALL tests
```
