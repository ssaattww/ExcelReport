# Sandbox Matrix Investigation Phase Analysis

**Date**: 2026-02-17
**Context**: Task 2.4 prerequisite - Fix sandbox-matrix.md for investigation workflows
**Analyst**: Project Manager + Codex (gpt-5.3-codex, reasoning: high)

---

## Problem Statement

**Issue**: Current `sandbox-matrix.md` does not properly handle investigation workflows that require writing reports to `reports/` directory.

**Root Cause**: Investigation phase is currently mapped to `diagnose` intent, which defaults to `read-only` sandbox mode, preventing report file creation.

**Impact**: Blocks Task 2.4 and all future investigation workflows that need to produce written reports.

---

## Requirements

1. Investigation phase must support writing to `reports/` directory
2. Design must remain deterministic (same input → same sandbox)
3. Follow official Claude Code skill design guidelines (concise, clear)
4. Maintain consistency with existing routing-table.md and sandbox-matrix.md design
5. Preserve single responsibility principle

---

## Codex Analysis Results

### Four Approaches Evaluated

#### Approach 1: Add `investigate` as new canonical intent

**Implementation**:
- Add `investigate` to `routing-table.md` as new canonical intent
- Add `investigate` → `workspace-write` to `sandbox-matrix.md`

**Codex Evaluation**:
- ❌ Higher complexity and ambiguity risk in deterministic candidate resolution
- ❌ Potential overlap/conflict with existing `diagnose` intent
- ❌ Adds new canonical intent (more complex routing logic)

**PM Review**:
- ⚠️ Increases routing table complexity
- ⚠️ May create ambiguity between `investigate` and `diagnose`
- 🔴 Not recommended

---

#### Approach 2: Treat investigation as `design`

**Implementation**:
- Change `routing-table.md`: `investigate` → `design` (currently `investigate` → `diagnose`)
- No change to `sandbox-matrix.md` (design already uses `workspace-write`)

**Codex Evaluation**:
- ❌ Inconsistent with current intent semantics (`investigate` is currently under `diagnose`)

**PM Review**:
- ✅ Simplest change (only `routing-table.md` modified)
- ✅ Semantically natural: investigation → design → implementation flow
- ✅ `design` already has `workspace-write` permission
- ✅ No sandbox-matrix changes needed
- ✅ "Investigation as pre-design research" is a valid interpretation
- ⚠️ Changes existing `investigate` → `diagnose` mapping
- 🟢 **Strong alternative - worth deeper consideration**

**Questions**:
1. Is investigation fundamentally different from design, or is it a pre-design phase?
2. What is the semantic boundary between "investigate" and "design"?
3. Does changing the mapping break existing workflows that expect `investigate` → `diagnose`?

---

#### Approach 3: Add investigation-specific sandbox mode

**Implementation**:
- Add new sandbox mode (e.g., `reports-write-only`)
- Provide limited write permissions for investigation

**Codex Evaluation**:
- ❌ Not aligned with current allowed modes (`read-only`, `workspace-write`, `danger-full-access`)
- ❌ Significant design change to introduce new sandbox mode

**PM Review**:
- ❌ Codex CLI doesn't support custom sandbox modes
- ❌ Would require changes to core Codex implementation
- ❌ Over-engineering for the problem at hand
- 🔴 Not feasible

---

#### Approach 4: Document investigation sandbox policy + Policy Change (Codex Recommended)

**Implementation**:
- Change `diagnose` default sandbox from `read-only` → `workspace-write`
- Keep `review` as `read-only`
- Keep `investigate` → `diagnose` mapping in `routing-table.md`
- Document that investigation artifacts go to `reports/*`

**Files to Modify**:
1. **Primary**: `.claude/skills/workflow-entry/references/sandbox-matrix.md:17`
   - Change `diagnose` default to `workspace-write`
   - Update resolution rule at line 24
   - Add guardrail: investigation artifacts → `reports/*`
2. **Consistency updates**:
   - `.claude/skills/workflow-entry/SKILL.md:76`
   - `.claude/skills/workflow-entry/references/mandatory-stops.md:20`
   - `.claude/skills/workflow-entry/references/sandbox-escalation.md:3`
   - `.claude/skills/codex/SKILL.md:25, 56, 68`
3. **Optional**: `.claude/skills/workflow-entry/references/routing-table.md:13` (add trigger example)

**Codex Evaluation**:
- ✅ Solves report creation directly
- ✅ Preserves existing canonical intents
- ✅ Keeps routing deterministic
- ⚠️ `diagnose` gets broader default write capability

