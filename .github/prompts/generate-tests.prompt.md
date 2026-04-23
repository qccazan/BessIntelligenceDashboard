---
description: "Generate Playwright E2E tests for a specific user story and run the entire test suite."
agent: "playwright-tester"
argument-hint: "User story ID (e.g., US-01-01)"
---
Generate Playwright E2E tests for user story **{{input}}**.

1. Read the spec at `specs/features/` for this user story — focus on acceptance criteria and test cases.
2. Read the implemented frontend code to understand actual selectors and routes.
3. Create the test file at `e2e/tests/F-XX/us-XX-XX.spec.ts`.
4. Write at least one test per acceptance criterion.
5. **Run the ENTIRE test suite**: `cd e2e && npx playwright test`.
6. If any tests fail, fix them and re-run until all pass.
7. Report: tests written, ACs covered, full suite pass/fail status.
