# Tasks Status

Last Updated: 2026-04-05
Scope: ExcelReport開発 - issue #43 非同期api対応 / README刷新（Chart・Async）

## Progress Summary

- 2026-04-05 PR作成: README更新を含むPR #52 を作成し、本文に `Closes #48` を記載して issue連携
- 2026-04-05 README追記: QuickStart を component定義ベースへ更新（`component` + `grid` + `repeat` + `use` を含む構成）
- 2026-04-05 README刷新: `README.md` / `README.ja.md` を更新し、DSL `urn:excelreport:v2`・Chart機能・Async API・進捗ポーリング例を反映
- 2026-04-05 README方針反映: ユーザー指摘により README での ClosedXML 対比は記載しない方針へ調整
- 2026-04-05 PR#51 Codexレビュー対応: `TryGetResult` を終端状態ゲート化し、結果公開と終端状態遷移を原子的に統合
- 2026-04-05 PR#51 Codexレビュー対応: `Cancel` の `CancellationTokenSource` 破棄競合を防御し、`ObjectDisposedException` を `false` 返却へ変換
- 2026-04-05 PR#51 検証: `AsyncReportGeneratorTests` 7件 + Release全体198件通過を確認
- 2026-04-05 PR#51 記録: `reports/pr51-codex-review-fixes-2026-04-05.md` を追加
- 2026-04-05 issue#43 フォローアップ: 遅延箇所可視化のため `AsyncReportJobStatus` に総経過時間/フェーズ別経過時間を追加
- 2026-04-05 issue#43 フォローアップ: `RenderingCompletedUnits/RenderingTotalUnits` を追加し、Rendering中の細粒度進捗を取得可能化
- 2026-04-05 issue#43 フォローアップ: `AsyncReportGenerator` で `phase + timestamp` ベースの時間集計を実装し、`TryGetStatus` で実行中スナップショットを返却
- 2026-04-05 issue#43 フォローアップ: `RenderOptions.ProgressReporter` を追加し、レンダラーから進捗ユニット通知を受け取る方式へ拡張
- 2026-04-05 issue#43 フォローアップ: `Remove` を終端状態のみ許可に統一し、削除時に `CancellationTokenSource` を破棄
- 2026-04-05 issue#43 フォローアップ検証: `AsyncReportGeneratorTests` 7件 + 全体198件通過を確認
- 2026-04-05 issue#43 ドキュメント追記: 設計書へポーリングで実行時間/進捗を取得するC#例を追加
- 2026-04-05 issue#43 ドキュメント追記: 設計書へGUI（WinForms/WPF）でUIを固めない非同期ポーリング例を追加
- 2026-04-05 issue#43 記録: `reports/issue43-progress-timing-followup-2026-04-05.md` を追加
- 2026-04-05 issue#43 対応: 非同期API（job起動/進捗取得/結果取得/キャンセル）を設計書作成後に実装
- 2026-04-05 issue#43 設計: `Design/ReportGenerator/ReportGenerator_AsyncApi_DetailDesign.md` を追加
- 2026-04-05 issue#43 検証: `AsyncReportGeneratorTests` 4件 + 全体195件通過を確認
- 2026-04-05 issue#43 記録: `reports/issue43-async-api-design-and-implementation-2026-04-05.md` を追加
- 2026-04-05 issue#37 レビュー実施: SubAgentレビューを3チャンク（DSL/AST/XSD, Layout/WorksheetState, Renderer/Tests/Docs）に分割し、各結果を `reports/subagent-review-issue37-chunk{1..3}-2026-04-05.md` へ出力
- 2026-04-05 issue#37 レビュー統合: `reports/subagent-review-issue37-summary-2026-04-05.md` を作成し、指摘を重大度別に集約
- 2026-04-05 issue#37 指摘対応: chart series OpenXML子要素順（`dPt`/`spPr`）を修正し、`RendererTests`/`ReportGeneratorTests` に schema妥当性検証を追加
- 2026-04-05 issue#37 指摘対応: `formulaRef` 系列解決を named area から分離（専用 series map 化）し、`*End` 衝突時の誤解決を回避
- 2026-04-05 issue#37 指摘対応: `TestDsl/DslDefinition_v2.xsd` を Design版と同期（`sheet/repeat` の `from/var` 属性/要素両対応）
- 2026-04-05 issue#37 指摘対応: `DslParser` の静的座標検証を rows/cols省略時も Excel上限で有効化、ルート要素名検証を追加
- 2026-04-05 検証: `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore` 実行で `Passed 189, Failed 0`
- 2026-04-05 追跡更新: `Design/Chart/Chart_DetailDesign.md` を追跡対象に追加し、tasks/phases の進捗記録に明示
- 2026-04-05 issue#37 設計レビュー: `Design/Chart/Chart_DetailDesign.md` をレビューし、実装方針を策定
- 2026-04-05 issue#37 方針レビュー: SubAgent（Socrates）に実装方針レビューを依頼し、指摘を反映した方針v2で確定
- 2026-04-05 issue#37 実装: chart DSL/AST/XSD・LayoutEngine・WorksheetState・Renderer（barStacked/line）まで一気通貫で実装
- 2026-04-05 issue#37 テスト: `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore` 実行で `Passed 185, Failed 0`
- 2026-04-05 記録: `reports/issue37-chart-design-and-plan-review-2026-04-05.md` を作成
- 2026-03-26 CI失敗修正: `LayoutEngineTests.Expand_WhenLocalAndImportedComponentsShareName_LocalComponentWins` の import fixture を `<workbook>` から `<components>` へ修正し、`componentImport` ルート厳格化と整合
- 2026-03-26 再検証: `dotnet test --no-restore` 全件実行で `Passed 179, Failed 0` を確認
- 2026-03-26 PR#47追加レビュー対応(3-6): `FindNamedArea` の global/unique-descendant 衝突時に global優先+Warning化、sheet-scope `conditionalFormatting@formulaRef` は fallback経路のみ local非リーク化
- 2026-03-26 PR#47追加レビュー対応(3-6): `styleImport`/`componentImport` にルート要素名（`styles`/`components`）のFatal検証を追加
- 2026-03-26 テスト追加・更新: `WorksheetStateTests`/`ReportGeneratorTests`/`ValidateDslTests`/`ComponentImportTests`/`StyleImportTests` を更新し 76件通過
- 2026-03-26 PR#47レビュー対応: `sheet` 直下 sibling `cell` の local formulaRef 参照回帰を修正（`LayoutEngine.ExpandSheet` で `cell` は `/sheet` 共有、非`cell` は `/sheet/node-{index}` を維持）
- 2026-03-26 テスト追加: `Expand_SheetCellSiblings_ShareLocalScopePath` / `Generate_SheetCellSiblingFormula_ResolvesLocalFormulaRef` を追加し、関連83件回帰テスト通過
- 2026-03-26 仕様追加: local `formulaRef` の曖昧解決でフォールバック（またはタイブレーク選択）した場合は `IssueSeverity.Warning` を必須化
- 2026-03-26 実装更新: `WorksheetStateBuilder` でフォールバック警告（`IssueKind.FormulaRefResolutionFallback`）を発行し、`ReportGeneratorResult.Issues` / ログへ集約
- 2026-03-26 テスト追加: `WorksheetStateTests` 3件 + `ReportGeneratorTests` 1件で warning発行を検証（`dotnet test --filter \"WorksheetStateTests|ReportGeneratorTests\"`: Passed 57）
- 2026-03-26 環境確認: `dotnet restore` は sandbox では `C:\Users\taiga\AppData\Roaming\NuGet\NuGet.Config` ACL で失敗するが、権限昇格実行で復旧し、その後 `dotnet test --no-restore --no-build` は通常実行で `Passed 165` を確認
- 2026-03-26 仕様確定: local可視性は「同一親の sibling は可視（use/grid/repeat内含む）、別親スコープは不可視」に統一し、`FindNamedArea` に子孫ローカル一意解決を追加
- 2026-03-26 仕様調整: `local` 可視性を「同一スコープの sibling は可視、sibling の内側スコープは不可視」に合わせて調整（`ExpandGrid` の child scope 付与を `cell` とコンテナで分岐）
- 2026-03-26 テスト調整: `LayoutEngineTests.Expand_RepeatGridSiblings_ShareLocalScopePath` へ期待値更新、`Generate_GridSiblingFormula_DoesNotResolveNestedSiblingLocalFormulaRef` を追加
- 2026-03-26 運用改善: `publish-nuget.yml` の push 版数解決を `latest_stable_tag` 基準から `VersionPrefix` チャネル内タグ基準へ変更（`X.Y.Z-pre` の `Z` をタグで継続インクリメント）
- 2026-03-26 受領レビュー: sub-agent 指摘として local formulaRef の nested scope 混線リスク（P1/P2）を確認
- 2026-03-26 運用更新: 設計書ファイル名のバージョンサフィックス（`_v1`等）を廃止（XSDは例外）し、`Design/BasicDesign.md` / `Design/DslDefinition/DslDefinition_DetailDesign.md` / `Design/DslParser/DslParser_DetailDesign.md` へリネーム
- 2026-03-26 レビュー追記: `reports/issue45-area-breaking-change-review-2026-03-26.md` を追加し、top-level sibling 間で local formulaRef スコープが混在し得るリスク（P1）を記録
- 2026-03-26 issue#45 破壊的変更実装: Named target属性を `area` に統一（`repeat@area` / `use@area` / `grid@area`）。`repeat@name` / `use@instance` / `grid@name` はASTでError化し非対応化
- 2026-03-26 実装更新: `INamedAreaTarget.AreaName` で named target解決経路を共通化（Parser/LayoutEngine）
- 2026-03-26 仕様更新: XSDを更新（Design版/TestDsl版）し `UseType@area` / `RepeatType@area` / `GridType@area` を反映
- 2026-03-26 テスト更新: `ReportGeneratorTests`（repeat/use/grid/sheet/formulaRef/local non-leak）・`LayoutEngineTests`・`SheetAstTests`・`WorksheetStateTests`・`ValidateDslTests` を更新/追加
- 2026-03-26 検証: `dotnet test --filter \"FullyQualifiedName~ReportGeneratorTests.Generate_ConditionalFormatting_\"` (Passed 14, Failed 0)
- 2026-03-26 検証: `dotnet test --filter \"...Expand_UseAreaAndRepeatAreaAndGridArea...|...Parse_Sheet_LayoutNodesWithAreaAttributes...|...Build_ConditionalFormatting_Target_NamedArea_PrecedesFormulaRefSeries...|...ValidateDsl_LegacyNamedTargetAttributes...|...ValidateDsl_SheetOptions_TargetGridArea...\"` (Passed 5, Failed 0)
- 2026-03-26 検証: `dotnet test --filter \"FullyQualifiedName~LayoutNodeTests.Parse_Use_HasAreaAttribute|FullyQualifiedName~LayoutNodeTests.Parse_Grid_HasAreaAttribute\"` (Passed 2, Failed 0)
- 2026-03-26 検証: `dotnet test --filter \"FullyQualifiedName~FullTemplate\"` (Passed 8, Failed 0)
- 2026-03-26 回帰検証: `dotnet test --no-build --no-restore --filter \"DslParser/ValidateDsl/LayoutNode/SheetAst/LayoutEngine/WorksheetState/Renderer/ReportGenerator\"` (Passed 127, Failed 0)
- 2026-03-26 総合検証: `dotnet test --no-build --no-restore` (Passed 161, Failed 0)
- 2026-03-26 後処理: repo直下の一時実行痕跡 `.appdata/.nuget/.dotnet` を削除し、以降は `%TEMP%` 配下環境変数でテスト実行
- 2026-03-26 再検証: `%TEMP%/excelreport-codex-env` 環境で `dotnet restore` 成功後、`--no-restore` で再ビルド+回帰 (`Passed 127`, `Passed 161`)
- 2026-03-26 環境対処: sandbox権限制約により `APPDATA` / `DOTNET_CLI_HOME` / `NUGET_PACKAGES` をワークスペース配下へリダイレクトしてテスト実行
- 2026-03-26 運用更新: `Design/BreakingChanges.md` を英語化し、予定バージョン表記を `X.Y.Zより後`（`after X.Y.Z`）へ統一
- 2026-03-26 運用更新: `publish-nuget.yml` から BreakingChanges の自動強制チェックを削除（Actionで失敗させない方針へ変更）
- 2026-03-26 issue#45 着手: `gh issue view 45` で要件を取得し、`conditionalFormatting` の範囲指定を `at` 直接指定から formulaRef 系列解決まで拡張する方針で調査開始
- 2026-03-26 issue#45 実装: `conditionalFormatting@at` で `formulaRef` 系列名を範囲ターゲットとして解決できるよう `WorksheetStateBuilder` を拡張
- 2026-03-26 local対応: `formulaRefScope="local"` 系列名指定時はスコープごとに条件付き書式を展開するよう対応
- 2026-03-26 競合解決: 同名の local/global 系列が共存する場合、`at` 解決は local を優先する仕様に統一
- 2026-03-26 テスト追加: `WorksheetStateTests` 3件 + `ReportGeneratorTests` 2件（issue#45向け）
- 2026-03-26 検証: `dotnet test --filter \"ConditionalFormatting\"` 15件全通過（Failed 0）
- 2026-03-26 記録: `reports/issue45-conditional-formatting-formularef-target-2026-03-26.md` を作成
- 2026-03-25 PR#41 inline指摘対応: `conditionalFormatting@at="A1"` 単一セル指定をレンダラーで解決可能に修正
- 2026-03-25 テスト追加: `RendererTests.Render_ConditionalFormatting_SingleCellTarget_IsRendered`
- 2026-03-25 検証: `ExcelReportLib.Tests` 141件全通過（Failed 0）
- 2026-03-25 記録: `reports/pr41-inline-single-cell-target-fix-2026-03-25.md` を作成
- 2026-03-25 PR#41最新レビュー追従: 2色colorScaleの子要素順序（cfvo先行）を回帰テストで明示保証
- 2026-03-25 テスト追加: `RendererTests.Render_ConditionalFormatting_TwoColorScale_ChildOrder_IsCfvoThenColor`
- 2026-03-25 検証: `ExcelReportLib.Tests` 140件全通過（Failed 0）
- 2026-03-25 記録: `reports/pr41-latest-review-followup-2026-03-25.md` を作成
- 2026-03-25 環境対応: `dotnet-install.sh` で .NET SDK 8.0.419 を `/workspace/.dotnet` へ導入
- 2026-03-25 検証: `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj` 実行（Passed 139, Failed 0）
- 2026-03-25 記録: `reports/test-run-dotnet-sdk-8.0.419-2026-03-25.md` を作成
- 2026-03-25 PR追従: conditionalFormatting の boolean(1/0)受理と local formulaRef 解決を修正
- 2026-03-25 テスト追加: `SheetAstTests.Parse_Sheet_ConditionalFormatting_BooleanLiterals_ParsesNumericBooleans` / `WorksheetStateTests.Build_ConditionalFormatting_FormulaRef_LocalScope_ResolvedFromTargetScope`
- 2026-03-25 記録: `reports/pr-followup-conditional-formatting-local-scope-and-bool-2026-03-25.md` を作成
- 2026-03-25 運用更新: ユーザー提示の最新版 `AGENTS.md`（Skills節含む）を同期
- 2026-03-25 記録: `reports/agents-instructions-sync-2026-03-25.md` を作成
- 2026-03-25 PR#41レビュー対応: colorScale の `cfvo`/`color` 子要素順序を `cfvo... -> color...` に修正
- 2026-03-25 テスト追加: `RendererTests` に colorScale 子要素順序アサーションを追加
- 2026-03-25 検証: `ExcelReportLib.Tests` 137件全通過（Failed 0）
- 2026-03-25 記録: `reports/pr41-review-cfvo-order-fix-2026-03-25.md` を作成
- 2026-03-25 レビュー追従: expression+formulaRef 条件付き書式の OpenXML スキーマ妥当性E2Eテストを追加
- 2026-03-25 テスト追加: `ReportGeneratorTests.Generate_ConditionalFormatting_ExpressionWithFormulaRef_OpenXmlSchemaValid`
- 2026-03-25 検証: `ExcelReportLib.Tests` 137件全通過（Failed 0）
- 2026-03-25 記録: `reports/issue34-review-followup-2026-03-25.md` を作成
- 2026-03-25 issue#34追加対応: expression条件付き書式に `formulaRef` 指定を追加（`formula` 未指定時は `NOT(ISBLANK(ref))` 自動生成）
- 2026-03-25 E2E追加: `ReportGeneratorTests` に 2カラー/3カラー/expression+formulaRef の3ケースを追加
- 2026-03-25 検証: `ExcelReportLib.Tests` 136件全通過（Failed 0）
- 2026-03-25 記録: `reports/issue34-conditional-formatting-formularef-e2e-2026-03-25.md` を作成
- 2026-03-25 issue#34追加対応: expression条件付き書式で `cell` 相当の書式属性（font/numberFormat/border/fill）を指定可能に拡張
- 2026-03-25 レンダラー拡張: `conditionalFormatting@formula` から DifferentialFormat(dxf) を生成し `FormatId` に紐付け
- 2026-03-25 記録: `reports/issue34-conditional-formatting-style-settings-2026-03-25.md` を作成
- 2026-03-25 issue#34追加対応: `conditionalFormatting` で 3色colorScale（midColor）と expression式一致時の書式変更（formula+fillColor）を実装
- 2026-03-25 テスト拡張: `SheetAstTests` / `WorksheetStateTests` / `RendererTests` を追加更新し 133件全通過を確認
- 2026-03-25 設計更新: `Design/DslDefinition/DslDefinition_DetailDesign_v1.md` に 3色・expression 対応範囲を反映
- 2026-03-25 記録: `reports/issue34-conditional-formatting-extensions-2026-03-25.md` を作成
- 2026-03-25 issue#34追記対応: 条件付き書式の対応範囲（2色colorScaleのみ）を設計書へ明記
- 2026-03-25 設計更新: `Design/DslDefinition/DslDefinition_DetailDesign_v1.md` に `7.5 conditionalFormatting` 節を追加
- 2026-03-25 設計更新: `Design/BasicDesign_v1.md` のDSL要素一覧へ `conditionalFormatting` を追加
- 2026-03-25 記録: `reports/issue34-design-doc-update-2026-03-25.md` を作成
- 2026-03-25 issue#34対応: `sheetOptions/conditionalFormatting`（2色colorScale）を実装し、レンダラー出力まで対応
- 2026-03-25 テスト追加: `SheetAstTests` / `WorksheetStateTests` / `RendererTests` に条件付き書式ケースを追加
- 2026-03-25 検証: `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj` 132件全通過（Failed 0）
- 2026-03-25 記録: `reports/issue34-conditional-formatting-2026-03-25.md` を作成
- 2026-03-25 PR#40再対応: `LayoutEngine` の scopePath 採番を再修正し、grid兄弟セルが同一スコープを共有するよう反映
- 2026-03-25 テスト追加: `LayoutEngineTests.Expand_RepeatGridSiblings_ShareSameScopePath` を追加
- 2026-03-25 記録: `reports/pr40-scopepath-sibling-fix-2026-03-25.md` を更新
- 2026-03-25 環境対応: .NET SDK 8.0.419 を導入して `dotnet test` 実行環境を整備
- 2026-03-25 不具合修正: `RendererTests` の `LayoutCell` ヘルパーを新コンストラクタ引数（`formulaRefScope`/`scopePath`）に追随
- 2026-03-25 検証: `ExcelReportLib.Tests` 129件全通過（Failed 0）
- 2026-03-25 記録: `reports/pr39-dotnet-sdk-test-run-2026-03-25.md` を作成
- 2026-03-25 PR#39指摘対応: `formulaRefScope` を XSD で `local|global` の列挙型に制約
- 2026-03-25 防御実装: `CellAst` で不正 `formulaRefScope` を Warning 記録 + `global` 正規化
- 2026-03-25 テスト追加: `LayoutNodeTests.Parse_Cell_InvalidFormulaRefScope_FallsBackToGlobalWithWarning`
- 2026-03-25 記録: `reports/pr39-inline-comments-fix-2026-03-25.md` を作成
- 2026-03-24 CI修正: PR #38 の `xunit-tests` 失敗原因（`LastIndexOf('/', StringComparison.Ordinal)` の誤用）を修正
- 2026-03-24 記録: `reports/pr38-ci-fix-worksheetstate-lastindexof-2026-03-24.md` を作成
- 2026-03-24 issue#35対応: `cell@formulaRefScope`（local/global）を追加し、formulaRef の解決スコープを制御可能に拡張
- 2026-03-24 実装拡張: `LayoutEngine` がセルに `scopePath` を付与、`WorksheetStateBuilder` が最寄りスコープ優先で `#{...}` を解決
- 2026-03-24 テスト追加: `WorksheetStateTests` に local scope + global fallback の検証ケースを追加
- 2026-03-24 記録: `reports/issue35-formula-ref-scope-2026-03-24.md` を作成
- 2026-03-24 機能追加: `cell` の `value` を属性/子要素(`<value>`)の両記法に対応
- 2026-03-24 互換制御: `cell` の `value` が属性と子要素で競合した場合はWarningを記録し属性値を優先
- 2026-03-24 テスト追加: `LayoutNodeTests` 2件 + `DslParserTests` 1件で `cell/<value>` 対応を検証
- 2026-03-24 記録: `reports/cell-value-element-support-2026-03-24.md` を作成
- 2026-03-24 不具合修正: repeat内の括弧付き式（@((p...))）で var が未解決になる ExpressionSyntaxError を解消
- 2026-03-24 実装改善: var書き換えを先頭一致依存からRoslyn構文木ベースの実参照置換へ一般化
- 2026-03-24 テスト追加: LayoutEngineTests に括弧付き式ケースと識別子参照ケース（p == null）を追加
- 2026-03-24 記録: reports/rewrite-repeat-var-robust-2026-03-24.md を作成
- 2026-03-24 不具合修正: 入れ子repeatの条件式で var を複数参照した際の ExpressionSyntaxError (m 未解決) を解消
- 2026-03-24 テスト追加: LayoutEngineTests.Expand_NestedRepeat_ConditionalExpressionUsingVarMultipleTimes_DoesNotEmitExpressionSyntaxError を追加（修正前Fail/修正後Pass）
- 2026-03-24 記録: `reports/repeat-var-conditional-expression-fix-2026-03-24.md` を作成
- 2026-03-24 ドキュメント改善: ルートREADMEのbuildステータスバッジを実ワークフロー表示へ修正
- 2026-03-24 運用統一: NuGet同梱READMEをリポジトリREADMEへ一本化（専用README削除）
- 2026-03-24 記録: `reports/readme-badge-and-package-readme-sync-2026-03-24.md` を作成
- 2026-03-24 CI改善: master push時のNuGet prerelease suffixを `-pre` 固定へ変更（`-pre.<run_number>` 廃止）
- 2026-03-24 CI改善: master push時に GitHub pre-release も自動作成する処理を追加
- 2026-03-24 記録更新: `reports/master-push-prerelease-auto-version-2026-03-24.md` をNuGet/GitHub両対応内容へ更新
- 2026-03-24 機能追加: `sheet` / `repeat` の `from`・`var` で子要素指定をサポート（属性形式も継続サポート）
- 2026-03-24 互換制御: 属性と子要素が同時指定された場合はIssue(Warning)を記録し、属性値を優先
- 2026-03-24 設計更新: `Design/DslDefinition/DslDefinition_DetailDesign_v1.md` と `Design/DslParser/DslParser_DetailDesign_v1.md` に from/var の属性・子要素併用仕様を反映
- 2026-03-24 記録: `reports/from-var-element-support-2026-03-24.md` を作成
- 2026-03-24 記録: `reports/from-var-element-design-doc-sync-2026-03-24.md` を作成
- 2026-03-24 CI更新: master pushをトリガーにNuGet prereleaseを自動publishするworkflowへ拡張
- 2026-03-24 バージョン規約: 最新安定タグ基準でpatch(3桁目)を自動増分し、`-pre.<run_number>`を付与
- 2026-03-24 記録: `reports/master-push-prerelease-auto-version-2026-03-24.md` を作成
- 2026-03-24 CI対応: master向けPRでxUnitテストを実行するGitHub Actions (`.github/workflows/pr-xunit-tests.yml`) を追加
- 2026-03-24 記録: `reports/pr-xunit-workflow-2026-03-24.md` を作成
- 2026-03-24 調査対応: NuGetパッケージのREADME未同梱警告を調査し、`reports/nuget-package-readme-fix-2026-03-24.md` を作成
- 2026-03-24 実装対応: `ExcelReportLib.csproj` に `PackageReadmeFile` と README同梱設定を追加し、`ExcelReportLib/README.md` を新規作成
- 2026-03-24 不具合調査: csx上のpublic型（`Submission#...`）で `repeat from` 評価が失敗する問題を調査し、`reports/csx-expression-hash-type-investigation-2026-03-24.md` を作成
- 2026-03-24 修正対応: `ExpressionEngine` に不正型名検出 + dynamicフォールバックを追加、回帰テストを追加
- 2026-03-19 調査対応: `sheet repeat` 未対応範囲を調査し、`reports/sheet-repeat-investigation-2026-03-19.md` を作成
- 2026-03-19 設計対応: `Design/DslDefinition/DslDefinition_DetailDesign_v1.md` にsheet repeat仕様を統合更新
- 2026-03-19 テスト先行: `ValidateDsl` / `LayoutEngine` / `ReportGenerator` に sheet repeat の先行テストを追加（Red確認）
- 2026-03-19 実装対応: `SheetAst` / `DslParser` / `LayoutEngine` / `DslDefinition_v1.xsd` を更新し sheet repeat を実装
- 2026-03-19 検証: `ExcelReportLib.Tests` 105件全通過

