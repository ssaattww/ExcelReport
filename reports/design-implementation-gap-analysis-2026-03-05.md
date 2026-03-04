# 設計書と実装のギャップ分析レポート

- 作成日: 2026-03-05
- 対象設計: `Design/` 配下の指定11ファイル
- 対象実装: `ExcelReport/ExcelReportLib/` 配下の指定ファイル群
- 分析観点: 設計定義機能、実装済み機能、未実装/不完全、設計乖離、影響度

## DslParser + AST

### 1. 設計で定義されている機能一覧
1. DSL全体要素（`workbook/styles/component/componentImport/sheet/grid/cell/use/repeat/sheetOptions`）のXSD準拠パース（`DslDefinition_DetailDesign_v1.md` §3-§7、`DslDefinition_v1.xsd`）。
2. `sheet` 直下の `styleRef` と `style`（インライン）対応（`DslDefinition_DetailDesign_v1.md` §3.4、§5.2）。
3. `formulaRef` 系列の1次元連続性検証（`DslDefinition_DetailDesign_v1.md` §9、`DslParser_DetailDesign_v1.md` §6.2）。
4. `repeat@from` の制約検証（IEnumerableであること）（`DslDefinition_DetailDesign_v1.md` §4.5）。
5. `TreatExpressionSyntaxErrorAsFatal` に基づく式構文チェック（`BasicDesign_v1.md` §2.3、`DslParser_DetailDesign_v1.md` §7.2）。
6. 構文→意味→参照解決→静的レイアウト検証の段階的処理（`BasicDesign_v1.md` §2.3）。

### 2. 実装済みの機能一覧
1. `DslParser.ParseFromFile/ParseFromText/ParseFromStream` と `DslParserOptions`/`DslParseResult` を実装（`DSL/DslParser.cs`）。
2. XSD検証（`ValidateWithSchema`）とXML破損検出（`XmlMalformed`）を実装。
3. AST構築（`WorkbookAst`, `SheetAst`, `StyleAst`, `UseAst`, `RepeatAst`, `SheetOptionsAst` ほか）を実装。
4. 参照系の検証（未定義style/component、重複名、style scope違反、sheetOptions参照先）を実装。
5. 静的座標検証（シート範囲・Excel上限）を実装。

### 3. 未実装・不完全な機能一覧
1. `sheet` 直下インライン `style` のAST保持が未実装（設計: `DslDefinition_DetailDesign_v1.md` §3.4/§5.2、実装: `DSL/AST/SheetAst.cs` が `StyleRefs` のみ）。影響度: Medium。
2. `formulaRef` 系列の1D連続検証が未実装（設計: `DslDefinition_DetailDesign_v1.md` §9、`DslParser_DetailDesign_v1.md` §6.2、実装: `DSL/DslParser.cs` に `FormulaRefSeriesNot1DContinuous` 発行ロジックなし）。影響度: High。
3. `repeat@from` のIEnumerable制約検証がParser段階で未実装（設計: `DslDefinition_DetailDesign_v1.md` §4.5、実装: `ValidateRepeatConstraints` は属性有無のみ）。影響度: Medium。
4. 式構文チェック（`TreatExpressionSyntaxErrorAsFatal`）が未実装（設計: `BasicDesign_v1.md` §2.3、`DslParser_DetailDesign_v1.md` §7.2、実装: `DSL/DslParser.cs` でオプション値を参照しない）。影響度: High。
5. `component` 子要素制約（`grid/use/repeat` のみ）を厳密に守っていない（設計: `DslDefinition_v1.xsd` `ComponentType`、実装: `LayoutNodeAst.AllowedLayoutNodeNames` に `cell` 含有、`ComponentAst` は先頭1要素のみ採用）。影響度: Medium。
6. `styleRef` のネスト許容がXSD仕様と不一致（設計: `DslDefinition_v1.xsd` `StyleRefType` は属性のみ、実装: `StyleRefAst.StyleRefs`）。影響度: Low。

### 4. 実装が設計と異なる箇所
1. 検証フェーズ順序が設計と異なり、`ValidateDsl` の後に `ResolveStyleRefs/ResolveComponentRefs` を実行している（`DSL/DslParser.cs`）。
2. `component` の不正構造に対し設計上の厳密制約より緩く受理する実装になっている（`DSL/AST/ComponentAst.cs`）。

