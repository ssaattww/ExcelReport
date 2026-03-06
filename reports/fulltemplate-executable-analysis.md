# FullTemplate XML 実行可能性調査

## 結論
`Design/DslDefinition/DslDefinition_FullTemplate_Sample_v1.xml` は、**現在の `ReportGenerator` 実装ではそのまま正しく実行できません**。

主な阻害要因は次の4点です。
1. `ReportGenerator` が `ParseFromText` 固定で、DSLファイル基準パス (`RootFilePath`) を渡せないため、`styleImport`/`componentImport` の相対パス解決が呼び出し側 `cwd` 依存。
2. `componentImport` は AST 解析されるが、`LayoutEngine` が `workbook.Components` のみを component index に積むため、外部 component が展開されない。
3. `sheetOptions` の `at="DetailHeader"` / `at="DetailRows"` のような `use instance` / `repeat name` 解決先が、実行時の named area に変換されないため `freeze`/`groupRows`/`autoFilter` が効かない。
4. `formulaRef` と `#{...}` 置換の実装が見当たらず、式文字列がそのまま出力される。

---

## 1. 外部ファイル存在確認

### 1-1. Design 配下
- `Design/DslDefinition/DslDefinition_FullTemplate_Sample_v1.xml`
- `Design/DslDefinition/DslDefinition_FullTemplate_SampleExternalStyle_v1.xml`
- `Design/DslDefinition/DslDefinition_FullTemplate_SampleExternalComponent_v1.xml`

### 1-2. TestDsl 配下
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_Sample_v1.xml`
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_SampleExternalStyle_v1.xml`
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_SampleExternalComponent_v1.xml`

`Design` と `TestDsl` の同名3ファイルは `diff` で差分なしでした。

---

## 2. FullTemplate で使用している機能の実装状況

調査対象実装は、ユーザー指定の `ExcelReport/Engine` 相当として `ExcelReport/ExcelReportLib` 配下を確認しました。

| 機能 | 判定 | 根拠 |
|---|---|---|
| `use` | **部分実装** | `UseAst` と展開処理は存在。ただし外部 component は `LayoutEngine` の index 対象外。`LayoutEngine.cs:31`, `LayoutEngine.cs:494-511`, `DslParser.cs:315-347` |
| `repeat` | **実装済み** | `RepeatAst` 解析と `ExpandRepeat` 展開あり。`RepeatAst.cs`, `LayoutEngine.cs:231-307` |
| `cell` | **実装済み** | `CellAst`、値/式評価、レンダリングあり。`CellAst.cs`, `LayoutEngine.cs:140-176`, `XlsxRenderer.cs:256-329` |
| `styleRef` | **実装済み** | AST/解決/適用あり。`StyleRefAst.cs`, `DslParser.cs`, `StyleResolver.cs:243-276` |
| `style` (inline) | **実装済み** | `LayoutNodeAst` で取得、`StyleResolver.BuildPlan` でマージ。`LayoutNodeAst.cs:44-55`, `StyleResolver.cs:66-188` |
| `sheetOptions` | **部分実装** | AST/検証/Renderer 経路はあるが、`at=instance/repeat名` を実座標化する実装なし。`SheetOptionsAst.cs`, `DslParser.cs:532-600`, `WorksheetStateBuilder.cs:113-129` |
| `freeze` | **部分実装** | `A1` 直接指定は可。`at="DetailHeader"` のような名前参照は named area 不在で未反映。`XlsxRenderer.cs:160-170`, `451-476` |
| `groups` | **部分実装** | 解析・状態保持あり。実適用は row/col range or named area 前提。`SheetOptionsAst.cs`, `XlsxRenderer.cs:197-254`, `493-514` |
| `groupRows` | **部分実装** | 実適用は `1:10` 形式のみ確実。`at="DetailRows"` は named area 未生成で効かない。`XlsxRenderer.cs:227-236` |
| `autoFilter` | **部分実装** | `A1:C1` or named area は可。`at="DetailHeader"` は named area 不在で未反映。`XlsxRenderer.cs:331-340`, `478-491` |
| `formulaRef` | **未実装** | `CellAst` で保持されるが範囲解決/展開ロジックが存在しない。`CellAst.cs`, `LayoutCell.cs`, `WorksheetState/CellState.cs` |
| `#{...}` formula placeholder | **未実装** | 式処理は先頭 `=` を外すのみでプレースホルダ置換なし。`XlsxRenderer.cs:441-449` |
| `styleImport` | **実装済み** | 外部 style 読込と再帰 index あり。`StyleImportAst.cs:18-72`, `StyleResolver.cs:191-215` |
| `componentImport` | **部分実装** | 読込は可能だが layout 展開に未接続。`ComponentImportAst.cs:23-84`, `LayoutEngine.cs:31`, `494-511` |

