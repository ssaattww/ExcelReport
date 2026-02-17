# Skill Design Guidelines and Phase 2 Refactoring Plan

**Report Date**: 2026-02-17
**Author**: Project Manager (Claude Sonnet 4.5)
**Context**: Task 2.2/2.3 design review based on official Claude Code documentation

---

## Executive Summary

Task 2.2 (completed) and Task 2.3 (planned) deviate from official Claude Code skill design guidelines. This report documents the official patterns, identifies design issues, and provides a refactoring plan to align with best practices.

**Key Finding**: Official documentation recommends keeping `SKILL.md` concise (≤500 lines) and delegating detailed reference material to separate files. Current Task 2.2 approach embeds ~100 lines of detailed contract sections into each of 10 skills, creating ~1000 lines of duplicated content.

**Recommendation**: Refactor Task 2.2 and redesign Task 2.3 to follow official patterns.

---

## Official Claude Code Skill Design Guidelines

### Source
- **Official Documentation**: https://code.claude.com/docs/ja/skills
- **Key Section**: "サポートファイルを追加する" (Add Supporting Files)

### Core Principles

#### 1. Keep SKILL.md Concise
> "Keep `SKILL.md` to 500 lines or less. Move detailed reference material to separate files."

**Rationale**:
- SKILL.md should focus on the skill's core functionality
- Detailed reference materials should not load into context every time a skill runs
- Readers should understand the skill's purpose without wading through extensive documentation

#### 2. Use Supporting Files for Details
> "Large reference docs, API specifications, or collections of examples don't need to load into context every time a skill runs."

**Recommended Structure**:
```
my-skill/
├── SKILL.md (required - overview and navigation)
├── reference.md (detailed API docs - loaded when needed)
├── examples.md (usage examples - loaded when needed)
└── scripts/
    └── helper.py (utility script - executed, not loaded)
```

**How to Reference**:
```markdown
## Additional resources
- For complete API details, see [reference.md](reference.md)
- For usage examples, see [examples.md](examples.md)
```

#### 3. Let Claude Load Details When Needed
- Claude can read supporting files when the skill references them
- Detailed content is loaded on-demand, not upfront
- This keeps the initial skill context small and focused

### Existing Pattern: workflow-entry

**workflow-entry follows official guidelines perfectly**:

**Contract Handshake Section** (~15 lines in SKILL.md):
```markdown
## Contract Handshake

Before execution, build and validate a contract-compliant payload with required fields:
- objective
- scope
- constraints
- acceptance_criteria
- allowed_commands
- sandbox_mode
- route_intent
- route_target
- stop_conditions

If any required field is missing, emit `[Stop: contract-missing-field]`.
Until Task 3.1 is implemented, this section is the minimum contract baseline.
```

**Details Delegated** to:
- `.claude/skills/workflow-entry/references/codex-execution-contract.md`
- `.claude/skills/workflow-entry/references/contract-checklist.md`

**Stop/Approval Enforcement Section** (~10 lines in SKILL.md):
```markdown
## Stop/Approval Enforcement

Use the following tags:
- Stop: `[Stop: reason]`
- Approval: `[Approve: phase-name]`

Protocol and response schema are defined in `references/stop-approval-protocol.md`.
Mandatory stop points and resume conditions are defined in `references/mandatory-stops.md`.
No state transition is allowed unless approval response contains `approved: true`.
```

**Details Delegated** to:
- `.claude/skills/workflow-entry/references/stop-approval-protocol.md`
- `.claude/skills/workflow-entry/references/mandatory-stops.md`

**Pattern**: Concise overview in SKILL.md + detailed references in separate files.

---

## Task 2.2 Design Issues

### Current Approach (Completed)

**What was done**:
- Created `non-entry-execution-contract-template.md` (88 lines, 7 sections)
- Added detailed "Execution Contract" section (~100 lines, 7 subsections) to 10 skills:
  - codex-lifecycle-orchestration
  - backend-lifecycle-execution
  - codex-task-execution-loop
  - backend-task-quality-loop
  - codex-diagnose-and-review
  - backend-diagnose-workflow
  - codex-document-flow
  - backend-document-workflow
  - backend-integration-tests-workflow
  - tmux-sender

**Section Structure** (embedded in each SKILL.md):
1. Binding (~4 lines)
2. Input (~6 lines)
3. Output (~5 lines)
4. Status Semantics (~7 lines)
5. Violation Handling (~5 lines)
6. Example (~40 lines YAML)
7. References (~6 lines)

**Total**: ~100 lines per skill × 10 skills = ~1000 lines of duplicated structure

### Deviation from Official Guidelines

