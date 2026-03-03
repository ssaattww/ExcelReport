# Phase 3 Styles + ExpressionEngine Design Analysis and Implementation Plan

Report Date: 2026-03-03  
Owner: Codex  
Purpose: Phase 3 implementation planning artifact for `Styles` and `ExpressionEngine`

## 1. Scope and Source Set

This plan is based on the following requested sources:

- `Design/Styles/Styles_DetailDesign.md`
- `Design/ExpressionEngine/ExpressionEngine.md`
- `Design/BasicDesign_v1.md` (Styles / ExpressionEngine sections)
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md` (style-related sections)
- `ExcelReport/ExcelReportLib/DSL/AST/StyleAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/StylesAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/StyleRefAst.cs`

The current codebase already contains AST and parser-side style reference infrastructure, but the design still leaves several ownership boundaries and runtime behaviors to be fixed before implementation.

## 2. Styles Module Design Summary

### 2.1 Responsibilities and interface

The `Styles` module is responsible for:

- collecting global styles from `<styles>` blocks and style import sources
- building a global style dictionary keyed by style name
- validating style definitions and references
- validating scope compatibility between style usage location and declared style scope
- normalizing style properties into a logical style representation
- returning an ordered logical application plan to downstream layout processing

The design intent is that `Styles` resolves and validates logical styles, but does not apply physical Excel formatting itself. Physical style application belongs later in the pipeline.

The primary design interface is:

```csharp
public interface IStyleResolver
{
    StylePlan ResolveStyles(
        IReadOnlyList<StyleRefAst> styleRefs,
        StyleAst? inlineStyle,
        StyleApplicationScope scope,
        IList<Issue> issues);
}
```

The contract implied by the design is:

- input: already parsed style references, optional inline style, and the usage scope (`cell` or `grid`)
- output: an ordered logical `StylePlan`
- side effects: append warnings/errors to the shared issue list instead of throwing for recoverable validation failures

### 2.2 Input and output types

Design-facing input/output types are:

- `StyleAst`
  - global style definition
  - logical fields: `Name`, `Scope`, and style properties (`font`, `fill`, `border`, `numberFormat`)
- `StyleRefAst`
  - named reference to a global style
- `LocalStyleAst` (design concept)
  - inline style shape without `Name` or `Scope`
- `GlobalStyles`
  - global dictionary of style definitions, keyed by style name
- `StylePlan`
  - ordered style application candidates for later merge/application

The current implementation differs slightly from the design:

- there is no separate `LocalStyleAst`; inline and global `<style>` currently share `StyleAst`
- `StyleAst` already materializes parsed properties into `RawProperties` and typed accessors
- `StyleRefAst` can hold a resolved `StyleRef` link populated after parsing

### 2.3 `StylePlan` structure

The design describes `StylePlan` as a logical application plan, not a final merged style:

```text
StylePlan
  AppliedStyles : List<StyleAst>
  InlineStyle   : LocalStyleAst?