### 5. 影響度
- High: 2
- Medium: 3
- Low: 1

## ExpressionEngine

### 1. 設計で定義されている機能一覧
1. RoslynベースのC#式評価（`ExpressionEngine.md` §1、§4.1）。
2. API契約 `IExpressionEvaluator.Evaluate(string, EvaluationContext): object`（`ExpressionEngine.md` §2.1）。
3. `Globals` モデル（`root/data/vars`）と参照設定（`ExpressionEngine.md` §3.1-§3.2）。
4. Compilation/Runtimeのエラー分類（`ExpressionEngine.md` §5.2）。
5. 式キャッシュ（`ExpressionEngine.md` §6.1）。

### 2. 実装済みの機能一覧
1. `IExpressionEngine.Evaluate` による式評価APIを実装（`ExpressionEngine/IExpressionEngine.cs`）。
2. `root/data/vars` 参照、メンバーアクセス、インデクサアクセスを実装（`ExpressionEngine/ExpressionEngine.cs`）。
3. `ConcurrentDictionary<string, Lazy<...>>` ベースのキャッシュを実装。
4. エラー時に `#ERR(...)` 文字列を返す `ExpressionResult` を実装。

### 3. 未実装・不完全な機能一覧
1. Roslyn評価パイプライン（`CSharpScript.Create` 等）未実装（設計: `ExpressionEngine.md` §4.1、実装: 独自パーサ `ExpressionParser`）。影響度: High。
2. API契約不一致（設計: `IExpressionEvaluator.Evaluate -> object`、実装: `IExpressionEngine.Evaluate -> ExpressionResult`）。影響度: Medium。
3. `Globals` 型/モデル未実装（設計: `ExpressionEngine.md` §3.1、実装: `ExpressionContext` のみ）。影響度: Medium。
4. Compilation/Runtime分類未実装（実装はほぼ `IssueKind.ExpressionSyntaxError` に集約）。影響度: Medium。
5. 設計で例示される一般C#式（LINQ、演算、複合式）を網羅できない（実装は識別子+`.`+`[]`中心）。影響度: High。

### 4. 実装が設計と異なる箇所
1. 設計は「C#スクリプト評価エンジン」、実装は「制限付き式ナビゲータ」になっている。
2. インターフェース名は `IExpressionEvaluator` エイリアスを残すが、実体契約は設計差分がある。

### 5. 影響度
- High: 2
- Medium: 3
- Low: 0

## Styles

### 1. 設計で定義されている機能一覧
1. `styleRef`/インラインstyleから `StylePlan`（候補の順序付き中間定義）を構築（`Styles_DetailDesign.md` §5-§8）。
2. 適用階層情報（sheet/component/grid/cell）と順序情報の保持（`Styles_DetailDesign.md` §5.1-§5.2）。
3. scope違反の検出とWarning化（`Styles_DetailDesign.md` §5.4、§9.1）。
4. `IStyleResolver.ResolveStyles(...)` 契約（`Styles_DetailDesign.md` §6.1）。
5. Stylesは最終スタイルを決めず、LayoutEngineが最終決定（`Styles_DetailDesign.md` §3、§5、`BasicDesign_v1.md` §2.8）。

### 2. 実装済みの機能一覧
1. グローバルスタイル辞書構築（`StyleResolver.BuildGlobalStyles`）。
2. `styleRef` 解決、未定義参照Issue追加（`StyleResolver.Resolve`）。
3. scope違反時Warning、およびcellでの `outer/all` border無効化（`StyleResolver.ResolveStyleCore`）。
4. `StylePlan` 返却（`BuildPlan`）とトレース情報保持（`StyleValueTrace`）。

