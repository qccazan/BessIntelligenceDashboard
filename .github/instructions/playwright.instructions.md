---
description: "Use when writing Playwright E2E tests: test files, page objects, test helpers. Covers Playwright TypeScript test patterns for the BESS Intelligence Dashboard."
applyTo: "**/*.spec.ts"
---
# Playwright E2E Test Conventions

## File Structure

- One test file per user story: `e2e/tests/F-XX/us-XX-XX.spec.ts`.
- File names use `kebab-case` matching the user story ID.
- Mirror the `specs/features/` folder structure.

## Test Structure

- Each acceptance criterion (AC) from the spec becomes at least one `test()` block.
- Use `test.describe()` to group tests by user story.
- Test names should reference the AC: `test('AC-1: login screen is the first view visible on page load', ...)`.
- Keep tests independent — each test should be able to run in isolation.

```typescript
import { test, expect } from '@playwright/test';

test.describe('US-01-01: Branded Login Screen', () => {
  test('AC-1: login screen is first and only view on initial page load', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: /BESS Intelligence/i })).toBeVisible();
  });
});
```

## Selectors

- Prefer accessible selectors: `getByRole`, `getByLabel`, `getByText`, `getByTestId`.
- Use `data-testid` attributes only when semantic selectors are ambiguous.
- Avoid CSS selectors and XPath unless absolutely necessary.
- Use Page Object Model only if a page is referenced by 3+ test files. Otherwise, inline selectors.

## Assertions

- Use `expect()` with Playwright's built-in matchers: `toBeVisible`, `toHaveText`, `toHaveURL`, `toHaveCount`.
- For visual layout: check element visibility, text content, and relative positioning. No pixel-level snapshot testing for PoC.
- Always `await` assertions that interact with the page.

## Navigation & Auth

- Every test starts with `page.goto('/')` — the login page.
- For tests behind authentication, create a helper that logs in:

```typescript
async function login(page: Page) {
  await page.goto('/');
  await page.getByLabel('Username').fill('admin');
  await page.getByLabel('Password').fill('admin');
  await page.getByRole('button', { name: /log in/i }).click();
}
```

- Place shared helpers in `e2e/helpers/`.

## Running Tests

- **CRITICAL**: After generating new tests, always run the ENTIRE test suite:
  ```bash
  cd e2e && npx playwright test
  ```
- Never run only the new tests. The full suite must pass.
- Use `--reporter=list` for CI output.

## Configuration

- Base URL in `playwright.config.ts` points to the local frontend dev server.
- Test against Chromium only for PoC speed. Add Firefox/WebKit later.
- Timeout: 30 seconds per test (default).