```

However, the design also requires each applied style to retain ordering context:

- hierarchy level (sheet / component / grid / cell)
- within-level precedence
- whether the source came from attribute, `<styleRef>`, or inline `<style>`

This creates a practical gap: `List<StyleAst>` alone is not enough to preserve the metadata required for deterministic merge. Phase 3 implementation should therefore introduce a wrapper record (for example, `ResolvedStyleCandidate`) that carries:

- referenced `StyleAst`
- application scope
- hierarchy level
- sequence/order index
- source kind (`attribute`, `styleRef`, `inline`)

That wrapper should be the actual unit inside `StylePlan`.

### 2.4 Scope validation rules

The design requires scope-aware validation at style usage time:

- `scope=grid` used in a cell context: warning
- `scope=cell` used in a grid context: warning
- `scope` omitted: treat as `both`
- `border@mode=outer` or `border@mode=all` used in a cell context: warning

Required fallback behavior:

- invalid scope usage should not stop rendering
- border effects that are invalid for the target scope are ignored
- non-border properties (`font`, `fill`, `numberFormat`) remain applicable when possible

This means scope validation is not all-or-nothing. The implementation must validate at property granularity, especially for border-related rules.

## 3. ExpressionEngine Design Summary

### 3.1 Responsibilities and interface

The `ExpressionEngine` is responsible for evaluating DSL expressions written as `@(...)` using Roslyn scripting.

Core responsibilities:

- evaluate embedded C# expressions against runtime data
- expose report data and variables through a controlled globals object
- cache compiled delegates to avoid repeated compilation
- return safe error output without aborting report generation
- emit diagnostic information to logging / issue tracking

The designed interface is:

```csharp
public interface IExpressionEvaluator
{
    object? Evaluate(string expression, EvaluationContext context);
}
```

Supporting design concept:

```csharp
public sealed class EvaluationContext
{
    public object? Root { get; init; }
    public object? Data { get; init; }
    public IReadOnlyDictionary<string, object?> Vars { get; init; }
}
```

### 3.2 Expression evaluation mechanism

The intended evaluation flow is:

1. The parser/layout pipeline identifies an expression body from `@(...)`.
2. `ExpressionEngine` creates a Roslyn script for the expression.
3. The expression is compiled into a reusable delegate.
4. A globals object is created from `EvaluationContext`.
5. The compiled delegate is executed with those globals.
6. The result is returned as `object?`.
7. Compilation/runtime failures are converted into a non-fatal error value and logged.

The design specifically centers on Roslyn scripting via `CSharpScript.Create<object>(...)`.

Practical implications for Phase 3:

- the parser should pass the inner expression body, not the literal `@(...)`
- globals shape must be stable and testable
- error handling must be deterministic because report generation continues after failures

### 3.3 Cache strategy

The intended cache strategy is a compiled-script cache keyed by expression string.

Baseline design:

- use `ConcurrentDictionary<string, ...>` for thread-safe access
- cache compiled delegates, not raw evaluation results
- reuse delegates across repeated calls with different `EvaluationContext`

The main open choice is cache lifetime:

- per report execution
- per service / application lifetime

For Phase 3, the safer default is per `IExpressionEvaluator` instance with explicit ownership. That preserves reuse while keeping memory behavior bounded by the chosen evaluator lifetime.

## 4. Existing AST and Parser Connection Points

### 4.1 Existing AST definitions

Current AST already provides the core style nodes:

- `StyleScope` enum: `Cell`, `Grid`, `Both`
- `StyleAst`
  - `Name`
  - `Scope`
  - `Span`
  - `RawProperties`
  - typed accessors such as `FontName`, `FontSize`, `FontBold`, `FillColor`, `NumberFormatCode`, `Borders`
- `BorderInfo`
  - `Mode`, `Top`, `Bottom`, `Left`, `Right`, `Color`
- `StylesAst`
  - collection of global `StyleAst`
  - collection of `StyleImportAst`
- `StyleRefAst`
  - `Name`
  - `Span`
  - nested `StyleRefs`
  - resolved `StyleRef` link populated later

This means Phase 3 does not need a fresh style AST model. It needs to align runtime behavior with the current AST and close design gaps around ordering and validation.

### 4.2 Effective integration points

The main implementation touchpoints are:

- `DslParser.ResolveStyleRefs`
  - central place where global styles are indexed and `StyleRefAst.StyleRef` is assigned
  - natural hook for duplicate-name policy, unresolved reference warnings, and `cell@styleRef` shortcut wiring
- `LayoutNodeAst`
  - carries style references and inline style information used for later style resolution
- `CellAst`
  - already captures `cell@styleRef` shortcut as a string, but it is not fully integrated into `StyleRefAst` resolution
- `SheetAst`
  - currently supports sheet-level style references, but not sheet-level inline style objects implied by the DSL design

Phase 3 should treat these as the canonical attachment points instead of introducing parallel style plumbing.

### 4.3 Current design-to-code gaps

The most relevant mismatches are:

- no separate inline-style AST type even though the design distinguishes `LocalStyleAst`
- `StylePlan` design requires ordering metadata that current AST objects do not carry
- `StyleAst` does not currently enforce required `name` for global style definitions
- `cell@styleRef` exists in AST but is not fully resolved through the same reference pipeline
- `SheetAst` does not model sheet-level inline styles described by the design

## 5. Recommended Implementation Task Breakdown

The task order below is dependency-first. Subtasks are intentionally small enough to execute and verify independently.

### 5.1 Task 1: Fix design contract decisions before coding

Decide and document the following before implementation starts:

- whether final style precedence is owned by `Styles` or by `LayoutEngine`
- whether duplicate global style names are resolved as first-wins or later-wins
- the exact error return contract for expression failures (`#ERR(...)`, error object, or dedicated value type)
- the lifetime of the expression delegate cache
- whether Phase 3 includes sheet-level inline styles and `componentImport`-level style contribution

This is the highest-leverage task because several implementation details are blocked by unresolved ownership.

### 5.2 Task 2: Normalize AST and parser input to match the intended design

Implement parser-facing alignment work:

1. Add explicit handling for duplicate global style names in `DslParser.ResolveStyleRefs`.
2. Wire `cell@styleRef` shortcut into the same resolution path used by `<styleRef>`.
3. Enforce or at least issue warnings for missing global `style@name`.
4. Decide whether to keep nested `StyleRefAst.StyleRefs`; if retained, define deterministic flattening order.
5. If required by scope, extend `SheetAst` parsing to support inline sheet-level `<style>`.

