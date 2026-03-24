# NuGet Package README Fix Investigation (2026-03-24)

## Background

NuGet publish flow (triggered by GitHub Release) reported that the package does not include a README.

Reference:
- https://devblogs.microsoft.com/dotnet/add-a-readme-to-your-nuget-package/#add-a-readme-to-your-package

## Current State

- `ExcelReport/ExcelReportLib/ExcelReportLib.csproj` had package metadata (`PackageId`, `Description`, license, tags) but no `PackageReadmeFile`.
- No README file was packed into the `.nupkg` root.
- Release workflow `/.github/workflows/publish-nuget.yml` already executes `dotnet pack` and `dotnet nuget push` correctly.

## Root Cause

NuGet requires both:
1. `PackageReadmeFile` metadata in the project file, and
2. the referenced README file to be included in the package content.

Both were missing from the library project.

## Fix

- Added `<PackageReadmeFile>README.md</PackageReadmeFile>` to `ExcelReportLib.csproj`.
- Added `<None Include="README.md" Pack="true" PackagePath="\" />` so README is packed into nupkg root.
- Added new `ExcelReport/ExcelReportLib/README.md` as package-facing documentation.

## Validation

- Executed `dotnet pack ExcelReport/ExcelReportLib/ExcelReportLib.csproj --configuration Release --output artifacts-local/nuget-readme-check`.
- Confirmed package entries contain `README.md` at nupkg root.
- Confirmed generated `ExcelReportLib.nuspec` contains `<readme>README.md</readme>`.

- Run `dotnet pack` for `ExcelReportLib.csproj`.
- Inspect generated `.nupkg` to confirm:
  - `README.md` exists in package root.
  - nuspec contains `<readme>README.md</readme>`.