**PM Review**:
- ✅ Maintains existing `investigate` → `diagnose` mapping
- ✅ Deterministic routing preserved
- ✅ No new canonical intent or sandbox mode
- ⚠️ **Semantic shift**: `diagnose` now means "diagnose + report writing"
- ⚠️ **Asymmetry**: `review` (read-only) vs `diagnose` (workspace-write) - why different?
- ⚠️ **Mitigation unclear**: "Keep explicit stop/approval for code fixes" - where is this enforced?
- ⚠️ **Risk**: Broader write permissions for all `diagnose` workflows (not just investigation)
- 🟡 **Viable but needs clarification**

**Questions**:
1. What is the semantic difference between `review` and `diagnose` that justifies different sandbox defaults?
2. How do we enforce "reports-only writes" vs "code writes" within the same `diagnose` intent?
3. Does this violate single responsibility principle? (diagnose = investigate + analyze + report)

---

## Comparative Analysis

| Criterion | Approach 1 (New Intent) | Approach 2 (→ Design) | Approach 3 (New Sandbox) | Approach 4 (Diagnose → WW) |
|---|---|---|---|---|
| **Simplicity** | ❌ Complex | ✅ Simple | ❌ Very Complex | 🟡 Moderate |
| **Files Modified** | 2 (routing, sandbox) | 1 (routing only) | Many | 5+ (sandbox + consistency) |
| **Semantic Clarity** | ❌ Overlaps diagnose | ✅ Natural flow | N/A | ⚠️ Diagnose shifts meaning |
| **Determinism** | ✅ Maintained | ✅ Maintained | ✅ Maintained | ✅ Maintained |
| **Single Responsibility** | 🟡 Adds new intent | ✅ Preserved | N/A | ⚠️ Diagnose = 2+ things |
| **Risk** | Medium | Low | Very High | Medium |
| **Feasibility** | ✅ Yes | ✅ Yes | ❌ No | ✅ Yes |
| **Codex Recommendation** | ❌ Rejected | ❌ Rejected | ❌ Rejected | ✅ Recommended |
| **PM Assessment** | 🔴 Not Recommended | 🟢 Strong Alternative | 🔴 Not Feasible | 🟡 Viable with Caveats |

---

## PM Critical Review

### Approach 4 (Codex Recommended) - Concerns

1. **Semantic Consistency**:
   - `review` = read-only observation
   - `diagnose` = read-only analysis + workspace-write reporting?
   - Why should diagnosis produce artifacts but review doesn't?
   - This asymmetry needs strong justification

2. **Single Responsibility Violation**:
   - Current: `diagnose` = "analyze and identify root cause"
   - Proposed: `diagnose` = "analyze + identify root cause + write reports"
   - Are we conflating two concerns?

3. **Enforcement Gap**:
   - Mitigation says "keep approval for code fixes"
   - But `diagnose` now has `workspace-write` by default
   - How do we prevent unintended code writes during diagnosis?
   - No clear enforcement mechanism identified

4. **Scope Creep Risk**:
   - Today: investigation needs reports
   - Tomorrow: diagnosis writes code fixes?
   - Slippery slope if we don't draw clear boundaries

### Approach 2 (Investigation → Design) - Strengths

1. **Semantic Clarity**:
   - Investigation = research and analysis **before** design
   - Design = synthesis of research into actionable plan
   - Natural progression: investigate → design → implement

2. **Minimal Change**:
   - Only `routing-table.md` modified
   - No sandbox-matrix changes (design already workspace-write)
   - Fewer consistency updates needed

3. **Clear Responsibility**:
   - Investigation = gather information + document findings
   - Design = create plans based on findings
   - Both can write (investigation → reports, design → plans)

4. **Precedent**:
   - Many workflows already treat investigation as pre-design
   - Task 2.4 approach: investigate → design template → implement
   - This matches actual workflow patterns

### Open Questions for Codex

1. **Semantic Boundaries**:
   - What is the fundamental semantic difference between:
     - `investigate` (current: → diagnose)
     - `design` (current: → design)
   - Is investigation fundamentally a diagnostic activity or a design-prep activity?

2. **Review vs Diagnose Asymmetry**:
   - If `diagnose` gets workspace-write, why not `review`?
   - What is the principle that differentiates them?
   - Is this principle consistent and defensible?

3. **Enforcement Mechanism**:
   - How do we enforce "reports-only writes" in Approach 4?
   - Can we rely on documentation alone, or do we need tooling support?
   - What prevents `diagnose` from writing code if it has workspace-write?

4. **Existing Workflow Impact**:
   - Are there existing workflows that rely on `investigate` → `diagnose` mapping?
   - Would changing to `investigate` → `design` break those workflows?
   - Can we audit current usage patterns?

---

## Recommendation