- Completed: 23 / 23
- In Progress: 0 / 23
- Not Started: 0 / 23
- Completion Rate: 100%

## Task List

| Task ID | Title | Status | Assignee | Dependencies | Phase |
|---|---|---|---|---|---|
| 1 | TestDsl側のXSD/サンプルXMLをDesign/側の新記法に統一 | Done | Codex + PM | None | 1 |
| 2 | DslParser実装の不備修正（属性未取得・バグ修正） | Done | Codex + PM | 1 | 1-2 |
| 3 | DslParser単体テストプロジェクト新設 | Done | Codex + PM | 2 | 2 |
| 4 | ValidateDsl実装とXSD検証有効化 | Done | Codex + PM | 3 | 2 |
| 5 | ExpressionEngine実装 | Done | Codex + PM | 4 | 3 |
| 6 | Styles resolver + StylePlan実装 | Done | Codex + PM | 5 | 3 |
| 7 | LayoutEngine実装 | Done | Codex + PM | 6 | 4 |
| 8 | WorksheetState実装 (TDD) | Done | Codex + PM | 7 | 5 |
| 9 | Renderer実装 (TDD) | Done | Codex + PM | 8 | 6 |
| 10 | Logger実装 (TDD) | Done | Codex + PM | 9 | 7 |
| 11 | ReportGenerator実装 (TDD) | Done | Codex + PM | 10 | 7 |
| 12 | Fix Border要素順序 (CT_Border schema準拠) | Done | Codex + PM | 9 | 8 |
| 13 | Fix 複数BorderInfo統合 (StyleKey.FromCell) | Done | Codex + PM | 12 | 8 |
| 14 | Grid border展開 (mode="outer"/"all") | Done | Codex + PM | 13 | 8 |
| 15 | FullTemplate E2Eテスト + borderテスト追加 | Done | Codex + PM | 12 | 8 |
| 16 | ReportGeneratorにファイルパスベース実行追加 | Done | Codex + PM | 15 | 9 |
| 17 | LayoutEngine外部component展開 + 重複style import対応 | Done | Codex + PM | 16 | 9 |
| 18 | sheetOptions at="名前"→実座標マッピング実装 | Done | Codex + PM | 17 | 9 |
| 19 | formulaRef / #{...}プレースホルダ置換実装 | Done | Codex + PM | 17 | 9 |
| 20 | FullTemplate E2Eテスト（実xlsx生成検証） | Done | Codex + PM | 18, 19, 21 | 9 |
| 21 | sheet/gridのrows/cols省略→自動計算 + Design XML修正 | Done | Codex + PM | 17 | 9 |
| 22 | sheet repeat 対応の調査レポート + 設計書 + 先行テスト作成 | Done | Codex + PM | 21 | 10 |
| 23 | sheet repeat 実装 (DSL/AST/ValidateDsl/LayoutEngine/Renderer互換確認) | Done | Codex + PM | 22 | 10 |


