---
description: "Implement the React frontend (pages, components, services, routing) for a specific user story."
agent: "frontend-dev"
argument-hint: "User story ID (e.g., US-01-01)"
---
Implement the frontend for user story **{{input}}**.

1. Read the spec at `specs/features/` for this user story.
2. **Read `specs/mock-ui.html`** — match its visual design, layout, colors, and component structure.
3. Check the backend controllers for available API endpoints.
3. Create the required page(s), component(s), and API service function(s).
4. Update routing in App.tsx.
5. Use Tailwind CSS for all styling. Reference the acceptance criteria for visual requirements.
6. Add `data-testid` attributes to key elements for Playwright.
7. Run `npm run build` to validate.