**Status**: Analysis incomplete - need deeper comparison between Approach 2 and Approach 4.

**Next Steps**:
1. Ask Codex to address PM's critical review questions
2. Request detailed comparison: Approach 2 vs Approach 4
3. Clarify semantic boundaries and enforcement mechanisms
4. Make final decision based on comprehensive analysis

**Preliminary Preference**: Approach 2 (investigation → design)
- Simpler change
- Clearer semantics
- Lower risk

**Codex Preference**: Approach 4 (diagnose → workspace-write)
- Preserves existing mapping
- Direct solution

**Decision Authority**: User approval required after final analysis.

---

## Files Referenced

- `.claude/skills/workflow-entry/references/sandbox-matrix.md`
- `.claude/skills/workflow-entry/references/routing-table.md`
- `.claude/skills/workflow-entry/SKILL.md`
- `.claude/skills/codex/SKILL.md`

---

## Appendix: Current State

### Current Mapping (routing-table.md)
```
investigate → diagnose
```

### Current Sandbox (sandbox-matrix.md)
```
diagnose: read-only (escalation to workspace-write requires approval)
design: workspace-write
```

### Problem
```
investigate → diagnose → read-only → ❌ cannot write reports
```

### Solution A (Approach 4)
```
investigate → diagnose → workspace-write ✅
```

### Solution B (Approach 2)
```
investigate → design → workspace-write ✅
```

---

## Codex Follow-up Analysis (Detailed Response)

### Executive Summary

**Codex Final Recommendation**: **Approach 2** (`investigate` → `design`) **with explicit semantic redefinition**

**Condition**: In this repository, `investigate` means pre-design research/reporting, NOT bug root-cause debugging.

**Critical Warning**: Do NOT adopt Approach 4 as currently defined unless hard technical enforcement is implemented. Documentation-only guardrails are insufficient.

---

### 1. Semantic Boundaries Analysis

**Fundamental Definitions**:
- `investigate` = **epistemic/diagnostic act** - establish what is true and why
- `design` = **normative/prescriptive act** - decide what should be built

**Upstream Semantics** (claude-code-workflows):
- `diagnose` explicitly states "Investigate problem…" (`claude-code-workflows/commands/diagnose.md:3`)
- `design` explicitly states "NEVER investigate/analyze yourself" (`claude-code-workflows/commands/design.md:13`)

**Repository-Specific Usage**:
- In this repo, `investigate` is used for pre-design research/reporting
- Investigation produces reports as design artifacts
- This is process-level design-prep, not pure bug debugging

**Conclusion**: Investigation is diagnostic fundamentally, but can be design-prep in process terms.

---

### 2. Approach 2 vs Approach 4 - Deeper Implications

#### Approach 2 Strengths
✅ **Preserves least-privilege model** already established for diagnose/review (read-only first)
✅ **Minimal mechanical change** (single routing row in routing-table.md:13)
✅ **Avoids reopening Phase 1 sandbox policy baseline** (completed in tasks/integration-tasks.md:213, reports/phase1-current-status-2026-02-13.md:47)

#### Approach 2 Weaknesses
⚠️ **Semantic drift** from upstream diagnose semantics
⚠️ **Misrouting risk**: "investigate bug" requests might incorrectly route to document flow

#### Approach 4 Strengths
✅ **Preserves current routing semantics** (`investigate` under diagnose)
✅ **Directly solves** report-writing friction

#### Approach 4 Weaknesses
❌ **Conflicts with explicit current policy** that review/diagnose start read-only and escalate only after approval:
   - `.claude/skills/workflow-entry/SKILL.md:76`
   - `.claude/skills/workflow-entry/references/mandatory-stops.md:20`
   - `.claude/skills/workflow-entry/references/sandbox-escalation.md:12`
   - `.claude/skills/codex/SKILL.md:25`
❌ **Broadens default write authority** for ALL diagnose requests, not just reporting
❌ **Requires non-trivial policy re-baselining** across 6+ files/docs

#### Alignment Assessment
- **Single Responsibility**: Approach 2 better (unless diagnose scope is redefined with mode separation)
- **Semantic Clarity**: Approach 2 clearer for this repo's "research report" usage; weaker for bug-debug wording
- **Long-term Maintainability**: Approach 2 better under current architecture; Approach 4 increases policy complexity and drift risk

---

### 3. Review vs Diagnose Asymmetry (Approach 4 Critique)

**Current Asymmetry Would Be Weakly Justified**:
- `review` is not inherently read-only; it can enter fix phase (`claude-code-workflows/commands/review.md:70`)
- `diagnose` also can move to fixes (`.claude/skills/codex-diagnose-and-review/SKILL.md:33, 68`)

