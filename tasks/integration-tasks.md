# Workflow Integration Tasks

Based on:
- [Workflow Integration Analysis](../reports/workflow-integration-analysis.md)
- [Integration Implementation Plan](../reports/integration-implementation-plan.md)

**Project Manager**: Claude Code
**Implementation**: Codex
**Start Date**: 2026-02-13

---

## Phase 1: P0 Foundation Integration (3-5 days)

### 1. Unified Entry Point (入口一本化)

**Priority**: P0 - CRITICAL
**Dependency**: None
**Risk**: High (routing errors could break all workflows)

#### Tasks

- [ ] **1.1** Create new unified entry skill
  - File: `.claude/skills/workflow-entry/SKILL.md`
  - Consolidate routing logic from `backend-workflow-entry` and `codex-workflow-entry`
  - Define priority order: implement/build/task > review/diagnose > design/plan/update-doc/reverse-engineer > add-integration-tests
  - **Assignee**: Codex
  - **Estimate**: 1-2 hours

- [ ] **1.2** Convert existing entries to compatibility adapters
  - Files: `.claude/skills/backend-workflow-entry/SKILL.md`, `.claude/skills/codex-workflow-entry/SKILL.md`
  - Change to "delegate to new entry only" mode
  - Prohibit independent routing
  - **Assignee**: Codex
  - **Estimate**: 1 hour

- [ ] **1.3** Create routing consistency table
  - File: `.claude/skills/workflow-entry/references/routing-table.md`
  - Document workflow → entry mapping
  - **Assignee**: Codex
  - **Estimate**: 30 minutes

- [ ] **1.4** Verification: Representative scenario testing
  - Test: implement, design, diagnose scenarios
  - Verify: Same input → same route (3x execution consistency)
  - Verify: Old entries delegate to new entry
  - **Assignee**: Claude Code (manual testing)
  - **Estimate**: 1 hour

---

### 2. Stop Points & Approval Flow (Stop点・承認フロー明文化)

**Priority**: P0 - CRITICAL
**Dependency**: None (can run in parallel with 1)
**Risk**: High (unauthorized transitions could bypass approvals)

#### Tasks

- [ ] **2.1** Define stop tag format and approval response format
  - Format: `[Stop: reason]`, `[Approve: phase-name]`
  - Response fields: `approved: true/false`, `scope_changes`, `constraints`
  - File: `.claude/skills/workflow-entry/references/stop-approval-protocol.md`
  - **Assignee**: Codex
  - **Estimate**: 1 hour

- [ ] **2.2** Define mandatory stop points per workflow
  - Minimum required: before design approval, before implementation, before high-risk changes, on quality gate failure
  - File: `.claude/skills/workflow-entry/references/mandatory-stops.md`
  - **Assignee**: Codex
  - **Estimate**: 2 hours

- [ ] **2.3** Update lifecycle and document flow skills
  - Files: `.claude/skills/codex-lifecycle-orchestration/SKILL.md`, `.claude/skills/codex-document-flow/SKILL.md`
  - Add stop conditions
  - **Assignee**: Codex
  - **Estimate**: 2 hours

- [ ] **2.4** Document exception conditions
  - Only exceptions: emergency stop, destructive operation cancellation
  - File: Same as 2.1
  - **Assignee**: Codex
  - **Estimate**: 30 minutes

- [ ] **2.5** Verification: Stop → Approval → Resume flow
  - Test: Stop triggers, approval required, resume after approval
  - Test: Rejection triggers safe stop with alternative actions
  - **Assignee**: Claude Code (manual testing)
  - **Estimate**: 1 hour

---

### 3. Codex Execution Contract (Codex実行契約定義)

**Priority**: P0 - CRITICAL
**Dependency**: Must complete before full workflow deployment
**Risk**: High (inconsistent output could break orchestration)

#### Tasks

- [ ] **3.1** Create execution contract specification
  - File: `.claude/skills/workflow-entry/references/codex-execution-contract.md`
  - **Assignee**: Codex
  - **Estimate**: 2 hours

- [ ] **3.2** Define input schema
  - Required: `objective`, `scope`, `constraints`, `acceptance_criteria`, `allowed_commands`, `sandbox_mode`
  - Optional: `context_files`, `known_risks`, `stop_conditions`
  - **Assignee**: Codex (part of 3.1)