## Additional Work

- 2026-04-05 PR #52: README刷新+QuickStart改善を `master` 向けに提出し、`Closes #48` で issue連携
- 2026-04-05 README QuickStart改善: 英日 README の QuickStart DSL を component定義型へ変更（`ItemHeader` / `ItemRow` + `repeat` + `use`）
- 2026-04-05 README刷新: `README.md` / `README.ja.md` を Chart・Async対応の内容に全面更新（DSL v2、chart DSL例、Async進捗ポーリング例）
- 2026-04-05 README方針: ユーザー指摘を反映し、ClosedXML との対比説明は記載しない方針で統一
- 2026-04-05 PR #51: Codexレビュー2件（`TryGetResult` 完了競合 / `Cancel` dispose競合）を実装修正
- 2026-04-05 PR #51: 実施記録 `reports/pr51-codex-review-fixes-2026-04-05.md` を追加
- 2026-04-05 issue #43 follow-up: `AsyncReportJobStatus` に `ElapsedMilliseconds` / `CurrentPhaseElapsedMilliseconds` / `PhaseElapsedMilliseconds` を追加し、遅延箇所可視化を可能化
- 2026-04-05 issue #43 follow-up: `RenderingCompletedUnits` / `RenderingTotalUnits` を追加し、描画予定数ベースの細粒度進捗を取得可能化
- 2026-04-05 issue #43 follow-up: `RenderOptions.ProgressReporter` を追加し、レンダラー進捗を `AsyncReportGenerator` へ連携
- 2026-04-05 issue #43 follow-up: `AsyncReportGeneratorTests` を拡張し、slow rendererのフェーズ時間と途中ユニット進捗を検証
- 2026-04-05 issue #43 follow-up: 実施記録 `reports/issue43-progress-timing-followup-2026-04-05.md` を追加
- 2026-04-05 issue #37: グラフ作成機能（chart）を実装完了（DSL/AST/XSD/Layout/State/Renderer）
- 2026-04-05 issue #37: 設計レビュー -> 実装方針策定 -> SubAgentレビュー -> 実装の順で実施し、レビュー指摘を反映
- 2026-04-05 issue #37: Chart設計書 `Design/Chart/Chart_DetailDesign.md` を追跡内容へ追加
- 2026-04-05 issue #37: `ExcelReportLib.Tests` 全体回帰を実施し `Passed 185, Failed 0` を確認
- 2026-04-05 issue #37: 実施記録 `reports/issue37-chart-design-and-plan-review-2026-04-05.md` を追加
- 2026-04-05 issue #37 PR#49 follow-up: Codexレビュー2件（fallback色のシート跨ぎ一貫性 / chart座標Excel上限）を修正し、回帰テスト2件を追加
- 2026-04-05 issue #37 PR#49 follow-up: 調査記録 `reports/pr49-codex-review-followup-2026-04-05.md` を追加
- 2026-04-05 設計改善: `DslDefinition_DetailDesign.md` に属性単位の逆引き章（`area`/`formulaRef`/`at` など）を追加