**Principle Should Be**:
**Analysis-first intents start read-only; writing requires explicit gate**

By this principle, `review` and `diagnose` should remain **symmetric**.

**Conclusion**: Approach 4's asymmetry lacks strong justification.

---

### 4. Enforcement Mechanism (Approach 4 Critical Gap)

**Documentation Alone is NOT Enough**:
- With `workspace-write`, sandbox no longer prevents code writes
- Current prevention is mostly procedural stop/approval policy, NOT technical isolation

**If Approach 4 is Chosen, Add Hard Controls**:
1. **Contract scope restriction**: `scope.in_scope = reports/**`, `out_of_scope = source dirs`
2. **Post-run diff gate**: Reject non-`reports/*` writes before completion
3. **Explicit stop**: Before any transition from report-write to code-fix phase
4. **Contract violation**: Fail/blocked status, NOT advisory warning

**Without These Controls**: Approach 4 is unsafe.

---

### 5. Existing Workflow Impact Audit

**Hard Mapping Location**: `.claude/skills/workflow-entry/references/routing-table.md:13`

**Executable Code/Tests**: No code/tests specifically keyed to `investigate` as separate runtime contract found

**Policy Dependencies on review/diagnose read-only start + escalation** (Approach 4 migration impact):
- `.claude/skills/workflow-entry/references/sandbox-matrix.md:17, 18`
- `.claude/skills/workflow-entry/SKILL.md:76`
- `.claude/skills/workflow-entry/references/mandatory-stops.md:20`
- `.claude/skills/workflow-entry/references/sandbox-escalation.md:12`
- `.claude/skills/codex/SKILL.md:25`
- `tasks/integration-tasks.md:213`

**Migration Impact**:
- **Approach 2**: Low technical impact, Medium-High user-behavior risk (bug investigations phrased as "investigate")
- **Approach 4**: Medium-High technical impact (6+ files), High policy re-baselining effort

---

### 6. Risk Assessment Summary

| Risk Category | Approach 2 | Approach 4 |
|---|---|---|
| Security/Policy Risk | 🟢 Low | 🔴 High |
| Enforcement Risk | 🟢 Low | 🔴 High (without tooling) |
| Semantic Misroute Risk | 🟡 Medium-High | 🟢 Low |
| Migration Effort | 🟢 Low | 🟡 Medium-High |
| **Overall Risk** | 🟢 **Low-Medium** | 🔴 **High** |

---

### 7. Final Decision Framework

**Decision Path**:

1. **If least-privilege and existing Phase-1 sandbox invariants are non-negotiable**:
   → Choose **Approach 2**

2. **If preserving "investigate = diagnostic" wording is non-negotiable**:
   → Approach 4 is ONLY acceptable with hard enforcement controls (not docs-only)

3. **If you cannot implement path-level/report-only enforcement NOW**:
   → Do **NOT** choose Approach 4

4. **If choosing Approach 2, immediately document lexical guidance**:
   - Use `debug`/`root cause`/`diagnose` for failure analysis
   - Use `investigate` for research/report-before-design

**Codex Recommendation**: This is the most defensible path against SRP, clarity, and maintainability criteria under current codebase state.

---

## Final Project Manager Decision

### Recommendation: **Approach 2** (investigate → design)

**Rationale**:
1. ✅ **Security**: Preserves least-privilege model (Phase 1 baseline)
2. ✅ **Simplicity**: Minimal change (1 file, 1 line)
3. ✅ **Maintainability**: No policy re-baselining across 6+ files
4. ✅ **Feasibility**: No hard enforcement controls required
5. ✅ **Risk**: Low security/policy risk vs High for Approach 4
6. ⚠️ **Tradeoff**: Requires semantic redefinition and lexical guidance

**Implementation Plan**:
1. Modify `.claude/skills/workflow-entry/references/routing-table.md:13`:
   - Change: `investigate` → `diagnose`
   - To: `investigate` → `design`
2. Add lexical guidance to `workflow-entry/SKILL.md` or new reference doc:
   - **Use `investigate`**: For research, analysis, and report generation before design
   - **Use `diagnose`**: For bug root-cause analysis and failure investigation
   - **Use `debug`/`root cause`**: For explicit debugging workflows
3. Update any existing documentation referencing investigation workflows
4. No sandbox-matrix.md changes required (design already workspace-write)

**Migration Risk Mitigation**:
- Document clear usage guidelines
- Monitor for misrouted "investigate bug" requests
- Add trigger examples to routing-table.md if needed
- Phase 2 verification tasks (2.21-2.23) will validate routing behavior

**Decision Authority**: Requires user approval before implementation.

---

**Analysis Status**: ✅ Complete - Ready for user decision and implementation
