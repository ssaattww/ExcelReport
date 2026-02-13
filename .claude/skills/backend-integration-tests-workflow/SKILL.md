---
name: backend-integration-tests-workflow
description: Add integration and E2E tests to existing backend code in Claude without subagent delegation. Mirrors add-integration-tests command flow with review and quality gates.
---

# Backend Integration Tests Workflow

## Purpose

- Execute `/add-integration-tests`-equivalent flow directly in Claude.
- Add test skeletons, implement tests, review quality, and validate gates.

## Flow

1. Validate Design Doc path (or select latest approved Design Doc).
2. Generate integration/E2E test skeleton plan from acceptance criteria.
3. Create test implementation task file.
4. Implement skeleton test cases.
5. Review test quality against skeleton intent.
6. Apply review fixes until approved (bounded loop).
7. Run final quality gate (tests + coverage + static checks).
8. Report readiness for commit.

## Bounded Revision Loop

- If review status is `needs_revision`, apply required fixes and re-review.
- Maximum 2 revision loops before escalation.

## Quality Gate Requirements

- All added tests pass.
- Integration test behavior matches Design Doc acceptance criteria.
- Coverage is non-regressive for touched backend modules.
- No unresolved static/build errors.

## Hard Rules

- Do not skip test review between implementation and final quality.
- Do not merge incomplete skeleton coverage.
- Do not claim completion without explicit pass evidence.