### 3. 未実装・不完全な機能一覧
1. 設計API `ResolveStyles(...)` 未実装（実装APIは `BuildPlan(...)`）（設計: `Styles_DetailDesign.md` §6.1、実装: `Styles/IStyleResolver.cs`）。影響度: Medium。
2. 適用階層（sheet/component/grid/cell）の明示メタデータ保持が不足（設計: `Styles_DetailDesign.md` §5.1、実装: `StylePlan` は階層概念を直接保持しない）。影響度: Medium。
3. Stylesが最終決定しない設計に対し、実装は `EffectiveStyle` を確定している（設計: `Styles_DetailDesign.md` §3/§5、実装: `StylePlan.EffectiveStyle`, `StyleResolver.BuildPlan`）。影響度: High。
4. `styleRef` 属性と `<styleRef>` 要素の区別された順序モデルをAPIで表現できていない（設計: `Styles_DetailDesign.md` §5.2/§8.1/§8.2）。影響度: Medium。
5. 未定義 `styleRef` の扱いが設計想定（Warning中心）と異なりError化される（設計: `Styles_DetailDesign.md` §9.1-§9.2、実装: `StyleResolver.Resolve`）。影響度: Medium。
6. 重複style名の扱い（Warningと運用ルール明示）がStyles側で実装されていない（実装は辞書後勝ち上書き）。影響度: Low。

### 4. 実装が設計と異なる箇所
1. Stylesモジュールの責務が「中間定義生成」から「実質的な最終合成」まで拡張されている。
2. LayoutEngineとの責務境界（最終スタイル決定位置）が逆転している。

### 5. 影響度
- High: 1
- Medium: 4
- Low: 1

## LayoutEngine

### 1. 設計で定義されている機能一覧
1. `ILayoutEngine.Build(WorkbookNode, object)` API（`LayoutEngine.md` §3.1）。
2. LogicalGrid/Rxによる座標伝播（`LayoutEngine.md` §2、§5.3、§6）。
3. `StylePlan` から `FinalStyleDefinition` を最終決定（`LayoutEngine.md` §4.4、§5.3.5）。
4. `Area`/`FormulaSeries` を含む `LayoutPlan` 生成（`LayoutEngine.md` §3.2、§5.3.6）。
5. Fatal/Error/Warning/Infoのエラーモデル（`LayoutEngine.md` §7）。

### 2. 実装済みの機能一覧
1. `Expand(WorkbookAst, object?)` でシート展開し `LayoutPlan` を生成。
2. `cell/grid/repeat/use` の再帰展開と式評価連携を実装。
3. 座標範囲チェックとIssue蓄積を実装。
4. `LayoutCell` に `StylePlan`/`Value`/`Formula`/`FormulaRef` を保持。

### 3. 未実装・不完全な機能一覧
1. API契約不一致（設計: `Build`、実装: `Expand`）（`LayoutEngine.md` §3.1、`LayoutEngine/ILayoutEngine.cs`）。影響度: Medium。
2. `componentImport` 経由コンポーネントを展開対象に含めない（実装: `BuildComponentIndex(workbook.Components)` のみ）。影響度: High。
3. `sheetOptions` を `LayoutSheet.Options` に渡していない（実装: `new LayoutSheet(sheet.Name, cells, sheet.Rows, sheet.Cols)`）。影響度: High。
4. LogicalGrid/Rx伝播モデル未実装（設計: `LayoutEngine.md` §2/§6、実装: 単純オフセット計算）。影響度: Medium。
5. `FinalStyleDefinition` 決定未実装（設計: §4.4/§5.3.5、実装: `StylePlan` をそのまま保持）。影響度: High。
6. `Area`/`FormulaSeries` 生成未実装（設計: §3.2/§5.3.6、実装: `LayoutSheet.NamedAreas` は空、系列集約なし）。影響度: High。
7. 設計エラーモデルに対しSeverity運用が簡略（Fatal化ポリシー、段階別分類が不足）。影響度: Medium。

### 4. 実装が設計と異なる箇所
1. 設計が求める「最終スタイル決定エンジン」ではなく「セル配置+簡易展開エンジン」に寄っている。
2. 仕様上重要な `Area/FormulaSeries/SheetOptions` が下流へ渡らないため、後続モジュール要件を満たせない。

### 5. 影響度
- High: 4
- Medium: 3
- Low: 0

## WorksheetState

### 1. 設計で定義されている機能一覧
1. `WorksheetWorkbookState Build(LayoutPlan)` 契約（`WorksheetState_DetailDesign.md` §6.1-§6.2）。
2. `FinalStyle -> StyleSnapshot` 変換とWorkbook全体style辞書dedupe（§4.4、§5.6、§5.8）。
3. `FormulaSeriesMap` 構築（§4.9、§5.5）。
4. `Issue` を生成・統合し返却（§2.2、§7）。
5. `CellState` の `ValueKind/ErrorText/MergedRange` を含む最終状態モデル（§4.3）。
6. `SheetOptions`（Freeze/Print/View/AutoFilter）写像（§4.6、§5.7）。

