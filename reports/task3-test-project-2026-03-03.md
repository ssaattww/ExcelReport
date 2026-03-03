# Task 3: DslParser単体テストプロジェクト新設

## 作成したファイル一覧

- `ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj`
- `ExcelReport/ExcelReportLib.Tests/DslTestFixtures.cs`
- `ExcelReport/ExcelReportLib.Tests/DslParserTests.cs`
- `ExcelReport/ExcelReportLib.Tests/WorkbookAstTests.cs`
- `ExcelReport/ExcelReportLib.Tests/SheetAstTests.cs`
- `ExcelReport/ExcelReportLib.Tests/LayoutNodeTests.cs`
- `ExcelReport/ExcelReportLib.Tests/StyleAstTests.cs`
- `ExcelReport/ExcelReportLib.Tests/ComponentImportTests.cs`
- `reports/task3-test-project-2026-03-03.md`

## テストケース一覧と目的

### DslParserTests

- `ParseFromText_ValidXml_ReturnsWorkbookAst`
  - フルテンプレートXMLを `DslParser.ParseFromText` で読み込み、Fatalなしで `WorkbookAst` が生成されることを確認する。
- `ParseFromText_InvalidXml_ReturnsFatalIssue`
  - 不正なXML文字列を渡したときに `IssueKind.XmlMalformed` の Fatal が返り、`Root` が `null` になることを確認する。
- `ParseFromText_EmptyInput_ReturnsFatalIssue`
  - 空文字列入力時に Fatal のXMLパースエラーが返ることを確認する。

### WorkbookAstTests

- `Parse_FullTemplate_HasExpectedSheets`
  - `WorkbookAst` がシート定義を読み込み、`Summary` シートを保持することを確認する。
- `Parse_FullTemplate_HasStyles`
  - ルート `styles` から外部スタイル定義をたどり、期待するスタイル群を取得できることを確認する。
- `Parse_FullTemplate_HasComponents`
  - `componentImport` 経由で外部コンポーネント定義を読み込み、期待するコンポーネント名が存在することを確認する。

### SheetAstTests

- `Parse_Sheet_HasRowsAndCols`
  - `sheet` 要素の `rows` / `cols` 属性が正しくパースされることを確認する。
- `Parse_Sheet_HasLayoutNodes`
  - シート直下のレイアウトノード数と主要ノード種別（`cell`, `repeat`）が取得できることを確認する。
- `Parse_Sheet_HasSheetOptions`
  - `sheetOptions` 配下の `freeze`, `groupRows`, `autoFilter` が正しく取得できることを確認する。

### LayoutNodeTests

- `Parse_Cell_HasStyleRefAndFormulaRef`
  - `cell` の `formulaRef` 属性と入れ子の `styleRef` が正しく取得できることを確認する。
- `Parse_Repeat_HasFromExprRaw`
  - `repeat` の `from` 属性文字列が `FromExprRaw` に保持されることを確認する。
- `Parse_Use_HasInstanceAttribute`
  - `use` の `instance` 属性が `InstanceName` として取得できることを確認する。
- `Parse_Grid_ChildNodes`
  - `grid` 直下の子ノードが期待数だけ `Children` に格納されることを確認する。

### StyleAstTests

- `Parse_Style_HasBorders`
  - `style` 配下の `border` 要素が `BorderInfo` として直接パースされることを確認する。
- `Parse_Style_HasScope`
  - `style` の `scope` 属性が `StyleScope` に変換されることを確認する。

### ComponentImportTests

- `Parse_ComponentImport_LoadsExternalFile`
  - `componentImport` が外部ファイルを解決し、コンポーネント定義を読み込めることを確認する。
- `Parse_ComponentImport_HasStyles`
  - 外部コンポーネントファイル内の `styles` が取得でき、さらに `styleImport` 先のスタイル群まで参照できることを確認する。

## テストフィクスチャへのパス解決方法

- フィクスチャの実体は `ExcelReport/ExcelReportLibTest/TestDsl/` の既存XMLをそのまま利用する。
- `ExcelReport/ExcelReportLib.Tests/DslTestFixtures.cs` で、`[CallerFilePath]` を使ってテストプロジェクトのディレクトリを特定する。
- そのディレクトリを基準に `../ExcelReportLibTest/TestDsl` を連結し、絶対パスへ正規化してフィクスチャディレクトリを求める。
- 各テストは `DslTestFixtures.GetPath(...)` / `ReadText(...)` / `LoadDocument(...)` を通して参照し、XMLの複製は行わない。
- `DslParser.ParseFromText` と `WorkbookAst` / `ComponentImportAst` の外部参照解決では、フィクスチャの絶対パスまたはその親ディレクトリを渡して相対 `href` を解決する。
