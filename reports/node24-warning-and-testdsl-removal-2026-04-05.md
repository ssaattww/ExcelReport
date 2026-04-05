# Node24 Warning 対応 + TestDsl整理

Date: 2026-04-05
Branch: feature/issue-43-async-api

## Request

- GitHub Actions の Node.js 20 deprecation warning に対応
- `ExcelReportLibTest/TestDsl` を削除
- 作業前に `master` を同期

## Sync

- `origin/master` を fetch 後、現在ブランチへ merge 実施
- merge base 更新後に作業開始

## Implemented Changes

1. Workflow 対応
- `.github/workflows/pr-xunit-tests.yml`
- `.github/workflows/publish-nuget.yml`

両 workflow に以下を追加:
- `FORCE_JAVASCRIPT_ACTIONS_TO_NODE24: "true"`

2. TestDsl 移設と旧パス削除
- 削除: `ExcelReport/ExcelReportLibTest/TestDsl`
- 移設先: `ExcelReport/ExcelReportLib.Tests/TestDsl`
  - `DslDefinition_FullTemplate_Sample_v2.xml`
  - `DslDefinition_FullTemplate_SampleExternalComponent_v2.xml`
  - `DslDefinition_FullTemplate_SampleExternalStyle_v2.xml`
  - `DslDefinition_v2.xsd`

3. 参照更新
- `ExcelReport/ExcelReportLib.Tests/DslTestFixtures.cs`
  - fixture 基準パスを `ExcelReportLib.Tests/TestDsl` に変更
- `ExcelReport/ExcelReportExe/Program.cs`
  - サンプルコメントの fixture パスを新パスへ更新

## Verification

- 実行コマンド:
  - `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --configuration Release --no-restore --verbosity minimal`
- 結果:
  - `Passed: 198, Failed: 0, Skipped: 0`

## Notes

- ビルド時の nullable 警告（既存）は継続して出力されるが、今回変更に起因する失敗はなし