| Aspect | Official Pattern | Task 2.2 Approach | Issue |
|--------|------------------|-------------------|-------|
| SKILL.md size | Concise (≤500 lines) | +100 lines detailed contract | Verbose, buries core functionality |
| Detail location | Separate reference files | Embedded in each SKILL.md | Violates DRY principle |
| Content loading | On-demand when needed | Always loaded with skill | Unnecessary context overhead |
| Maintenance | Update 1 reference file | Update 10 SKILL.md files | High maintenance burden |
| Consistency | Single source of truth | 10 copies risk drift | Consistency risk |

### Example: codex-lifecycle-orchestration

**Before Task 2.2** (focused on core functionality):
```markdown
# Codex Lifecycle Orchestration

## Role
- Main agent performs both orchestration and execution.
- Use skills as procedure modules; do not delegate to subagents.

## Scope
- Requirements
- PRD/ADR/Design
- Work planning
- Implementation execution
- Quality and completion reporting

## Scale Determination
[Table with scale criteria]

## Phase Flow
[Detailed phase descriptions]

## Stop Conditions
- Requirement changes alter scope/scale after planning.
- Quality gate cannot pass within safe fix boundaries.
```

**After Task 2.2** (contract section dominates):
```markdown
# Codex Lifecycle Orchestration

## Role
[4 lines]

## Scope
[6 lines]

## Execution Contract
### Binding
[4 lines]

### Input
[6 lines]

### Output
[5 lines]

### Status Semantics
[7 lines]

### Violation Handling
[5 lines]

### Example
[40 lines of YAML]

### References
[6 lines]

[Original content continues below...]
```

**Problem**: The contract section (~100 lines) now dominates the SKILL.md, pushing core functionality (Role, Scope, Phase Flow) down and making the skill harder to understand at a glance.

---

## Correct Design Pattern

### Recommended Structure

**Individual Skill SKILL.md** (concise reference):
```markdown
# Codex Lifecycle Orchestration

## Role
- Main agent performs both orchestration and execution.

## Scope
- Requirements, PRD/ADR/Design, Work planning, Implementation execution, Quality reporting

## Execution Contract

This skill follows the non-entry execution contract standard.

**Required extensions**: `lifecycle_scale` (small|medium|large), `phase` (requirements|docs|approval|implementation|quality)

For complete contract details, see [workflow-entry/references/non-entry-execution-contract-template.md](../../workflow-entry/references/non-entry-execution-contract-template.md).

### Contract Example

```yaml
input:
  objective: "Run medium lifecycle orchestration"
  scope: { in_scope: ["Design and work plan generation"], out_of_scope: ["Production deployment"] }
  contract_extensions:
    lifecycle_scale: "medium"
    phase: "design"
output:
  status: "completed"
  summary: "Completed medium-scale design and planning phases"
  quality_gate: { result: "pass", evidence: ["Required lifecycle artifacts completed"] }
  contract_extensions:
    lifecycle_scale: "medium"
    phase: "implementation-ready"
```

## Scale Determination
[Original scale table]

## Phase Flow
[Original phase descriptions]
```

**Shared Template** (detailed, single source of truth):
```
workflow-entry/references/
└── non-entry-execution-contract-template.md (already exists from Task 2.2)
```

### Benefits

| Benefit | Impact |
|---------|--------|
| **Concise SKILL.md** | Core functionality visible immediately |
| **DRY Principle** | 1 template instead of 10 copies |
| **Easy Maintenance** | Update 1 file instead of 10 |
| **Consistency** | Single source of truth prevents drift |
| **Official Compliance** | Follows Claude Code best practices |
| **On-demand Loading** | Details loaded only when Claude needs them |

---

## Task 2.3 Design Review

### Original Plan (Rejected)

**What was planned**:
- Create `stop-approval-section-template.md` (8 subsections)
- Add detailed "Stop and Approval Protocol" section (~80-100 lines, 8 subsections) to 12 skills
- Total: ~1000 lines of duplicated structure

**Same issues as Task 2.2**:
- Violates official guideline (concise SKILL.md)
- Creates massive duplication
- Buries core functionality
- High maintenance burden

### Correct Design for Task 2.3

**Recommended Approach**:

1. **Create shared template** (single source of truth):
   - `workflow-entry/references/stop-approval-section-template.md`
   - Contains all 8 subsections with detailed protocol

2. **Add concise reference section** to each of 12 skills:
   ```markdown
   ## Stop and Approval Protocol

   This skill follows the standard stop/approval protocol.

   **Stop points for this skill**:
   - `[Stop: pre-design-approval]` → `[Approve: design-approval]` (before design finalization)
   - `[Stop: sandbox-escalation-required]` → `[Approve: sandbox-escalation]` (permission elevation)
   - `[Stop: quality-gate-failed]` → `[Approve: resume-after-fix]` (failed checks)

   For complete protocol details, see [workflow-entry/references/stop-approval-section-template.md](../../workflow-entry/references/stop-approval-section-template.md).

   For mandatory stops and resume conditions, see [workflow-entry/references/mandatory-stops.md](../../workflow-entry/references/mandatory-stops.md).
   ```

