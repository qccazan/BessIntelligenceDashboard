---
description: "Use when writing React TypeScript frontend code: pages, components, services, routing, styling. Covers Vite + React 19 + TypeScript + Tailwind CSS v4 patterns."
applyTo: "**/*.tsx"
---
# Frontend React/TypeScript Conventions

## Component Structure

- Functional components only. No class components.
- Pages in `src/pages/` — one file per route (e.g., `LoginPage.tsx`, `DashboardPage.tsx`).
- Reusable components in `src/components/` — extracted when used by 2+ pages.
- Export components as named exports, not default exports.

## Styling

- Tailwind CSS v4 utility classes for all styling. No CSS files unless absolutely necessary.
- Use semantic class grouping: layout → spacing → typography → colors → effects.
- For conditional classes, use template literals or a simple helper. No classnames/clsx library needed for PoC.
- Dark mode: not required for PoC. Design for light mode only.

## API Calls

- All API calls in `src/services/`. One service file per backend resource (e.g., `batteryService.ts`).
- Use `fetch` — no axios. Every service function is typed with request/response interfaces.
- API base URL from `import.meta.env.VITE_API_URL`.
- Pattern for service functions:

```typescript
export async function getBatteries(): Promise<Battery[]> {
  const res = await fetch(`${API_URL}/api/batteries`);
  if (!res.ok) throw new Error(`Failed to fetch batteries: ${res.status}`);
  return res.json();
}
```

## Routing

- React Router v7 for routing. Routes defined in `App.tsx`.
- Login page is the default route (`/`). Dashboard and other pages behind a simple auth check.
- Use `<Navigate>` for redirects, `useNavigate()` for programmatic navigation.

## State Management

- `useState` and `useEffect` for local state. No Redux or Zustand for PoC.
- Auth state: simple `useState<boolean>` in `App.tsx`, passed via props or React Context if needed by 3+ components.
- Lift state up to the nearest common ancestor. Create Context only when prop drilling reaches 3+ levels.

## Authentication (PoC)

- Login page with username/password fields. Hardcoded check: username `admin`, password `admin`.
- On successful login, set auth state to `true` and navigate to dashboard.
- No JWT, no tokens, no backend auth. This is a frontend-only gate.

## TypeScript

- No `any`. Define interfaces for all API responses and component props.
- Interfaces in the same file as the component/service that uses them, unless shared by 3+ files (then `src/types/`).
- Use strict TypeScript configuration.

## Naming

- `PascalCase` for component files and component names.
- `camelCase` for service files, hooks, variables, functions.
- Props interfaces: `ComponentNameProps` (e.g., `LoginPageProps`).
