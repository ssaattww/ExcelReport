# ExcelReport Project Survey (2026-03-03)

作成日: 2026-03-03  
対象リポジトリ: `/home/ibis/dotnet_ws/ExcelReport`

## 1. 調査対象と結論

本調査では、以下を全件読了した。

- `ExcelReport/` 配下の非生成ソースコードと DSL テスト素材
- `Design/` 配下の設計ドキュメントと DSL 定義素材
- `reports/` 配下の全 Markdown レポート
- `.sln` / `.slnx` / `.csproj`
- 設定ファイル相当（`.gitignore`, `.claude/settings.local.json`）

結論として、ExcelReport は「C# オブジェクトを入力に Excel (OOXML) レポートを生成するためのテンプレート駆動ライブラリ」を目指した設計であり、現時点の実装は `DslParser + AST + サンプル実行コード` に強く偏っている。設計書は広範に整備されている一方で、実装は初期段階であり、パイプライン後段（ExpressionEngine, Styles resolver, LayoutEngine, WorksheetState, Renderer, Logger, ReportGenerator）は未実装である。

## 2. ExcelReport の目的と全体像

`Design/BasicDesign_v1.md` と各詳細設計書の記述を総合すると、ExcelReport の想定アーキテクチャは次の流れである。

1. `DslParser` が XML DSL を AST に変換し、構文・参照・静的妥当性を検証する。
2. `ExpressionEngine` が `@(...)` の C# 式を評価する。
3. `Styles` がスタイル定義を集約し、`LayoutEngine` にスタイル候補（StylePlan）を渡す。
4. `LayoutEngine` が `repeat/use/grid/cell` を展開し、最終スタイル込みの論理レイアウトを作る。
5. `WorksheetState` が出力直前の最終状態（セル、結合、名前付き領域、数式系列、シート設定）を固定化する。
6. `Renderer` が `.xlsx` に物理出力し、Issues シートと監査シートも生成する。
7. `Logger` が進捗・監査ログを横断管理する。
8. `ReportGenerator` が上記全体を統合するトップレベル・ファサードになる。

設計の中心思想は、「判断はできるだけ上流で終わらせ、下流は機械的に投影する」ことである。特に、スタイル優先順位の最終決定は `LayoutEngine`、最終状態の固定化は `WorksheetState`、物理出力は `Renderer` に責務分離されている。

## 3. プロジェクト構造

### 3.1 主要ディレクトリ

- `.claude/`: Claude/Codex ワークフロー設定
- `Design/`: ExcelReport の基本設計・詳細設計・DSL 定義
- `ExcelReport/`: 実際の .NET ソリューション本体
- `reports/`: 調査レポート群（ExcelReport 関連とワークフロー関連が混在）
- `tasks/`: ワークフロー運用タスク管理
- `claude-code-workflows/`: 別系統のワークフロー参照実装

ExcelReport 本体としては、実質的に `Design/` と `ExcelReport/` が中核であり、他はワークフロー運用資産である。

### 3.2 ソリューション/プロジェクト

- `ExcelReport.sln`
- `ExcelReport/ExcelReport.slnx`
- `ExcelReport/ExcelReportLib/ExcelReportLib.csproj`
- `ExcelReport/ExcelReportExe/ExcelReportExe.csproj`

### 3.3 実装コード一覧

- `ExcelReport/ExcelReportExe/Program.cs`
- `ExcelReport/ExcelReportExe/SampleData.cs`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/IAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/ICellAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/Common.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/WorkBookAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/SheetAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/SheetOptionsAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/StylesAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/StyleAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/StyleRefAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/StyleImportAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/ComponentAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/ComponentsAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/ComponentImportAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/LayoutNodeAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/CellAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/GridAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/RepeatAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/UseAst.cs`

### 3.4 DSL 定義/素材一覧

- `Design/BasicDesign_v1.md`
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md`
- `Design/DslDefinition/DslDefinition_v1.xsd`
- `Design/DslDefinition/DslDefinition_FullTemplate_Sample_v1.xml`
- `Design/DslDefinition/DslDefinition_FullTemplate_SampleExternalComponent_v1.xml`
- `Design/DslDefinition/DslDefinition_FullTemplate_SampleExternalStyle_v1.xml`
- `Design/DslDefinition/DslDefinition_FullTemplate_SampleInput_v1.cs`
- `Design/DslParser/DslParser_DetailDesign_v1.md`
- `Design/ExpressionEngine/ExpressionEngine.md`
- `Design/LayoutEngine/LayoutEngine.md`
- `Design/Styles/Styles_DetailDesign.md`
- `Design/WorkSheetState/WorksheetState_DetailDesign.md`
- `Design/Renderer/Renderer_DetailDesign.md`
- `Design/Logger/Logger_DetailDesign.md`
- `Design/ReportGenerator/ReportGenerator_DetailDesign.md`

