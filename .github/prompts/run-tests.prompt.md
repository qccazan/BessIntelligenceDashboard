---
description: "Run the entire Playwright E2E test suite and report results."
tools: [execute, read, search]
argument-hint: "Optional: specific test file to focus on"
---
Run the full Playwright E2E test suite.

```bash
cd e2e && npx playwright test
```

Report:
- Total tests: passed / failed / skipped
- If any failures: which tests failed, the error message, and suggested fixes
- If all pass: confirm clean run

If a specific test file was mentioned, still run the **full suite** but highlight results for that file.