**Length**: ~15-20 lines per skill instead of ~100 lines

---

## Refactoring Plan

### Phase 1: Refactor Task 2.2 (10 skills)

**For each of the 10 skills**:

1. **Replace detailed contract section** (~100 lines) with concise reference (~20 lines):
   - Brief statement: "This skill follows the non-entry execution contract"
   - List required extensions (e.g., `lifecycle_scale`, `phase`)
   - Provide concise YAML example (~15 lines, not 40)
   - Reference template for details

2. **Preserve**:
   - Skill-specific extension definitions
   - Concise contract example
   - Core functionality sections (Role, Scope, etc.)

3. **Remove**:
   - Detailed subsections (Binding, Input, Output, Status Semantics, Violation Handling, References)
   - Verbose YAML examples

**Template** (non-entry-execution-contract-template.md):
- Keep as-is (already comprehensive)
- This becomes the single source of truth

**Expected outcome**:
- Each SKILL.md: -80 lines (from ~100 to ~20 for contract section)
- Total reduction: ~800 lines across 10 skills
- SKILL.md files return to focusing on core functionality

### Phase 2: Implement Task 2.3 with Correct Design (12 skills)

**Create template**:
- `workflow-entry/references/stop-approval-section-template.md`
- 8 subsections with detailed protocol (as originally planned)

**For each of the 12 skills**:
- Add concise Stop/Approval reference section (~15-20 lines)
- List skill-specific stop points (3-5 key stops)
- Reference template for complete protocol

**Expected outcome**:
- Each SKILL.md: +20 lines (instead of +100)
- Total addition: ~240 lines instead of ~1200
- Maintains SKILL.md conciseness

---

## Implementation Strategy

### Task Breakdown

**New Tasks** (replace original 2.2/2.3):

1. **Task 2.2-refactor**: Refactor 10 skills to use concise contract references
   - Input: Current verbose contract sections
   - Output: Concise references (~20 lines each)
   - Estimated effort: 1-2 hours

2. **Task 2.3-revised**: Add concise Stop/Approval references to 12 skills
   - Create stop-approval-section-template.md
   - Add ~15-20 line reference sections to each skill
   - Estimated effort: 1-2 hours

### Validation Criteria

**After refactoring**:
- [ ] Each SKILL.md ≤ 500 lines (ideally much less)
- [ ] Core functionality visible in first ~100 lines
- [ ] Contract/Stop details referenced, not embedded
- [ ] Single template serves as source of truth
- [ ] YAML examples are concise (~15 lines, not 40)
- [ ] Skills remain functionally equivalent

---

## Codex Execution Guidelines

**When delegating to Codex for refactoring/implementation**:

1. **Mandate compliance with this report**:
   - "Follow the design patterns in reports/skill-design-guidelines-2026-02-17.md"
   - "Keep SKILL.md concise (≤500 lines)"
   - "Use references for detailed content"

2. **Provide specific constraints**:
   - "Contract reference section: max 20 lines"
   - "YAML example: max 15 lines (show structure, not every field)"
   - "Preserve all core functionality sections (Role, Scope, Phase Flow, etc.)"

3. **Require evidence**:
   - "Measure SKILL.md line count before/after"
   - "Verify core functionality remains in first 100 lines"
   - "Confirm reference paths are correct"

---

## References

- **Official Documentation**: https://code.claude.com/docs/ja/skills
- **Existing Correct Pattern**: `.claude/skills/workflow-entry/SKILL.md`
- **Task 2.2 Template**: `.claude/skills/workflow-entry/references/non-entry-execution-contract-template.md`
- **Coverage Matrix**: `reports/phase2-coverage-matrix-2026-02-17.md`
- **Feedback Points**: `tasks/feedback-points.md` (Feedback Point 5: Critical thinking and single responsibility)

---

## Appendix: Line Count Comparison

### Before Refactoring (Task 2.2 as-is)

| Skill | Contract Section Lines | Core Functionality Lines | Total SKILL.md Lines |
|-------|------------------------|--------------------------|----------------------|
| codex-lifecycle-orchestration | ~100 | ~150 | ~250 |
| backend-lifecycle-execution | ~100 | ~120 | ~220 |
| (8 more skills) | ~100 each | varies | varies |
| **Total contract lines** | **~1000** | - | - |

### After Refactoring (Proposed)

| Skill | Contract Section Lines | Core Functionality Lines | Total SKILL.md Lines |
|-------|------------------------|--------------------------|----------------------|
| codex-lifecycle-orchestration | ~20 | ~150 | ~170 |
| backend-lifecycle-execution | ~20 | ~120 | ~140 |
| (8 more skills) | ~20 each | varies | varies |
| **Total contract lines** | **~200** | - | - |

**Net Reduction**: ~800 lines across 10 skills

---

**End of Report**
