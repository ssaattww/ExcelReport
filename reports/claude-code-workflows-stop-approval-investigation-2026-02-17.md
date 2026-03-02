# Investigation Report: claude-code-workflows Stop/Approval Patterns

Date: 2026-02-17  
Target: `claude-code-workflows` (backend/frontend/top-level/skills)  
Purpose: Extract reusable Stop/Approval protocol parts for Task 2.3 (`stop-approval-section-template.md`).

## 1. Executive Summary

`claude-code-workflows` has a clear two-mode model:

1. Document/design/planning mode uses explicit human gates (`[Stop: ...]`, `[STOP]`, AskUserQuestion-driven approvals).
2. Autonomous implementation mode starts only after batch approval, then runs status-driven loops until escalation or completion.

The strongest reusable source is `subagents-orchestration-guide` (`claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:87`) plus command-level concrete stop placements (`claude-code-workflows/commands/design.md:24`, `claude-code-workflows/commands/update-doc.md:26`, `claude-code-workflows/commands/front-design.md:43`, `claude-code-workflows/commands/implement.md:17`).

## 2. Scope Findings

### 2.1 Backend commands/agents (`claude-code-workflows/backend/`)

- Backend plugin registers command/agent sets via plugin manifest (`claude-code-workflows/backend/.claude-plugin/plugin.json:23`, `claude-code-workflows/backend/.claude-plugin/plugin.json:35`).
- Backend command behavior is the same stop/approval protocol as top-level command files (example stop gate: `claude-code-workflows/backend/commands/implement.md:17`).
- Backend skill behavior follows the same orchestration stop table (example: `claude-code-workflows/backend/skills/subagents-orchestration-guide/SKILL.md:92`).

### 2.2 Frontend commands/agents (`claude-code-workflows/frontend/`)

- Frontend plugin registers frontend-specific commands/agents (`claude-code-workflows/frontend/.claude-plugin/plugin.json:24`, `claude-code-workflows/frontend/.claude-plugin/plugin.json:34`).
- Frontend introduces uppercase stop marker usage (`claude-code-workflows/frontend/commands/front-design.md:43`, `claude-code-workflows/frontend/commands/front-design.md:52`).
- Frontend autonomous loop uses same escalation/approval semantics as backend (`claude-code-workflows/commands/front-build.md:90`, `claude-code-workflows/commands/front-build.md:94`, `claude-code-workflows/commands/front-build.md:105`).

### 2.3 Top-level commands/agents (`claude-code-workflows/`)

- Top-level commands contain canonical stop/approval patterns (examples below).
- Agents define machine-readable status fields used by orchestrator for resume/escalation decisions.

### 2.4 Skills (`claude-code-workflows/skills/`)

- `subagents-orchestration-guide` is the primary protocol source for stop points and autonomous-mode boundaries (`claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:87`).
- `documentation-criteria` reinforces approval ordering between document artifacts (`claude-code-workflows/skills/documentation-criteria/SKILL.md:20`, `claude-code-workflows/skills/documentation-criteria/SKILL.md:166`).
- `codex-skill-direct-mode` has simplified stop principles useful for Codex delegation fallback (`claude-code-workflows/skills/codex-skill-direct-mode/SKILL.md:37`).

## 3. Stop Point Marker Patterns

### 3.1 Marker formats found

1. `[Stop: <name>]` (most structured)
- `claude-code-workflows/commands/design.md:24`
- `claude-code-workflows/commands/design.md:28`
- `claude-code-workflows/commands/update-doc.md:26`
- `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:183`

2. `[Stop]` (generic)
- `claude-code-workflows/commands/implement.md:51`
- `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:181`

3. `[STOP]` (frontend variant)
- `claude-code-workflows/commands/front-design.md:43`
- `claude-code-workflows/commands/front-design.md:52`

4. Text-only stop directives
- `claude-code-workflows/commands/plan.md:16`
- `claude-code-workflows/commands/build.md:14`
- `claude-code-workflows/commands/build.md:82`

### 3.2 Observed stop semantics

- Document phase stops are approval gates.
- Autonomous-mode stops are exception gates (escalation, requirement change, explicit user interruption).
- Both are explicitly codified in skill-level rules (`claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:89`, `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:257`).

## 4. Approval Request Patterns

### 4.1 AskUserQuestion as standard gate tool

- Explicitly mandated at stop markers (`claude-code-workflows/commands/implement.md:17`).
- Skill-level requirement: use AskUserQuestion for confirmation/questions (`claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:90`).
- Also used in reverse-engineering and update-doc clarification/branching:
  - `claude-code-workflows/commands/reverse-engineer.md:16`
  - `claude-code-workflows/commands/front-reverse-design.md:18`
  - `claude-code-workflows/commands/update-doc.md:86`

### 4.2 Phase-specific approval contract

- Central stop table defines required user action by phase (`claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:92`).
- Work plan approval is special: “batch approval for implementation phase” (`claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:98`).

### 4.3 Approval by structured status fields

- Quality gate: `approved: true` triggers commit (`claude-code-workflows/commands/build.md:76`, `claude-code-workflows/agents/quality-fixer.md:132`).
- Review gate: document decision values include `approved`, `approved_with_conditions`, `needs_revision`, `rejected` (`claude-code-workflows/agents/document-reviewer.md:124`).
- Test review gate: `approved|needs_revision|blocked` (`claude-code-workflows/agents/integration-test-reviewer.md:64`).

## 5. Resume Conditions

### 5.1 Post-stop resume logic

- Requirement-analysis stop resume:
  - Re-run analyzer if scope change affects scale (`claude-code-workflows/commands/implement.md:54`).
  - Proceed if `confidence: "confirmed"` or no scale change (`claude-code-workflows/commands/implement.md:56`).

