# Task 7: LayoutEngine

## Scope

- Implemented `ExcelReport/ExcelReportLib/LayoutEngine/ILayoutEngine.cs`
- Implemented `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs`
- Implemented `ExcelReport/ExcelReportLib/LayoutEngine/LayoutPlan.cs`
- Implemented `ExcelReport/ExcelReportLib/LayoutEngine/LayoutCell.cs`
- Implemented `ExcelReport/ExcelReportLib/LayoutEngine/LayoutSheet.cs`
- Added `ExcelReport/ExcelReportLib.Tests/LayoutEngineTests.cs`

## Implemented Behavior

- Expands workbook sheets into `LayoutPlan` / `LayoutSheet` / `LayoutCell`
- Recursively expands `cell`, `grid`, `repeat`, and `use`
- Evaluates `when`, `repeat from`, `use with`, and cell expressions
- Binds repeat variables and supports direct variable expressions such as `@(it)` and `@(it.Name)`
- Resolves components from workbook-level component definitions
- Resolves final cell styles through `StyleResolver.BuildPlan`
- Computes absolute 1-based cell coordinates from relative placements
- Validates generated cell coordinates against the declared sheet bounds

## Validation

### Library Build

Validated with:

```bash
dotnet msbuild ExcelReport/ExcelReportLib/ExcelReportLib.csproj /t:Restore /p:TargetFramework=net8.0
dotnet msbuild ExcelReport/ExcelReportLib/ExcelReportLib.csproj /t:Build /p:TargetFramework=net8.0
```

Result:

- Build succeeded
- Existing nullable warnings remain in pre-existing AST files

### Scenario Validation

Because the workspace only has .NET SDK 8.0 and the solution targets `net10.0`, and because test packages were not available offline from NuGet, the xUnit project could not be restored/executed directly in this environment.

To validate behavior, a temporary `net8.0` console harness under `/tmp/LayoutEngineSmoke` referenced the built `ExcelReportLib.dll` and executed the six requested scenarios:

- `Expand_SingleCell_ProducesLayoutCell`
- `Expand_Grid_ChildrenPositioned`
- `Expand_Repeat_ExpandsCollection`
- `Expand_Use_ResolvesComponent`
- `Expand_WhenFalse_SkipsNode`
- `Expand_NestedRepeatGrid_CorrectPositions`

Result:

- All six smoke checks passed

## Environment Constraints

- Direct `dotnet test` was not runnable as-is because:
  - installed SDK: `8.0.416`
  - project target: `net10.0`
  - offline restore could not resolve `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, and `coverlet.collector`
