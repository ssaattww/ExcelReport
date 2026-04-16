# Issue #61 Review Round 1

- Date: 2026-04-16
- Scope: `git diff --` (working tree changes on current branch)
- Verdict: 2 findings

## Findings (severity order)

1. **[Medium] `from` あり `var` 省略時の既定変数 `item` が正規化スコープに反映されず、`@item` が誤って `@(root.Item)` に変換される**
- File: `ExcelReport/ExcelReportLib/ExcelTemplate/ExcelTemplateOutputContractBuilder.cs:148`
- File: `ExcelReport/ExcelReportLib/ExcelTemplate/ExcelTemplateOutputContractBuilder.cs:149`
- File: `ExcelReport/ExcelReportLib/ExcelTemplate/ExcelTemplateOutputContractBuilder.cs:150`
- Detail: `BuildSheets` は `definition.VariableName` が null の場合に `EmptyVariableNames` を使うため、シート repeat の暗黙変数が存在しない前提でセル値正規化が走る。`@item` のような simple shorthand は `ExcelTemplateExpressionNormalizer` で `@(root.Item)` に変換され、シート repeat 文脈のローカル変数参照にならない。
- Spec mismatch evidence: DSL 側はシート repeat の `var` 省略時に既定値 `item` を使う実装 (`ExcelReport/ExcelReportLib/DSL/AST/SheetAst.cs:37`, `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:87`)。
- Risk: workbook meta で `from` は指定するが `var` を省略したテンプレートに `@item` が含まれると、期待値と異なる式が出力される（値不正または評価失敗）。

2. **[Low] 上記回帰を捕捉するテストケースが不足している**
- File: `ExcelReport/ExcelReportLib.Tests/ExcelTemplateOutputContractBuilderTests.cs:281`
- File: `ExcelReport/ExcelReportLib.Tests/ExcelTemplateOutputContractBuilderTests.cs:355`
- Detail: 追加テストは `from+var` 正常系と `var without from` 異常系は網羅しているが、`from` のみ（`var` 省略）で `@item` shorthand がローカル変数として正規化されるべきケースがない。今回の不整合は現テスト群では検知できない。

## Verification Notes

- Ran:
  - `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter "FullyQualifiedName~ExcelTemplateOutputContractBuilderTests|FullyQualifiedName~ExcelTemplateConverterTests|FullyQualifiedName~ExcelTemplateEndToEndTests|FullyQualifiedName~ExcelTemplateExtractorTests|FullyQualifiedName~XmlTemplateSerializerTests"`
- Result: Passed 27 / Failed 0 / Skipped 0