- [ ] **3.3** Define output schema
  - Required: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`
  - Status values: `completed | needs_input | blocked | failed`
  - **Assignee**: Codex (part of 3.1)

- [ ] **3.4** Update codex/SKILL.md to reference contract
  - File: `.claude/skills/codex/SKILL.md`
  - Add contract compliance requirement
  - Update prompt templates
  - **Assignee**: Codex
  - **Estimate**: 1 hour

- [ ] **3.5** Update compatibility adapters to reference contract
  - Files: Backend/codex workflow entries
  - **Assignee**: Codex
  - **Estimate**: 30 minutes

- [ ] **3.6** Create contract compliance checklist
  - File: `.claude/skills/workflow-entry/references/contract-checklist.md`
  - **Assignee**: Codex
  - **Estimate**: 30 minutes

- [ ] **3.7** Verification: Contract compliance testing
  - Test: 3 execution types (implement/review/diagnose)
  - Verify: All required fields present
  - Verify: `status=needs_input` triggers stop and approval request
  - **Assignee**: Claude Code (manual testing)
  - **Estimate**: 2 hours

---

### 4. Sandbox Matrix Correction (sandboxマトリクス修正)

**Priority**: P0 - CRITICAL
**Dependency**: Integrated with unified entry (task 1)
**Risk**: Medium-High (read-only on doc generation = failure, excessive permissions = security risk)

#### Tasks

- [ ] **4.1** Reclassify current matrix as read-only vs. write-enabled
  - Document current state
  - **Assignee**: Codex
  - **Estimate**: 30 minutes

- [ ] **4.2** Fix document generation workflows to workspace-write
  - Workflows: design, plan, update-doc, reverse-engineer
  - Update routing in workflow-entry
  - **Assignee**: Codex
  - **Estimate**: 1 hour

- [ ] **4.3** Implement two-stage escalation for review/diagnose
  - Stage 1: Start with read-only
  - Stage 2: Escalate to workspace-write only after approval
  - **Assignee**: Codex
  - **Estimate**: 2 hours

- [ ] **4.4** Define escalation conditions
  - Condition: Fix application required AND approval obtained
  - File: `.claude/skills/workflow-entry/references/sandbox-escalation.md`
  - **Assignee**: Codex
  - **Estimate**: 1 hour

- [ ] **4.5** Synchronize matrix across skills
  - Files: `codex-workflow-entry/SKILL.md`, `codex/SKILL.md`
  - Ensure consistency
  - **Assignee**: Codex
  - **Estimate**: 30 minutes

- [ ] **4.6** Verification: Sandbox selection testing
  - Test: 5+ execution cases
  - Verify: Document workflows select workspace-write
  - Verify: Review/diagnose don't escalate without approval
  - **Assignee**: Claude Code (manual testing)
  - **Estimate**: 1 hour

---

## Phase 1 Rollback Plan

**Trigger conditions:**
- Unauthorized transitions detected (any occurrence)
- Contract required field missing rate ≥ 5%
- Sandbox misselection causes task failure (2+ consecutive)

**Rollback procedure:**
1. Disable new `workflow-entry` skill
2. Re-enable direct routing in compatibility adapters
3. Restore contract to warn-only mode (not strict)
4. Keep legacy sandbox matrix available at `references/sandbox-matrix.legacy.md`

**Rollback verification:**
- [ ] Test rollback procedure (dry run)
- [ ] Document rollback steps in detail
- [ ] Prepare legacy references

---

## Phase 1 Acceptance Criteria

- [ ] New `workflow-entry` skill can launch all 10 workflows
- [ ] Old entries act as compatibility adapters and always delegate to new entry
- [ ] `design/plan/update-doc/reverse-engineer` execute with `workspace-write`
- [ ] `review/diagnose` start with `read-only`, escalate to `workspace-write` only after approval
- [ ] All workflows trigger stop points and do not transition without approval
- [ ] Codex output always includes required contract items (`status`, `changed_files`, `tests`, `quality_gate`, `blockers`)
- [ ] Quality gate results are attached to completion reports
- [ ] All workflows define requirement-change → re-analysis branch
- [ ] Rollback to compatibility mode is executable per workflow
- [ ] Representative scenarios (implement/design/diagnose/review) execute stably 3 consecutive times

---

## Phase 2: Full Workflow Deployment (TBD)

**Note**: Phase 2 tasks will be added after Phase 1 completion and validation.

---

## Progress Tracking

**Overall Progress**: 0/27 tasks completed (0%)

**By Category:**
- Unified Entry: 0/4 completed
- Stop/Approval: 0/5 completed
- Execution Contract: 0/7 completed
- Sandbox Matrix: 0/6 completed
- Rollback Plan: 0/3 completed
- Acceptance Criteria: 0/10 validated

**Next Actions:**
1. Review this task list with stakeholders
2. Begin with task 1.1 (create unified entry skill)
3. Execute tasks in priority order with Codex

---

**Last Updated**: 2026-02-13
**Project Manager**: Claude Code
**Status**: Planning Complete - Ready for Phase 1 Execution
