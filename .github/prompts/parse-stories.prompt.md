---
description: "Parse the master user stories document into individual feature spec files with acceptance criteria and test case outlines."
agent: "story-parser"
---
Parse `specs/user-stories.md` into individual feature specification files.

Split every Feature (F-XX) and User Story (US-XX-XX) into its own file at `specs/features/F-XX/US-XX-XX.md`.

Each file must include: user story, description, acceptance criteria, and a test cases table with at least one test per AC.

Report a summary of features, stories, and test cases created.