補足:
- FullTemplate は workbook 側と external component 側の両方で同じ style ファイルを import しており、`DuplicateStyleName` エラーが発生します（`DslParser.BuildStyleIndex` の重複検出）。

---

## 3. 既存テストでの FullTemplate 使用状況

### 3-1. FullTemplate ファイルを直接使うテスト
- `DslParserTests` (`DslParser.ParseFromText` + fixture path)
- `WorkbookAstTests`
- `SheetAstTests`
- `LayoutNodeTests`
- `ComponentImportTests`

いずれも **AST/パーサ寄りの検証** で、`ReportGenerator` での end-to-end 実行ではありません。

### 3-2. ReportGenerator の FullTemplate テスト
- `ReportGeneratorTests.Generate_FullTemplateSample_ProducesValidXlsx` は存在。
- ただしこれは **インライン DSL** で、`styleImport` / `componentImport` / `sheetOptions` を使っていません。

### 3-3. `sheetOptions` のテスト
- `SheetAstTests` は `freeze/groupRows/autoFilter` の **解析確認**。
- `RendererTests` / `WorksheetStateTests` は `freeze` 等を確認するが、主に直接座標・直接 state での検証。
- FullTemplate の `at="DetailHeader"` / `at="DetailRows"` の実効確認は未カバー。

---

## 4. ReportGenerator 実行時の不足要素・未実装部分

### 4-1. 実行検証結果（手動実行）
`ReportGenerator.Generate` を使った最小検証を実施。

- FullTemplate を `repo root` から実行:
  - `LoadFile` エラー（外部 import 相対パス解決失敗）
  - `UndefinedComponent` / `UndefinedStyle` 多数
- FullTemplate を DSL 配下 `cwd` で実行:
  - import 自体は読まれるが `DuplicateStyleName` + `UndefinedComponent` 発生
- import 不要の最小 inline DSL で `sheetOptions` + `formulaRef` を検証:
  - `issues=0` でも `freeze/autoFilter/groupRows` は出力に反映されず
  - `A10` の式は `SUM(#{Detail.Value:Detail.ValueEnd})` のまま（置換なし）

### 4-2. 不足要素リスト
1. `ReportGenerator` に `ParseFromFile` / `RootFilePath` 指定経路がない。
2. `LayoutEngine` の component index に `ComponentImportAst` を含める処理がない。
3. `sheetOptions at` の target 名 (`use instance` / `repeat name`) を named area へマッピングする処理がない。
4. `groupRows` が名前ターゲットを解決できず、実質 row range 文字列依存。
5. `formulaRef` 系（`#{Name:NameEnd}` 置換・連続範囲解決）が未実装。
6. （仕様判断事項）同一 style の多重 import を重複エラーにする現在挙動が FullTemplate と相性不一致。

---

## 5. サンプルJSONデータと期待データ形状

### 5-1. XML が期待する形状
- `root.JobName`
- `root.Summary.Owner`
- `root.Summary.SuccessRate`
- `root.Lines[]` 各要素に `Name`, `Value`, `Code`

参照箇所:
- `DslDefinition_FullTemplate_Sample_v1.xml:14,17,37-41`
- `DslDefinition_FullTemplate_SampleExternalComponent_v1.xml:13,22,25,47,50,53`

### 5-2. 実在するサンプルデータ
- C# 型サンプルあり:
  - `Design/DslDefinition/DslDefinition_FullTemplate_SampleInput_v1.cs`
  - `ExcelReport/ExcelReportExe/SampleData.cs`
- テスト内匿名型データあり:
  - `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs:268-281`

### 5-3. JSON ファイル有無
- リポジトリ内に FullTemplate 用 JSON サンプルファイルは見当たりませんでした（`*.json` 該当なし）。

### 5-4. 式エンジン側の解決仕様
- ルート識別子は `root` / `data` / `vars` のみ。
- メンバは public property/field と Dictionary キーを解決可能。

根拠: `ExpressionEngine.cs:104-115`, `117-139`

---

## 付記（調査で確認したパス）
- DSL本体: `Design/DslDefinition/DslDefinition_FullTemplate_Sample_v1.xml`
- テストDSL: `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_Sample_v1.xml`
- コア実装: `ExcelReport/ExcelReportLib/*`
- テスト実装: `ExcelReport/ExcelReportLib.Tests/*`