### 3.5 テスト関連ファイル一覧

`ExcelReport/ExcelReportLibTest` はテストコードではなく、DSL フィクスチャ置き場になっている。

- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_v1.xsd`
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_Sample_v1.xml`
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_SampleExternalComponent_v1.xml`
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_SampleExternalStyle_v1.xml`

### 3.6 `reports/` 一覧

- `reports/design-validity-investigation-2026-02-08.md`
- `reports/implementation-inventory-2026-02-13.md`
- `reports/design-inventory-2026-02-13.md`
- `reports/design-implementation-alignment-2026-02-13.md`
- `reports/issues-and-improvements-2026-02-13.md`
- `reports/design-revision-strategy-2026-02-13.md`
- `reports/workflow-integration-analysis.md`
- `reports/integration-implementation-plan.md`
- `reports/phase1-current-status-2026-02-13.md`
- `reports/phase1-implementation-strategy.md`
- `reports/phase2-skill-gap-analysis-2026-02-13.md`
- `reports/phase2-coverage-matrix-2026-02-17.md`
- `reports/claude-code-workflows-stop-approval-investigation-2026-02-17.md`
- `reports/claude-code-workflows-quality-gate-investigation-2026-02-17.md`
- `reports/sandbox-matrix-investigation-phase-analysis-2026-02-17.md`
- `reports/skill-design-guidelines-2026-02-17.md`
- `reports/phase2-reference-sandbox-sync-audit-2026-03-02.md`
- `reports/phase2-task2.21-contract-compliance-verification-2026-03-02.md`
- `reports/phase2-task2.22-stop-approve-resume-verification-2026-03-02.md`
- `reports/phase2-task2.23-quality-gate-verification-2026-03-02.md`
- `reports/phase2-readiness-report-2026-03-02.md`
- `reports/phase3-investigation-2026-03-03.md`
- `reports/phase3-task3.1-validation-2026-03-03.md`
- `reports/phase3-closure-verification-2026-03-03.md`
- `reports/feedback-points-status-review-2026-03-03.md`
- `reports/enforce-status-updates-plan-2026-03-03.md`
- `reports/enforce-status-updates-plan-v2-2026-03-03.md`

## 4. プロジェクトファイルの内容

### 4.1 `ExcelReport.sln`

- ルート solution は `ExcelReportLib` と `ExcelReportExe` の 2 プロジェクトのみを含む。
- テストプロジェクトは solution に存在しない。
- `Debug|Any CPU` / `Release|Any CPU` のみ。

### 4.2 `ExcelReport/ExcelReport.slnx`

- `ExcelReportExe/ExcelReportExe.csproj`
- `ExcelReportLib/ExcelReportLib.csproj`

こちらも 2 プロジェクトのみで、構成は `.sln` と整合している。

### 4.3 `ExcelReportLib.csproj`

- SDK: `Microsoft.NET.Sdk`
- TargetFramework: `net10.0`
- `ImplicitUsings` 有効
- `Nullable` 有効
- `LayoutEngine/` フォルダのみ宣言されているが、実装ファイルは存在しない

### 4.4 `ExcelReportExe.csproj`

- SDK: `Microsoft.NET.Sdk`
- `OutputType=Exe`
- TargetFramework: `net10.0`
- `ExcelReportLib` への `ProjectReference`

### 4.5 ビルド観測

ローカルの .NET 8 SDK では `net10.0` をターゲットにしているため、`dotnet build ExcelReport.sln --disable-build-servers -maxcpucount:1 /p:UseSharedCompilation=false /nr:false` は `NETSDK1045` で失敗した。現状の環境では、.NET 10 SDK がない限り通常ビルドできない。

## 5. 設定ファイルの内容

### 5.1 実在する設定ファイル

- `.gitignore`
- `.claude/settings.local.json`

### 5.2 内容要約

- `.gitignore`
  - Visual Studio 生成物と `bin/obj` を除外
  - `.agents` を除外
  - `.claude` は除外していない
- `.claude/settings.local.json`
  - Claude/Codex 作業用の許可リスト
  - `WebSearch`, `Bash(...)`, `git`, `tmux` などを明示許可
  - アプリケーション実行時の設定ではなく、開発支援ツールのローカル設定

### 5.3 存在しない主要設定ファイル

- `.editorconfig`
- `Directory.Build.props` / `Directory.Build.targets`
- `global.json`
- `NuGet.config`
- `appsettings*.json`
- `*.runsettings`