- 2026-03-26 issue #45: named target属性の完全破壊変更（`area` 統一）を実装完了（`repeat@area`/`use@area`/`grid@area`）
- 2026-03-26 issue #45: `INamedAreaTarget.AreaName` による共通解決へ統一し、legacy属性（`name`/`instance`）拒否をテストで担保
- 2026-03-26 検証: `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore` 実行結果 `Passed 161, Failed 0`
- 2026-03-26 issue #45: DSL namespace/schema を v2 へ完全移行（`urn:excelreport:v2` / `DslDefinition_v2.xsd` / `*_v2.xml`）
- 2026-03-26 issue #45: parser/import に v2 namespace 強制チェックを追加し、v1互換を明示的に拒否
- 2026-03-26 issue #45(P1): top-level sibling ごとに scopePath を分離し、local formulaRef の sibling 混在を防止
- 2026-03-26 検証: 追加テスト3件 + 影響範囲回帰 + 全体回帰を実行し `Passed 165, Failed 0` を確認

- 2026-03-25 #35 E2E追加: `ReportGeneratorTests` に repeat + `formulaRefScope="local"` の実xlsx生成テストを追加
- 2026-03-25 検証: `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj` で追加E2E含む全件通過
- 2026-03-25 記録: `reports/issue35-e2e-repeat-local-scope-2026-03-25.md` を作成

