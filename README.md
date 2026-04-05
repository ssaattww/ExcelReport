# ExcelReportLib

[![PR xUnit](https://github.com/ssaattww/ExcelReport/actions/workflows/pr-xunit-tests.yml/badge.svg)](https://github.com/ssaattww/ExcelReport/actions/workflows/pr-xunit-tests.yml)
[![Publish NuGet](https://github.com/ssaattww/ExcelReport/actions/workflows/publish-nuget.yml/badge.svg)](https://github.com/ssaattww/ExcelReport/actions/workflows/publish-nuget.yml)
[![NuGet Version](https://img.shields.io/nuget/v/ExcelReportLib)](https://www.nuget.org/packages/ExcelReportLib/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ExcelReportLib)](https://www.nuget.org/packages/ExcelReportLib/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

A .NET 8 library for generating `.xlsx` workbooks from a custom XML DSL (`urn:excelreport:v2`) and runtime data.

## Overview

`ExcelReportLib` converts declarative report definitions into Excel files through a staged pipeline:

1. Parse XML DSL into AST nodes.
2. Evaluate expressions and resolve styles/components.
3. Expand layout primitives (`grid`, `repeat`, `use`, `cell`) into concrete coordinates.
4. Build worksheet state (cells, merges, named areas, sheet options, chart state).
5. Render OpenXML workbook streams, including `_Issues` and `_Audit` sheets when applicable.

Primary orchestration is handled by `ReportGenerator`.

## Features

- XML DSL-based report definitions (`urn:excelreport:v2`)
- Expression evaluation (`@(root...)`, `@(data...)`, `@(vars...)`)
- Reusable components via `<component>` and `<use>`
- External component import via `<componentImport>`
- Collection expansion via `<repeat>`
- Style system with imports, composition, borders, and number formats
- Named areas and formula placeholder resolution (e.g. `#{Detail.Value:Detail.ValueEnd}`)
- Formula reference aggregation for repeated component rows (`formulaRef`, `formulaRefScope`)
- Worksheet options (freeze panes, grouping, auto filter, conditional formatting)
- Chart rendering (`barStacked`, `line`) with `chartPalette`, `color`, `colorKey`, `colorBy`
- Async generation API (`AsyncReportGenerator`) with job status polling and rendering progress units
- OpenXML rendering with diagnostics and generation audit metadata

## Architecture

```mermaid
flowchart LR
    A[DSL XML] --> B[DslParser]
    B --> C[Workbook AST]

    C --> D[LayoutEngine]
    E[ExpressionEngine] --> D
    F[StyleResolver] --> D

    D --> G[LayoutPlan]
    G --> H[WorksheetStateBuilder]
    H --> I[WorksheetState]
    I --> J[XlsxRenderer]
    J --> K[.xlsx Stream]

    R[ReportGenerator] -. orchestrates .-> B
    R -. orchestrates .-> D
    R -. orchestrates .-> H
    R -. orchestrates .-> J
```

Core modules:

- `DSL/DslParser`: parsing + optional XSD validation + issue collection
- `ExpressionEngine`: expression parsing/evaluation with cache support
- `Styles/StyleResolver`: style indexing and precedence composition
- `LayoutEngine`: layout expansion, repeat/use expansion, conditional rendering
- `WorksheetState/WorksheetStateBuilder`: merge/bounds validation and formula/chart reference resolution
- `Renderer/XlsxRenderer`: OpenXML output generation
- `ReportGenerator`: end-to-end orchestration and phase logging
- `AsyncReportGenerator`: non-blocking job execution, status/result retrieval, cancellation, cleanup

## Installation

### Prerequisites

- .NET SDK 8.0+

### Add package

```bash
dotnet add package ExcelReportLib
```

### Add as a project reference (from source)

```bash
dotnet add <your-app>.csproj reference ExcelReport/ExcelReportLib/ExcelReportLib.csproj
```

### Build

```bash
dotnet build ExcelReport.sln
```

## Quick Start

### 1) Define DSL

```xml
<workbook xmlns="urn:excelreport:v2">
  <styles>
    <style name="HeaderCell" scope="cell">
      <font bold="true"/>
      <fill color="#F2F2F2"/>
      <border mode="cell" bottom="thin" color="#000000"/>
    </style>
  </styles>

  <component name="ItemHeader">
    <grid rows="1" cols="2">
      <cell r="1" c="1" value="Item" styleRef="HeaderCell" />
      <cell r="1" c="2" value="Value" styleRef="HeaderCell" />
    </grid>
  </component>

  <component name="ItemRow">
    <grid rows="1" cols="2">
      <cell r="1" c="1" value="@(data.Name)" />
      <cell r="1" c="2" value="@(data.Value)" />
    </grid>
  </component>

  <sheet name="Summary">
    <cell r="1" c="1" value="@(root.Title)" />

    <use component="ItemHeader" r="2" c="1" />

    <repeat r="3" c="1" direction="down" from="@(root.Items)" var="it">
      <use component="ItemRow" with="@(it)" />
    </repeat>
  </sheet>
</workbook>
```

### 2) Generate workbook

```csharp
using ExcelReportLib;

var dsl = File.ReadAllText("report.xml");
var data = new
{
    Title = "Sales Report",
    Items = new[]
    {
        new { Name = "Laptop", Value = 1200 },
        new { Name = "Display", Value = 450 },
        new { Name = "Keyboard", Value = 120 }
    }
};

var generator = new ReportGenerator();
var result = generator.Generate(dsl, data);

if (result.Succeeded)
{
    File.WriteAllBytes("report.xlsx", result.Output.ToArray());
}
else
{
    foreach (var issue in result.Issues)
    {
        Console.WriteLine($"[{issue.Severity}] {issue.Kind}: {issue.Message}");
    }
}
```

## Chart Example (formulaRef-based)

```xml
<workbook xmlns="urn:excelreport:v2">
  <chartPalette>
    <color key="Done" value="#4CAF50" />
    <color key="Doing" value="#FF9800" />
    <color key="Todo" value="#BDBDBD" />
  </chartPalette>

  <component name="TaskRow">
    <grid>
      <cell value="@(data.Name)" formulaRef="Task.Name" />
      <cell c="2" value="@(data.Workload)" formulaRef="Task.Workload" />
      <cell c="3" value="@(data.State)" formulaRef="Task.State" />
      <cell c="4" value="@(data.Blocked)" formulaRef="Task.Blocked" />
    </grid>
  </component>

  <sheet name="Summary">
    <repeat direction="down" from="@(root.Tasks)" var="it">
      <use component="TaskRow" with="@(it)" />
    </repeat>

    <chart type="barStacked" title="Progress" r="2" c="8" width="10" height="16" category="Task.Name">
      <series name="Workload" value="Task.Workload" colorBy="Task.State" />
      <series name="Blocked" value="Task.Blocked" color="#1E88E5" />
    </chart>
  </sheet>
</workbook>
```

## Async Generation + Progress Polling

```csharp
using ExcelReportLib;
using ExcelReportLib.Logger;

var asyncGenerator = new AsyncReportGenerator();
var jobId = asyncGenerator.StartGenerate(dsl, data);

while (true)
{
    if (!asyncGenerator.TryGetStatus(jobId, out var status))
    {
        throw new InvalidOperationException("Job not found.");
    }

    Console.WriteLine(
        $"state={status.State}, progress={status.ProgressPercent}% " +
        $"render={status.RenderingCompletedUnits}/{status.RenderingTotalUnits}, " +
        $"phase={status.CurrentPhase}, elapsed={status.ElapsedMilliseconds}ms");

    if (status.State is AsyncReportJobState.Succeeded or AsyncReportJobState.Failed or AsyncReportJobState.Canceled)
    {
        break;
    }

    await Task.Delay(200);
}

if (asyncGenerator.TryGetResult(jobId, out var asyncResult) && asyncResult.Succeeded)
{
    File.WriteAllBytes("report-async.xlsx", asyncResult.Output!.ToArray());
}

_ = asyncGenerator.Remove(jobId); // optional cleanup of completed job record
```

## API Reference Summary

### Primary API

- `ReportGenerator`
  - `Generate(string dsl, object? data, ReportGeneratorOptions? options = null, CancellationToken cancellationToken = default)`
  - `GenerateFromFile(string dslFilePath, object? data, ReportGeneratorOptions? options = null, CancellationToken cancellationToken = default)`
- `AsyncReportGenerator`
  - `StartGenerate(string dsl, object? data, ReportGeneratorOptions? options = null)`
  - `StartGenerateFromFile(string dslFilePath, object? data, ReportGeneratorOptions? options = null)`
  - `TryGetStatus(string jobId, out AsyncReportJobStatus status)`
  - `TryGetResult(string jobId, out ReportGeneratorResult result)`
  - `Cancel(string jobId)`
  - `Remove(string jobId)`
- `ReportGeneratorOptions`
  - `EnableSchemaValidation`
  - `TreatExpressionSyntaxErrorAsFatal`
  - `Logger`
  - `RenderOptions`
- `ReportGeneratorResult`
  - `Output`, `Issues`, `LogEntries`, `Succeeded`, `AbortedByFatal`, `UnhandledException`
- `AsyncReportJobStatus`
  - `State`, `ProgressPercent`, `CurrentPhase`, `ElapsedMilliseconds`
  - `CurrentPhaseElapsedMilliseconds`, `PhaseElapsedMilliseconds`
  - `RenderingCompletedUnits`, `RenderingTotalUnits`, `RenderingProgressPercent`

### Advanced/Composable APIs

- Parsing: `DslParser`, `DslParserOptions`, `DslParseResult`, `Issue`
- Expression: `IExpressionEngine`, `ExpressionEngine`, `ExpressionContext`, `ExpressionResult`
- Layout: `ILayoutEngine`, `LayoutEngine`, `LayoutPlan`, `LayoutSheet`, `LayoutCell`, `LayoutChart`
- Styles: `IStyleResolver`, `StyleResolver`, `StylePlan`, `ResolvedStyle`
- Worksheet state: `IWorksheetStateBuilder`, `WorksheetStateBuilder`, `WorksheetState`, `CellState`, `ChartState`
- Rendering: `IRenderer`, `XlsxRenderer`, `RenderOptions`, `RenderResult`, `RenderProgressInfo`
- Logging: `IReportLogger`, `ReportLogger`, `LogEntry`, `LogLevel`, `ReportPhase`

## Project Structure

```text
.
├── ExcelReport.sln
├── ExcelReport/
│   ├── ExcelReportLib/
│   │   ├── DSL/
│   │   ├── ExpressionEngine/
│   │   ├── LayoutEngine/
│   │   ├── Styles/
│   │   ├── WorksheetState/
│   │   ├── Renderer/
│   │   ├── ReportGenerator.cs
│   │   └── AsyncReportGenerator.cs
│   └── ExcelReportLib.Tests/
│       ├── DslParserTests.cs
│       ├── LayoutEngineTests.cs
│       ├── RendererTests.cs
│       ├── ReportGeneratorTests.cs
│       ├── AsyncReportGeneratorTests.cs
│       └── ...
└── reports/
```

## Testing

Run all tests:

```bash
dotnet test ExcelReport.sln
```

Run only library tests:

```bash
dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj
```

Run with coverage collector:

```bash
dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --collect:"XPlat Code Coverage"
```

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