## 6. 既に実装済みの機能一覧

現在の実装は、設計全体に対してかなり限定的である。実装済みなのは主に DSL の読み取り基盤であり、機能としては次が確認できる。

### 6.1 `DslParser` エントリポイント

- `ParseFromFile`
- `ParseFromText`
- `ParseFromStream`
- XML パース時の `XDocument.Load(..., LoadOptions.SetLineInfo)` による行番号取得
- XML 構文エラーを `IssueSeverity.Fatal` として返却

### 6.2 AST 構築

- `WorkbookAst`: `styles` / `component` / `componentImport` / `sheet` の取り込み
- `StylesAst`: `style` / `styleImport` の取り込み
- `StyleAst`: `font`, `fill`, `numberFormat`, `border` のプロパティ収集
- `ComponentAst`: `name` と本体レイアウトノードの取り込み
- `SheetAst`: `name`, `styleRef`, `sheetOptions`, 直下レイアウトノードの取り込み
- `SheetOptionsAst`: `freeze`, `groupRows`, `groupCols`, `autoFilter` の部分取り込み
- `LayoutNodeAst` 系:
  - `CellAst`
  - `GridAst`
  - `RepeatAst`
  - `UseAst`
- `Placement`: `r`, `c`, `rowSpan`, `colSpan`, `when`
- `SourceSpan`: 行・列情報

### 6.3 参照解決

- `ResolveStyleRefs`
  - ルート `styles`
  - `styleImport` から取り込んだ外部スタイル
  - `sheet` / 各レイアウトノードの子 `<styleRef>`
- `ResolveComponentRefs`
  - ルート `component`
  - `componentImport` から取り込んだ外部 `component`
  - `<use component="...">` の参照解決

### 6.4 サンプル実行コード

- `Program.cs` が DSL をパースし、AST をコンソール出力する
- `SampleData.cs` が入力オブジェクト想定のダミーデータを作る

## 7. テストファイルの有無と内容

### 7.1 テストコードの有無

テストコードは存在しない。

- `*Test*.csproj` / `*Tests*.csproj` は未検出
- `test` 用の `.cs` ファイルは未検出
- solution にテストプロジェクトが含まれていない

### 7.2 テスト素材の内容

`ExcelReport/ExcelReportLibTest/TestDsl/` には次の DSL フィクスチャがある。

- フルテンプレート例 (`workbook`)
- 外部 component 定義例 (`components`)
- 外部 style 定義例 (`styles`)
- XSD

ただし、これらは自動テストに接続されておらず、回帰検知に使われていない。また、`Design/` 配下の同名サンプルと記法が一致していない。

## 8. 設計と実装のギャップ（未実装機能）

設計書と既存レポートを照合すると、ギャップは次の順で大きい。

### 8.1 パイプライン後段が未実装

- `ExpressionEngine`: 未実装
- `Styles` の resolver / StylePlan: 未実装
- `LayoutEngine`: 未実装
- `WorksheetState`: 未実装
- `Renderer`: 未実装
- `Logger`: 未実装
- `ReportGenerator`: 未実装

つまり、現状は「DSL を読む」段階で止まっており、「Excel を生成する」本来の価値はまだ提供できていない。

### 8.2 `DslParser` 内の未実装/未接続

- XSD 検証はオプションだけあり、処理本体がコメントアウトされている
- `ValidateDsl` は空実装
- 式構文検証がない
- `TreatExpressionSyntaxErrorAsFatal` は未使用
- `sheet` の `rows/cols` を保持していない
- `cell@styleRef` ショートカットを読んでいない
- `cell@formulaRef` を読んでいない
- `repeat@from` を `RepeatAst.FromExprRaw` に反映していない
- `componentImport` 内の `<styles>` を保持・解決に接続していない

### 8.3 仕様どおりの検証が未実装

設計で要求されているが未実装の代表例:

- 重複 sheet 名検出
- 座標範囲検証
- `repeat` の構文/意味検証の拡張
- `sheetOptions` の `at` 参照妥当性
- `formulaRef` 系列整合性
- スタイル scope 違反の検出
- 静的レイアウト検証

## 9. `DslParser` 実装の品質評価

総合評価: **土台としては有用だが、現時点では「部分実装・検証未完成」**。

### 9.1 良い点

- 役割が明確で、AST 構築と参照解決の責務分離は読みやすい
- `SourceSpan` により診断情報の基礎がある
- `Issue` / `IssueSeverity` / `IssueKind` のモデルが整理されている
- `ParseFromFile/Text/Stream` の入口が揃っている
- 外部 style/component 読み込みの入口自体は用意されている