### 2. 実装済みの機能一覧
1. `LayoutPlan -> IReadOnlyList<WorksheetState>` 変換を実装。
2. セル辞書構築、結合範囲計算、名前付き領域取り込みを実装。
3. Freeze/GroupRows/GroupCols/AutoFilter の一部オプション変換を実装。

### 3. 未実装・不完全な機能一覧
1. 戻り値契約 `WorksheetWorkbookState` が未実装（実装: `IReadOnlyList<WorksheetState>`）（設計: §6.1-§6.2、実装: `WorksheetState/IWorksheetStateBuilder.cs`）。影響度: High。
2. Workbook単位 `Styles`（StyleSnapshot辞書）未実装（設計: §4.1/§5.8、実装: `CellState.Style` に `ResolvedStyle` 直保持）。影響度: High。
3. `FormulaSeriesMap` 未実装（設計: §4.2/§4.9/§5.5、実装: 該当モデルなし）。影響度: High。
4. `Issue` 蓄積モデル未実装（設計: §2.2/§7、実装: `InvalidOperationException` で中断）。影響度: High。
5. `CellState` 契約不足（`ValueKind/ErrorText/MergedRange` なし）（設計: §4.3、実装: `WorksheetState/CellState.cs`）。影響度: Medium。
6. `Print/View` を含む `SheetOptions` 未実装（設計: §4.6/§5.7、実装: `WorksheetOptionsState` は Freeze/Group/AutoFilterのみ）。影響度: Medium。
7. DSL層型 `SheetOptionsAst` へ直接依存し、責務境界が設計と異なる（設計: §3.1、実装: `WorksheetStateBuilder.BuildOptions(SheetOptionsAst?)`）。影響度: Low。

### 4. 実装が設計と異なる箇所
1. 「Issueを返すビルダー」ではなく「例外を投げる変換器」に近い振る舞い。
2. 下流Rendererが期待するWorkbook統合状態を生成しない。

### 5. 影響度
- High: 4
- Medium: 2
- Low: 1

## Renderer

### 1. 設計で定義されている機能一覧
1. `WorksheetWorkbookState` を受けてxlsxを出力（ファイルまたはストリーム）（`Renderer_DetailDesign.md` §1.3-§1.4、§5.11）。
2. `FormulaSeries` 適用（§2.1、§5.7）。
3. `SheetOptions`（Freeze/Print/View/AutoFilter）適用（§2.1、§5.8）。
4. `Issues` シートと `_Audit` シート生成（§2.1、§5.9、§5.10、§7）。
5. 進捗通知（Book/Sheet/CellBatch）（§2.1、§7.1）。
6. 例外をIssueへ変換するエラーモデル（§8.2-§8.3）。

### 2. 実装済みの機能一覧
1. `WorksheetState` 一覧からOpenXMLでxlsxを作成し `MemoryStream` を返却。
2. セル値/式、結合セル、名前付き領域、部分的SheetOptions（freeze/group/autofilter）を適用。
3. `_Issues` シート、`_Audit` シート（固定メタ情報）を生成。

### 3. 未実装・不完全な機能一覧
1. 入出力契約不一致（設計: `WorksheetWorkbookState` + 出力先選択、実装: `IReadOnlyList<WorksheetState>` + `MemoryStream` 固定）。影響度: High。
2. `RendererOptions` の出力パス/出力Stream未実装（設計: §5.11、実装: `RenderOptions` は監査用メタのみ）。影響度: High。
3. `FormulaSeries` 登録未実装（設計: §5.7、実装: 該当処理なし）。影響度: High。
4. `SheetOptions` のPrint/View未実装（設計: §5.8、実装: freeze/group/autofilter中心）。影響度: Medium。
5. Issuesシート仕様不一致（設計: `Issues` シート、実装: `_Issues`、列構成も異なる）。影響度: Medium。
6. `_Audit` が設計監査ログ行フォーマットを満たさない（実装は `GeneratedAt/TemplateName/DataSource` のみ）。影響度: Medium。
7. 例外→Issue変換ポリシー未実装（設計: §8.2-§8.3、実装: `Render` 内で包括catchなし）。影響度: High。
8. 進捗通知API連携未実装（設計: §7.1、実装: `IReportLogger` 非依存）。影響度: High。
9. 設計上「StyleSnapshotをそのまま物理適用」に対し、日付値へデフォルト書式を自動補完する実装差分がある（`DefaultDateFormatCode`）。影響度: Low。