- After batch approval:
  - Enter autonomous execution mode (`claude-code-workflows/commands/implement.md:18`, `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:230`).

### 5.2 Autonomous loop resume logic

- Task loop control fields:
  - `readyForQualityCheck: true` -> run quality-fixer (`claude-code-workflows/commands/build.md:75`, `claude-code-workflows/agents/task-executor.md:182`).
  - `approved: true` -> commit (`claude-code-workflows/commands/build.md:76`, `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:291`).

- Escalation break conditions:
  - `status: escalation_needed|blocked` (`claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:261`, `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:262`).
  - Requirement changes during run (`claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:264`).

### 5.3 Revision-loop resume logic

- Update-doc: max 2 review rejection cycles, then human intervention (`claude-code-workflows/commands/update-doc.md:131`, `claude-code-workflows/commands/update-doc.md:132`).
- Reverse-engineer and frontend reverse design: max 2 revisions then human review (`claude-code-workflows/commands/reverse-engineer.md:166`, `claude-code-workflows/commands/front-reverse-design.md:159`).

## 6. Reusable Components for Task 2.3

### 6.1 Strong reusable components

1. Stop point table schema
- Reuse Phase/Stop Point/User Action table style from `subagents-orchestration-guide` (`claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:92`).

2. Two-mode execution boundary
- Reuse explicit split: pre-batch approval (human-gated) vs post-batch approval (autonomous until escalation) (`claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:100`, `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:230`).

3. Status-driven resume contract
- Reuse concrete fields: `status`, `approved`, `readyForQualityCheck`, review decision (`claude-code-workflows/agents/task-executor.md:164`, `claude-code-workflows/agents/task-executor.md:182`, `claude-code-workflows/agents/quality-fixer.md:132`, `claude-code-workflows/agents/document-reviewer.md:124`).

4. Loop guardrails
- Reuse max-revision patterns + forced human intervention (`claude-code-workflows/commands/update-doc.md:132`, `claude-code-workflows/commands/reverse-engineer.md:166`).

5. Requirement-change interrupt rule
- Reuse “stop and restart from requirement-analyzer” semantics (`claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:253`, `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:254`).

### 6.2 Gaps/risks to normalize in Task 2.3 template

1. Marker inconsistency
- `[Stop: ...]`, `[Stop]`, `[STOP]`, and plain-language stop directives coexist.

2. Schema mismatch: document-reviewer
- Skill expects `approvalReady` (`claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:137`), while document-reviewer defines `verdict.decision` (`claude-code-workflows/agents/document-reviewer.md:124`).

3. Schema mismatch: design-sync
- Skill expects `sync_status (synced/conflicts_found)` (`claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:138`), while design-sync output uses `CONFLICTS_FOUND | NO_CONFLICTS` (`claude-code-workflows/agents/design-sync.md:121`).

4. “Auto-approved output” wording in document generators
- Some agents say “considered approved at execution” (`claude-code-workflows/agents/work-planner.md:62`, `claude-code-workflows/agents/prd-creator.md:93`, `claude-code-workflows/agents/technical-designer.md:282`, `claude-code-workflows/agents/technical-designer-frontend.md:251`), which can conflict with explicit human approval gates if not distinguished.

## 7. Recommendations for Task 2.3 (`stop-approval-section-template.md`)

1. Standardize marker grammar
- Use one canonical marker: `[Stop: <Gate Name>]`.
- Add required fields under each marker:
  - `trigger`
  - `ask_method` (default `AskUserQuestion`)
  - `required_user_action`
  - `resume_if`
  - `fallback_if_rejected`

2. Separate gate types explicitly
- `approval_gate` (human decision required)
- `escalation_gate` (autonomous stop on risk/error/status)

3. Normalize status contract in template
- Define canonical keys and mapping adapters:
  - `document_review`: map `verdict.decision` -> `approved|approved_with_conditions|needs_revision|rejected`
  - `design_sync`: map `NO_CONFLICTS|CONFLICTS_FOUND` <-> `synced|conflicts_found`

4. Encode batch approval boundary as first-class section
- Include mandatory statement: autonomous execution is forbidden before batch approval.
- Include stop conditions list copied from orchestration skill (`escalation_needed`, `blocked`, requirement change, explicit user stop).

5. Add loop limits and forced-human-review policy
- Standardize: `max_revision_cycles: 2` then `human_intervention_required: true`.

6. Clarify “agent-local approval” vs “user approval”
- Preserve useful fast-write behavior for agent outputs, but explicitly state it never bypasses workflow-level user approval gates.

## 8. Key Reference Index

- Core protocol: `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:87`
- Canonical backend stop handling: `claude-code-workflows/commands/implement.md:17`
- Canonical design stop flow: `claude-code-workflows/commands/design.md:24`
- Canonical document update stop flow: `claude-code-workflows/commands/update-doc.md:26`
- Frontend stop markers: `claude-code-workflows/commands/front-design.md:43`
- Autonomous build loop: `claude-code-workflows/commands/build.md:74`
- Frontend autonomous loop: `claude-code-workflows/commands/front-build.md:90`
- Requirement analyzer output fields: `claude-code-workflows/agents/requirement-analyzer.md:99`
- Task executor output fields: `claude-code-workflows/agents/task-executor.md:164`
- Quality fixer approval field: `claude-code-workflows/agents/quality-fixer.md:132`
- Document review decision field: `claude-code-workflows/agents/document-reviewer.md:124`
- Design-sync status field: `claude-code-workflows/agents/design-sync.md:121`
