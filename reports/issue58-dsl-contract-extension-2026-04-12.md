# issue #58 DSL契約拡張 実装記録

- 作成日: 2026-04-12
- 追記日: 2026-04-13
- 対象: issue #58 ExcelTemplate 対応の実装第1段
- ブランチ: `codex/create-design-document-for-approval`

## 1. 実施内容

issue #58 の実装着手として、ExcelTemplate 変換器より先に必要な DSL 契約拡張を実装した。

今回反映した内容:

- `cell@formula` を runtime schema / test fixture schema / AST / LayoutEngine に追加
- `use@styleOverflow="none|edge"` を runtime schema / test fixture schema / AST / LayoutEngine に追加
- `DslParser.ValidateDsl` に no-schema mode 用の補完検証を追加
  - `<cell>` の `value` と `formula` の同時指定を Error 化
  - `<use>` の `styleOverflow` 不正値を Error 化
- `IssueKind.TemplateRangeOverflow` を追加
- `LayoutEngine` で `styleOverflow=edge` の post-expand 補完を追加
  - `use` の seed 書式を anchor 矩形の style-only `LayoutCell` として保持
  - right / down / corner の trailing edge copy を実装
  - legacy DSL への Warning ノイズを避けるため、overflow 追跡は ExcelTemplate 向けアンカー情報がある `use` に限定
- `cell@formula` は `SUM(A1:A3)` のような `=` なし入力も受け付け、runtime では `=SUM(A1:A3)` に正規化
- 既存互換として `cell@value="=..."` は従来どおり数式扱いを維持

2026-04-13 追記:

- `styleOverflow=edge` の runtime テストを right に加えて down / right-down corner まで拡張した
- review 前に 3方向の補完挙動を unit test で固定した

## 2. 変更ファイル

- `Design/DslDefinition/DslDefinition_v2.xsd`
- `ExcelReport/ExcelReportLib.Tests/TestDsl/DslDefinition_v2.xsd`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/CellAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/UseAst.cs`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs`
- `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs`
- `ExcelReport/ExcelReportLib.Tests/LayoutNodeTests.cs`
- `ExcelReport/ExcelReportLib.Tests/ValidateDslTests.cs`
- `ExcelReport/ExcelReportLib.Tests/DslParserTests.cs`
- `ExcelReport/ExcelReportLib.Tests/LayoutEngineTests.cs`

## 3. テスト

実施:

- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter "FullyQualifiedName~LayoutNodeTests|FullyQualifiedName~ValidateDslTests|FullyQualifiedName~DslParserTests|FullyQualifiedName~LayoutEngineTests"`
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj`

結果:

- 対象テスト: 72 passed
- 全体テスト: 216 passed, 0 failed

## 4. 次の実装候補

次は handover と設計 13 章どおり、ExcelTemplate 側の中間モデル / extractor / validator / emitter へ進める。
ただしその前に、今回追加した `styleOverflow=edge` の right/down/corner 実装を基準として、ExcelTemplate converter が `use` の anchor range と seed style をどう DSL へ落とすかを具体化してから進むのが安全。