### 4. 実装が設計と異なる箇所
1. Rendererが「単純OpenXML書き出し」に寄っており、監査/進捗/Issue変換の運用機能が弱い。
2. 下流I/O契約（ファイル/ストリーム選択）と上流契約（Workbook統合状態入力）が未整合。

### 5. 影響度
- High: 5
- Medium: 3
- Low: 1

## Logger

### 1. 設計で定義されている機能一覧
1. Book→Sheet→Region→CellBatch の進捗モデル（`Logger_DetailDesign.md` §3）。
2. `IReportLogger.ReportProgress` / `GetProgressEvents`（§5.1）。
3. `IReportLogSink` による転送（§5.2）。
4. `IAuditLogExporter` / `AuditRow`（§5.3、§6）。
5. `LogLevel`（Trace〜Fatal）/`LogCategory`（§4.1-§4.2）。
6. `LogEntry` の文脈情報（BookId/Sheet/Region/Location/Properties）（§4.3）。

### 2. 実装済みの機能一覧
1. `IReportLogger` の基本ロギング (`Debug/Info/Warning/Error`, `LogIssue`)。
2. `ReportLogger` のスレッドセーフなエントリ蓄積。
3. レベル別取得 (`GetEntries(LogLevel)`) と監査トレイル取得 (`GetAuditTrail`)。

### 3. 未実装・不完全な機能一覧
1. 進捗イベントモデル（`ProgressScope/ProgressPhase/ProgressEvent`）未実装。影響度: High。
2. `ReportProgress` / `GetProgressEvents` 未実装（設計: §5.1、実装: `Logger/IReportLogger.cs` に存在しない）。影響度: High。
3. `IReportLogSink` 未実装（設計: §5.2）。影響度: Medium。
4. `IAuditLogExporter` / `AuditRow` 未実装（設計: §5.3/§6）。影響度: High。
5. `LogLevel.Trace` / `LogLevel.Fatal` 未実装（設計: §4.1、実装: `Debug/Info/Warning/Error` のみ）。影響度: Medium。
6. `LogCategory` / `LogContext` 未実装（設計: §4.2/§5.1）。影響度: Medium。
7. `LogEntry` の設計必須フィールド（BookId/Sheet/Region/Location/Properties）が不足。影響度: High。
8. フェーズ定義が設計 (`ProgressPhase`) と異なり `ReportPhase` 固定になっている。影響度: Low。

### 4. 実装が設計と異なる箇所
1. Loggerが「ログ保存」のみで、設計が要求する「進捗・監査データ供給」を担えていない。
2. ReportGenerator/Rendererで使うための文脈付きログモデルが不足。

### 5. 影響度
- High: 4
- Medium: 3
- Low: 1

## ReportGenerator

### 1. 設計で定義されている機能一覧
1. `IReportGenerator.GenerateAsync(ReportRequest, CancellationToken)`（`ReportGenerator_DetailDesign.md` §3.4、§5.4）。
2. `TemplateSource` / `ReportRequest` / `ReportResult` モデル（§3.1-§3.3）。
3. テンプレート取得優先順とASTキャッシュ（§2.1.3、§4.3、§5.2.3）。
4. 入力検証、BookId準備、Issue統合、Fatal判定、例外処理、キャンセル伝播（§2.1.2、§2.1.5-§2.1.7、§6）。
5. `Elapsed` を含む結果返却（§2.1.8、§3.3）。

### 2. 実装済みの機能一覧
1. 同期API `ReportGenerator.Generate(string dsl, object? data, ...)` を実装。
2. Parser→LayoutEngine→WorksheetStateBuilder→Renderer の基本パイプライン実装。
3. Parser/Layout Issue の統合、Fatal時中断、ログ記録を実装。

