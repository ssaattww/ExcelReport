# ExcelReport 実装インベントリ調査レポート

- 調査日: 2026-02-13
- 対象: `ExcelReport/`
- 目的: 実装済み範囲と未実装範囲をモジュール単位で可視化する

## 1. 調査対象ファイル（非生成物）

- ソリューション/プロジェクト:
- `ExcelReport/ExcelReport.slnx`
- `ExcelReport/ExcelReportLib/ExcelReportLib.csproj`
- `ExcelReport/ExcelReportExe/ExcelReportExe.csproj`
- 実装コード:
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/**/*.cs`
- 実行サンプル:
- `ExcelReport/ExcelReportExe/Program.cs`
- `ExcelReport/ExcelReportExe/SampleData.cs`
- DSL検証用素材:
- `ExcelReport/ExcelReportLibTest/TestDsl/*.xml`
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_v1.xsd`

## 2. 実装状況サマリ（モジュール別）

| モジュール | 状態 | 根拠 |
|---|---|---|
| DslParser | 実装済み（部分） | `ParseFromFile/Text/Stream`、参照解決、Issueモデルあり |
| DSL AST | 実装済み（部分） | `WorkbookAst`/`SheetAst`/`CellAst`/`UseAst`/`RepeatAst` 等あり |
| DslDefinition(XSD)運用 | 部分実装 | XSDファイルは存在するが、検証呼び出しは無効化 |
| ExpressionEngine | 未実装 | コード上に実装クラス/IFなし |
| LayoutEngine | 未実装 | 実装クラスなし、`csproj`にフォルダ宣言のみ |
| Styles（Resolver） | 未実装（ASTはあり） | `StyleAst`等ASTはあるが Resolver/Plan 実装なし |
| WorksheetState | 未実装 | 実装クラス/IFなし |
| Renderer | 未実装 | 実装クラス/IFなし |
| Logger | 未実装 | 実装クラス/IFなし |
| ReportGenerator | 未実装 | 実装クラス/IFなし |

## 3. 実装済み範囲の詳細

### 3.1 DslParserエントリポイント

- `public static class DslParser` として公開
- `ParseFromFile`, `ParseFromText`, `ParseFromStream` を提供
- XML不正時は `IssueKind.XmlMalformed` を `Fatal` で返却

証跡:
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:11`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:13`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:19`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:26`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:41`

### 3.2 参照解決とIssue定義

- `ResolveStyleRefs`、`ResolveComponentRefs` を実装
- `IssueSeverity`/`IssueKind`/`Issue`/`DslParseResult` を同一ファイルで定義

証跡:
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:66`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:81`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:130`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:326`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:334`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:370`

### 3.3 AST実装の中心点

- ルート/構造: `WorkbookAst`, `SheetAst`, `ComponentAst`, `StylesAst`
- レイアウト: `LayoutNodeAst`, `GridAst`, `CellAst`, `UseAst`, `RepeatAst`
- SheetOptions: `FreezeAst`, `GroupRowsAst`, `GroupColsAst`, `AutoFilterAst`
- import: `StyleImportAst`, `ComponentImportAst`

証跡:
- `ExcelReport/ExcelReportLib/DSL/AST/WorkBookAst.cs:8`
- `ExcelReport/ExcelReportLib/DSL/AST/SheetAst.cs:9`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/LayoutNodeAst.cs:8`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/UseAst.cs:11`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/RepeatAst.cs:11`
- `ExcelReport/ExcelReportLib/DSL/AST/SheetOptionsAst.cs:11`
- `ExcelReport/ExcelReportLib/DSL/AST/StyleImportAst.cs:9`
- `ExcelReport/ExcelReportLib/DSL/AST/ComponentImportAst.cs:11`

## 4. 部分実装・未実装ポイント

### 4.1 XSD検証はオプション定義のみ（実行無効）

- `EnableSchemaValidation` は存在
- 検証呼び出しと `ValidateWithSchema` 本体はコメントアウト

証跡:
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:47`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:282`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:318`

### 4.2 DSL固有検証は未実装スタブ

- `ValidateDsl` は空実装

証跡:
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs:308`

### 4.3 レンダリング系モジュールは不在

- `ExcelReportLib` 配下は `DSL` ディレクトリのみ
- `LayoutEngine` 実装ファイルは存在せず、`csproj` の `<Folder Include="LayoutEngine\" />` のみ

証跡:
- `ExcelReport/ExcelReportLib/ExcelReportLib.csproj:10`
- `ExcelReport/ExcelReportLib/DSL/`

### 4.4 サンプル実行はローカル絶対パス依存

- `Program.cs` は Windows絶対パス固定でDSLを読むサンプル

証跡:
- `ExcelReport/ExcelReportExe/Program.cs:16`

## 5. テスト実装状況

- `ExcelReportLibTest` は DSL素材（XSD/XML）のみで、テストコード（`*Test.cs`）は未確認
- 現状は「入力サンプル資産あり、テストハーネス未整備」の状態

証跡:
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_v1.xsd`
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_Sample_v1.xml`

## 6. フェーズ2結果要約

1. 実装の中心は DslParser + AST。
2. パイプライン後段（ExpressionEngine / LayoutEngine / WorksheetState / Renderer / Logger / ReportGenerator）は未実装。
3. 設計全体に対して実装率は初期段階で、整合性評価は DslDefinition/DslParser 周辺が主戦場になる。