This task should complete before the runtime `Styles` module is finalized, because `Styles` needs stable parsed inputs.

### 5.3 Task 3: Introduce runtime style planning types

Create the runtime types needed to carry design intent correctly:

- `StyleApplicationScope` (runtime usage context)
- `ResolvedStyleCandidate` (style + source metadata + ordering)
- `StylePlan` (ordered candidates + optional inline candidate representation)
- optional helper types for issue emission and property-level scope filtering

Key rule: do not reuse bare `List<StyleAst>` as the full runtime contract, because it cannot preserve required precedence metadata.

### 5.4 Task 4: Implement `IStyleResolver`

Implement the core `Styles` runtime service:

1. Flatten all applicable style sources into a deterministic sequence.
2. Validate each referenced style against usage scope.
3. Apply border-specific scope filtering (`cell` vs `outer` / `all`).
4. Emit warnings instead of throwing for recoverable scope violations.
5. Return a `StylePlan` that downstream code can merge/apply deterministically.

If final property merge remains a `LayoutEngine` responsibility, `IStyleResolver` should stop at plan creation. If ownership is moved into `Styles`, add a second merge step and return a final resolved style object.

### 5.5 Task 5: Implement `ExpressionEngine`

Implement the evaluator in a narrow, testable slice:

1. Define `EvaluationContext` and the concrete globals object shape.
2. Strip or standardize expression input so the evaluator receives a canonical expression body.
3. Compile expressions with Roslyn scripting.
4. Cache compiled delegates by expression string.
5. Execute delegates against runtime globals.
6. Convert compilation/runtime failures into the agreed non-fatal error result.
7. Record issues/log output without throwing unless the failure is unrecoverable infrastructure-level failure.

This work is mostly independent from `Styles`, but both modules should follow the same issue-reporting conventions.

### 5.6 Task 6: Integrate both modules into the layout/render pipeline

After both services exist, wire them into actual report planning:

1. Insert style resolution at the point where sheet/component/grid/cell style sources are known.
2. Ensure merge order matches the chosen precedence contract.
3. Insert expression evaluation at the point where DSL values are converted into runtime values.
4. Ensure expression errors propagate as report issues, not pipeline crashes.
5. Confirm style and expression evaluation can coexist in the same layout pass.

This is the first task where cross-module regressions are likely, so it should be isolated from lower-level implementation tasks.

### 5.7 Task 7: Add verification coverage

Add targeted tests in dependency order:

1. parser tests for duplicate styles, unresolved style refs, and `cell@styleRef`
2. `Styles` unit tests for scope validation and ordering
3. `Styles` tests for border mode filtering behavior
4. `ExpressionEngine` unit tests for successful evaluation, compile failure, runtime failure, and cache reuse
5. integration tests covering style resolution plus expression evaluation inside a representative report layout

The key quality gate is deterministic behavior under warnings: both systems are explicitly designed to keep report generation moving after recoverable errors.

## 6. Design Ambiguities and Required Clarifications

The following items should be resolved explicitly before implementation is considered complete:

### 6.1 Styles

- The design splits responsibility awkwardly between `Styles` and `LayoutEngine`; final precedence ownership is not fully explicit.
- Duplicate style-name handling is inconsistent across documents (`first wins + error` vs `later wins + warning`).
- `StylePlan` is underspecified for the amount of precedence metadata the design requires.
- It is unclear whether `border@mode` scope validation belongs in DSL validation, `Styles`, or both.
- The DSL design expects more style surface area (`cell@styleRef`, possible sheet-level inline style, `componentImport` styles) than the current AST/parser fully enforces.

### 6.2 ExpressionEngine

- Error return shape is inconsistent across descriptions (string, placeholder text, or dedicated object).
- Logging/issue reporting is described, but the interface does not define how issues are emitted.
- Allowed imports/namespaces need a final, technically correct list.
- Cache lifetime is left open, which affects memory behavior and reuse.
- Ownership of stripping `@(...)` syntax is not fully fixed between parser and evaluator.

## 7. Recommended Phase 3 Execution Order

A practical execution order is:

1. Lock the unresolved contracts in Section 6.
2. Align parser/AST behavior so style and expression inputs are canonical.
3. Introduce runtime planning types (`StylePlan`, `ResolvedStyleCandidate`, `EvaluationContext`).
4. Implement `IStyleResolver`.
5. Implement `IExpressionEvaluator`.
6. Integrate both into layout/render flow.
7. Add unit and integration coverage.

This sequence minimizes rework because parser/input normalization and contract decisions affect both runtime services.