### 3. 未実装・不完全な機能一覧
1. 非同期API `GenerateAsync` と `IReportGenerator` 契約未実装（設計: §3.4、実装: `ReportGenerator.cs`）。影響度: High。
2. `ReportRequest` / `TemplateSource` モデル未実装（設計: §3.1-§3.2、実装: `dsl` 文字列直入力）。影響度: High。
3. テンプレートキャッシュ未実装（設計: §2.1.3/§4.3、実装: 毎回 `DslParser.ParseFromText`）。影響度: High。
4. 入力検証方針不一致（設計は引数例外運用を明示、実装はFatal Issue化中心）。影響度: Medium。
5. BookId生成/Logger文脈準備未実装（設計: §5.2.2、実装: 該当処理なし）。影響度: Medium。
6. Issue統合範囲が不十分（設計は全モジュール統合、実装は主にParser/Layout依存）。影響度: High。
7. キャンセル伝播が不十分（設計: §6.3、実装: Renderer呼び出し時のみToken利用）。影響度: Medium。
8. `Elapsed` 計測未実装（設計: §2.1.8/§3.3、実装: `ReportGeneratorResult` に時間情報なし）。影響度: Medium。
9. `ReportResult` 契約不一致（設計は `RenderResult` を中核とする統一モデル、実装は `RenderResult?` 許容の独自構造）。影響度: Medium。

### 4. 実装が設計と異なる箇所
1. 設計の「統合オーケストレータ（要求/キャッシュ/監査/計測含む）」に対し、実装は「同期的な最小パイプライン実行器」。
2. 基盤契約（Request/Result/API）が設計版と互換でないため、将来の差し替え・外部連携が難しい。

### 5. 影響度
- High: 4
- Medium: 5
- Low: 0

## サマリ

### 未実装機能の総数
- 合計: **57件**
- 内訳:
1. DslParser + AST: 6
2. ExpressionEngine: 5
3. Styles: 6
4. LayoutEngine: 7
5. WorksheetState: 7
6. Renderer: 9
7. Logger: 8
8. ReportGenerator: 9

### High影響度の項目リスト
1. DslParser: `formulaRef` 系列1D連続検証未実装。
2. DslParser: `TreatExpressionSyntaxErrorAsFatal` を使った式検証未実装。
3. ExpressionEngine: RoslynベースC#評価未実装（実装は制限付き式）。
4. ExpressionEngine: 設計で前提の複雑C#式（LINQ等）未対応。
5. Styles: Styles側で `EffectiveStyle` を確定し、責務がLayoutEngineへ渡っていない。
6. LayoutEngine: `componentImport` 由来コンポーネントを展開対象に含めない。
7. LayoutEngine: `sheetOptions` を `LayoutSheet` に引き継がない。
8. LayoutEngine: `FinalStyleDefinition` 未生成。
9. LayoutEngine: `Area/FormulaSeries` 未生成。
10. WorksheetState: `WorksheetWorkbookState` 契約未実装。
11. WorksheetState: `StyleSnapshot` 辞書化/dedupe未実装。
12. WorksheetState: `FormulaSeriesMap` 未実装。
13. WorksheetState: Issue生成・統合未実装（例外中断）。
14. Renderer: 入出力契約（WorkbookState/出力先選択）未整合。
15. Renderer: `FormulaSeries` 適用未実装。
16. Renderer: 例外→Issue変換未実装。
17. Renderer: 進捗通知未実装。
18. Logger: Progressモデル/API未実装。
19. Logger: Audit exporter/model未実装。
20. Logger: `LogEntry` 必須文脈不足。
21. ReportGenerator: `GenerateAsync` + `ReportRequest/TemplateSource` 契約未実装。
22. ReportGenerator: ASTキャッシュ未実装。
23. ReportGenerator: 全モジュールIssue統合が未完成。

### 推奨対応順序
1. **LayoutEngine → WorksheetState → Renderer の契約復元**。
2. **Logger 契約（Progress/Audit）を先に固定**。
3. **ReportGenerator API/Request/Result の設計準拠化**。
4. **ExpressionEngine をRoslyn前提へ拡張**。
5. **Styles の責務整理（FinalStyle決定をLayoutEngineへ寄せる）**。
6. **DslParser + AST の仕様厳格化（formulaRef検証・式検証・XSD準拠差分解消）**。