- 2026-03-24 CI追加: `pull_request -> master` トリガーで `ExcelReportLib.Tests` を実行するworkflowを新規追加
- 2026-03-24 記録: `reports/pr-xunit-workflow-2026-03-24.md` を作成

- 2026-03-24 NuGet README対応: package readme metadata/content を追加し、release起点publishで警告が出ない状態へ修正
- 2026-03-24 記録: `reports/nuget-package-readme-fix-2026-03-24.md` を作成

- 2026-03-24 不具合修正: csx public型名（`#` を含む）での式評価失敗を修正（型名構文検証 + dynamicフォールバック）
- 2026-03-24 検証: `ExpressionEngineTests`（12件）全通過
- 2026-03-24 記録: `reports/csx-expression-hash-type-investigation-2026-03-24.md` を作成

- 2026-03-19 設計更新: `Design/ExpressionEngine/ExpressionEngine.md` を Roslyn実装前提に改訂
- 2026-03-19 レビュー: `reports/roslyn-expression-design-review-2026-03-19.md` を作成
- 2026-03-19 実装: `ExpressionEngine` を Roslyn (`Microsoft.CodeAnalysis.CSharp.Scripting`) ベースへ移行
- 2026-03-19 レビュー: `reports/roslyn-expression-implementation-review-2026-03-19.md` を作成
- 2026-03-19 検証: `ExcelReportLib.Tests` 110件全通過
- 2026-03-19 運用: `.gitignore` に `/.nuget` を追加し NuGet フォルダを非追跡化
- 2026-03-19 追加要件対応: テンプレート内LINQ式のE2Eテストを追加（`repeat@from` + `cell@value`）
- 2026-03-19 実装改善: `ExpressionEngine` を `root/data` 強型付けコンパイル対応（公開型）へ拡張
- 2026-03-19 設計反映: `Design/ExpressionEngine/ExpressionEngine.md` を実装内容に同期更新
- 2026-03-19 検証: `ExcelReportLib.Tests` 111件全通過
- 2026-03-19 記録: `reports/linq-template-e2e-roslyn-note-2026-03-19.md` を作成
- 2026-03-19 レビュー: `reports/linq-template-e2e-implementation-review-2026-03-19.md` を作成
- 2026-03-19 CI/CD: GitHub Releaseのpre-release/release種別に連動してNuGet版種を同期するよう `publish-nuget.yml` を更新

- 2026-03-19 ドキュメント運用: READMEからNuGet公開手順を削除し、`reports/nuget-publish-process-2026-03-19.md` に移管

## Status Definitions

- Done: 完了
- In Progress: 実施中
- Not Started: 未着手