### 9.2 問題点（実装品質）

- `ValidateDsl` が空実装で、品質保証の核が存在しない
- `EnableSchemaValidation=true` が既定なのに実際には無効化されており、API 契約と実態がずれている
- `RepeatAst` が `from` 属性を読んでおらず、`FromExprRaw` が常に空になる
- `CellAst` が `styleRef` / `formulaRef` を読んでおらず、プロパティが死んでいる
- `ComponentImportAst.Styles` が未設定で、設計上の外部 component 側 style 取り込みが未完成
- `SheetOptionsAst` は `groupCols` を `<groups>` 配下ではなく直下から読んでおり、設計どおりの XML に対して取りこぼす
- `SheetAst` / `GridAst` は重複座標時に `ToDictionary` で例外化しうるが、`Issue` として制御されない
- `StyleRefAst.StyleRef` と `UseAst.ComponentRef` は解析後前提の代入であり、未解決時に後段で null 参照リスクがある
- `ParseFromFile` に `DslParserOptions` を明示渡しした場合、`RootFilePath` 未設定のままになると相対 import 解決が不安定になる

### 9.3 問題点（運用品質）

- `Program.cs` は開発者ローカルの Windows 絶対パスをハードコードしており、リポジトリ内ではそのまま動かない
- テスト不在のため、修正時の安全網がない
- 例外ではなく `Issue` を返す設計と、`ToDictionary` など例外化しうる箇所が混在している

## 10. 設計ドキュメント自体の不備・矛盾点

### 10.1 DSL 仕様の二重系統

`Design/` 側の DSL 定義は新しい記法に寄っている一方、`ExcelReportLibTest/TestDsl` 側は古い記法が残っている。

具体例:

- `styleImport`
  - `Design/` 側: `<styleImport>`
  - `ExcelReportLibTest/TestDsl` 側 XSD/サンプル: `<import>` が残存
- `use` インスタンス属性
  - `Design/` 側: `instance`
  - `TestDsl` 側サンプル: `name` が残存
- span 属性
  - `Design/` 側: `rowSpan` / `colSpan`
  - `TestDsl` 側 XSD: `rowspan` / `colspan`

この状態では、どれを「正」として実装・利用すべきかが資料間で一貫しない。

### 10.2 設計書の As-Is / To-Be は改善されているが、根拠の参照先が古い箇所がある

2026-02-13 系のレポートは実装調査として有用だが、その後のワークフロー関連レポートが大量に混在しており、ExcelReport 本体の設計判断に関係する資料を探しにくい。

### 10.3 `reports/` の役割が混在している

`reports/` には ExcelReport の設計/実装調査と、Claude/Codex ワークフロー運用レポートが同居している。命名だけでは判別しにくく、ExcelReport の設計資料置き場としてはノイズが多い。

### 10.4 文書品質に乱れがあるファイルがある

`reports/phase1-implementation-strategy.md` は冒頭にコマンド出力片が混ざっており、章が重複している。設計判断の一次資料としては扱いにくい。

### 10.5 `BasicDesign` と実装の距離が大きい

`BasicDesign_v1.md` は理想アーキテクチャとしては整理されているが、実装済み範囲が `DslParser + AST` に限られるため、現状では「設計の全体像」と「コードの現実」の差が大きい。設計先行自体は許容できるが、As-Is の追従更新を継続しないと誤読の温床になる。

## 11. `reports/` 全体の読み込み結果と要約

### 11.1 ExcelReport 直接関連レポート

- `design-validity-investigation-2026-02-08.md`
  - 初回の設計実装差分調査。実装は `DslParser` 周辺のみと整理。
- `implementation-inventory-2026-02-13.md`
  - 実装棚卸し。`DslParser + AST + サンプル` 以外は未実装と明示。
- `design-inventory-2026-02-13.md`
  - `Design/` の棚卸し。設計カバレッジは広い。
- `design-implementation-alignment-2026-02-13.md`
  - 主要不整合を列挙。`use@instance`, `rowSpan/colSpan`, `styleImport`, 重複定義方針などを指摘。
- `issues-and-improvements-2026-02-13.md`
  - High/Medium/Low の優先度整理。最優先は DSL 仕様整合と検証未完成。
- `design-revision-strategy-2026-02-13.md`
  - 設計は As-Is / To-Be の二層化で運用すべき、という提案。

### 11.2 ワークフロー運用関連レポート

以下は ExcelReport アプリ本体ではなく、Claude/Codex ワークフローの運用整備に関する資料である。

- `workflow-integration-analysis.md`
- `integration-implementation-plan.md`
- `phase1-current-status-2026-02-13.md`
- `phase1-implementation-strategy.md`
- `phase2-skill-gap-analysis-2026-02-13.md`
- `phase2-coverage-matrix-2026-02-17.md`
- `claude-code-workflows-stop-approval-investigation-2026-02-17.md`
- `claude-code-workflows-quality-gate-investigation-2026-02-17.md`
- `sandbox-matrix-investigation-phase-analysis-2026-02-17.md`
- `skill-design-guidelines-2026-02-17.md`
- `phase2-reference-sandbox-sync-audit-2026-03-02.md`
- `phase2-task2.21-contract-compliance-verification-2026-03-02.md`
- `phase2-task2.22-stop-approve-resume-verification-2026-03-02.md`
- `phase2-task2.23-quality-gate-verification-2026-03-02.md`
- `phase2-readiness-report-2026-03-02.md`
- `phase3-investigation-2026-03-03.md`
- `phase3-task3.1-validation-2026-03-03.md`
- `phase3-closure-verification-2026-03-03.md`
- `feedback-points-status-review-2026-03-03.md`
- `enforce-status-updates-plan-2026-03-03.md`
- `enforce-status-updates-plan-v2-2026-03-03.md`

要点としては、「Codex を実務プレーン、Claude を制御プレーンに置く」という運用ルールの整備が主題であり、ExcelReport 本体の設計/実装状況には直接影響しない。プロジェクト文書としては別ディレクトリに分離した方が見通しがよい。

## 12. 推奨する開発順序

設計依存関係と実装の現状を踏まえると、次の順序が最も安全である。

### 12.1 第1段階: DSL 契約の一本化

最優先で次を揃える。

- `Design/` と `ExcelReportLibTest/TestDsl/` の DSL 記法統一
- `styleImport` / `instance` / `rowSpan,colSpan` の正規化
- サンプル XML / XSD / 実装の三者整合

ここが揃わない限り、後段実装を進めても入力契約がぶれる。

### 12.2 第2段階: `DslParser` 完成

- XSD 検証の有効化
- `ValidateDsl` 実装
- `cell@styleRef`, `cell@formulaRef`, `repeat@from`, `sheet@rows/cols`, `componentImport` 内 styles 取り込みの実装
- 例外化箇所を `Issue` ベースに整備
- 単体テスト追加

これは全後段モジュールの前提であり、ここが不安定だと下流実装が連鎖的に不安定になる。

### 12.3 第3段階: `Styles` と `ExpressionEngine`

- `Styles` のグローバル辞書・参照解決・scope 検証・StylePlan 実装
- `ExpressionEngine` の式評価とキャッシュ

`LayoutEngine` がこの 2 つに依存するため、ここは先行すべき。

### 12.4 第4段階: `LayoutEngine`

- `repeat/use/grid/cell` の展開
- 最終スタイル決定
- LayoutPlan の確立

プロジェクトの価値の中核はここにある。

### 12.5 第5段階: `WorksheetState`

- LayoutPlan から最終状態へ固定化
- merge / named area / formula series / sheet options の確定

### 12.6 第6段階: `Renderer`

- `.xlsx` 出力
- Issues シート
- `_Audit` シート

### 12.7 第7段階: `Logger` と `ReportGenerator`

- `Logger` の横断導入
- `ReportGenerator` による全体統合
- E2E テスト

`Logger` は設計上横断モジュールだが、先に個別モジュールの入出力契約が固まってから入れた方が手戻りが少ない。

## 13. 具体的な優先改善ポイント

短期的に最も効果が高いのは次の 6 点である。

1. `TestDsl` 側の DSL サンプルと XSD を `Design/` 側の新記法に統一する
2. `RepeatAst.FromExprRaw` 未設定を修正する
3. `CellAst` の `styleRef` / `formulaRef` 取り込みを実装する
4. `ComponentImportAst` の `<styles>` 取り込みを実装する
5. `SheetOptionsAst` の `groupCols` パース位置を修正する
6. `DslParser` 単体テストプロジェクトを新設する

## 14. 現状の総評

ExcelReport は、設計の見通し自体はかなり明確で、モジュール分割も妥当である。一方で、実装は `DslParser` を中心とした初期段階に留まり、しかもその `DslParser` も「AST 構築の土台」までで止まっている。したがって、現時点での最重要課題は新機能追加ではなく、まず「DSL 契約の整合」と「Parser の完成度引き上げ」である。

この 2 点を済ませてから後段モジュールへ進めば、設計資産を無駄にせず、依存関係どおりに堅実に実装を進められる。
