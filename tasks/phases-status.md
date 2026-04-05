# Phases Status

Last Updated: 2026-04-05

## Overall Progress

- 2026-04-05: issue #43 follow-up として、`AsyncReportJobStatus` に総経過時間/フェーズ別経過時間/現在フェーズ経過時間を追加
- 2026-04-05: issue #43 follow-up として、`RenderingCompletedUnits` / `RenderingTotalUnits` を追加し、描画予定数ベースの細粒度進捗を追加
- 2026-04-05: issue #43 follow-up として、`AsyncReportGenerator` に `phase + timestamp` ベースの時間集計を実装し、実行中スナップショットを返却
- 2026-04-05: issue #43 follow-up として、`RenderOptions.ProgressReporter` を追加し、レンダラー進捗を非同期ステータスへ連携
- 2026-04-05: issue #43 follow-up として、`Remove` を終端状態のみ許可に統一し、削除時 `CancellationTokenSource` をDispose
- 2026-04-05: issue #43 follow-up 検証として `AsyncReportGeneratorTests` 7件 + 全体198件通過を確認
- 2026-04-05: issue #43 設計書へポーリング取得例（実行時間/フェーズ時間/Rendering細粒度進捗）を追記
- 2026-04-05: issue #43 設計書へGUI（WinForms/WPF）でUIを固めない利用例を追記
- 2026-04-05: issue #43 follow-up 記録 `reports/issue43-progress-timing-followup-2026-04-05.md` を追加
- 2026-04-05: issue #43 非同期API（non-blocking job + status/result/cancel）を設計→方針確定→実装の順で完了
- 2026-04-05: 設計書 `Design/ReportGenerator/ReportGenerator_AsyncApi_DetailDesign.md` を追加
- 2026-04-05: 調査記録 `reports/issue43-async-api-design-and-implementation-2026-04-05.md` を追加
- 2026-04-05: issue#37 のSubAgentレビューを3分割で実施し、各レビュー結果を `reports/subagent-review-issue37-chunk{1..3}-2026-04-05.md` に出力
- 2026-04-05: レビュー統合サマリー `reports/subagent-review-issue37-summary-2026-04-05.md` を追加
- 2026-04-05: レビューHigh指摘対応として chart series OpenXML子要素順を修正し、chart XMLスキーマ妥当性テストを追加
- 2026-04-05: `formulaRef` 系列解決を専用map化し、named area `<name>End` との衝突による誤解決を回避
- 2026-04-05: `TestDsl/DslDefinition_v2.xsd` を Design版へ同期し、`sheet/repeat` の `from/var` 属性・子要素両対応を反映
- 2026-04-05: `DslParser` の静的座標検証（rows/cols省略時）とルート要素名検証を強化
- 2026-04-05: 回帰として `dotnet test --no-restore` 全件実行し `Passed 189, Failed 0` を確認
- 2026-04-05: issue#37 Chart設計書 `Design/Chart/Chart_DetailDesign.md` を追跡対象へ追加し、tasks/phases の進捗管理に反映
- 2026-04-05: issue#37 設計レビューを実施し、実装方針を策定（chart DSL/AST/XSD -> Layout -> WorksheetState -> Renderer）
- 2026-04-05: SubAgent（Socrates）に実装方針レビューを依頼し、scopePath維持・責務分離・異常系方針の指摘を反映
- 2026-04-05: issue#37 の実装と回帰検証を完了し、`dotnet test --no-restore` で `Passed 185, Failed 0` を確認
- 2026-03-26: GitHub xunit-tests 失敗（`Expand_WhenLocalAndImportedComponentsShareName_LocalComponentWins`）を修正し、`componentImport` 厳格化後のfixture整合を反映
- 2026-03-26: 回帰として `dotnet test --no-restore` を全件実行し `Passed 179, Failed 0` を確認
- 2026-03-26: ユーザー指定レビュー指摘3-6に対応し、global/local衝突解決・sheet-scope fallback非リーク化・importルート厳格検証を実装
- 2026-03-26: 追加テスト（WorksheetState/ReportGenerator/ValidateDsl/ComponentImport/StyleImport）を反映し、関連76件テスト全通過を確認
- 2026-03-26: PR#47レビュー指摘に対応し、`sheet` 直下 sibling `cell` の local formulaRef 共有スコープ回帰を修正
- 2026-03-26: `LayoutEngineTests` / `ReportGeneratorTests` に回帰テストを追加し、`WorksheetState/ReportGenerator/LayoutEngine` 関連83件テスト全通過を確認
- 2026-03-26: local `formulaRef` 曖昧解決時のフォールバック/タイブレークで Warning 必須化（`IssueKind.FormulaRefResolutionFallback`）を実装
- 2026-03-26: `WorksheetStateBuilder` -> `ReportGenerator` へ worksheet-state warning の集約経路を追加し、`ReportGeneratorResult.Issues` / logger に反映
- 2026-03-26: 検証として `dotnet test --filter "WorksheetStateTests|ReportGeneratorTests"` を実行し 57件全通過を確認
- 2026-03-26: `dotnet restore` の権限問題（AppData/NuGet.Config ACL）を昇格実行で回避し、その後通常権限で `dotnet test --no-restore --no-build` 全165件通過を確認
- 2026-03-26: local可視性方針を最終確定し、同一親siblingでの参照を許可するため `WorksheetStateBuilder.FindNamedArea` に子孫ローカル一意解決を追加
- 2026-03-26: `local` 可視性仕様を再調整（同一sibling可視・sibling内側不可視）し、`ExpandGrid` の child scope を `cell` とコンテナで分岐
- 2026-03-26: publish workflowの push版数基準を `VersionPrefix` チャネルタグへ変更し、`X.Y.Z-pre` の patch増分運用を維持
- 2026-03-26: sub-agentレビューで local formulaRef の nested scope 混線リスク（P1/P2）を受領
- 2026-03-26: 設計書ファイル名のバージョンサフィックス（`_v1`等）を廃止（XSDは例外）し、Design配下の対象Markdownをリネーム
- 2026-03-26: 実装後レビューを実施し、`reports/issue45-area-breaking-change-review-2026-03-26.md` に top-level sibling 間の local formulaRef スコープ混在リスク（P1）を記録
- 2026-03-26: issue#45の破壊的変更として named target 属性を `area` に統一（`repeat@area` / `use@area` / `grid@area`）
- 2026-03-26: 旧 `repeat@name` / `use@instance` / `grid@name` を非対応化（ASTでError化）
- 2026-03-26: `INamedAreaTarget.AreaName` による共通解決経路へ統合（DslParser/LayoutEngine）
- 2026-03-26: `ReportGeneratorTests` を中心に repeat/use/grid/sheet/formulaRef/local non-leak の条件付き書式E2Eを検証（14件通過）
- 2026-03-26: 追加検証として area移行テスト（LayoutEngine/SheetAst/WorksheetState/ValidateDsl/LayoutNode/FullTemplate）を実施し全件通過
- 2026-03-26: 広域回帰として Parser/Layout/Renderer/ReportGenerator を横断する 127テストを `--no-build --no-restore` で実行し全通過
- 2026-03-26: 全体回帰 `dotnet test --no-build --no-restore` を実行し 161件全通過
- 2026-03-26: repo直下の一時実行痕跡 `.appdata/.nuget/.dotnet` を削除し、以降の test 環境は `%TEMP%` 配下へ分離
- 2026-03-26: `%TEMP%/excelreport-codex-env` で `dotnet restore` を成功させた上で、`--no-restore` の再ビルド回帰（127件）と全件回帰（161件）を再確認
- 2026-03-26: sandbox環境制約対応として `APPDATA` / `DOTNET_CLI_HOME` / `NUGET_PACKAGES` をワークスペース配下へ切替えて `dotnet test` を実行
- 2026-03-26: `Design/BreakingChanges.md` を英語化し、予定バージョン表記を `X.Y.Zより後`（`after X.Y.Z`）へ統一
- 2026-03-26: `publish-nuget.yml` から BreakingChanges の自動検証ステップを削除（Actionによる強制チェックを廃止）
- 2026-03-26: issue#45 着手。`gh issue view 45` で要件を取得し、`conditionalFormatting` の範囲ターゲットに formulaRef 系列（global/local）を使えるよう調査を開始
- 2026-03-26: issue#45 実装として `conditionalFormatting@at` に formulaRef 系列名（global/local）を指定した範囲解決を追加
- 2026-03-26: local series の複数スコープ展開を追加し、同名の local/global 競合時は local 優先へ統一
- 2026-03-26: `WorksheetStateTests` 3件 + `ReportGeneratorTests` 2件を追加し、`ConditionalFormatting` フィルタ 15件全通過を確認
- 2026-03-26: 調査記録 `reports/issue45-conditional-formatting-formularef-target-2026-03-26.md` を追加
- 2026-03-25: PR#41 inline指摘対応として `conditionalFormatting@at` の単一セル指定（例: `A1`）をレンダラー解決対象に追加
- 2026-03-25: 回帰テスト `Render_ConditionalFormatting_SingleCellTarget_IsRendered` を追加
- 2026-03-25: 全体テスト `ExcelReportLib.Tests` 141件全通過を確認、記録 `reports/pr41-inline-single-cell-target-fix-2026-03-25.md` を追加
- 2026-03-25: PR#41最新レビュー指摘のフォローアップとして2色colorScaleの子要素順序を回帰テスト化
- 2026-03-25: 追加テスト+全体テストを実行し `ExcelReportLib.Tests` 140件全通過を確認
- 2026-03-25: 調査記録 `reports/pr41-latest-review-followup-2026-03-25.md` を追加
- 2026-03-25: ユーザー依頼に基づき .NET SDK 8.0.419 を `/workspace/.dotnet` へ導入
- 2026-03-25: `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj` を再実行し 139件全通過を確認
- 2026-03-25: 実行記録 `reports/test-run-dotnet-sdk-8.0.419-2026-03-25.md` を追加
- 2026-03-25: PRフォローアップとして conditionalFormatting の `fontBold/fontItalic/fontUnderline` で XML boolean literal（`1/0`）を受理
- 2026-03-25: `sheetOptions/conditionalFormatting@formulaRef` の local scope 解決を `at` 対象レンジ交差ベースで補完
- 2026-03-25: 回帰テスト2件を追加し、調査記録 `reports/pr-followup-conditional-formatting-local-scope-and-bool-2026-03-25.md` を作成
- 2026-03-25: ユーザー提示の最新運用ルールに合わせて `AGENTS.md` を更新（Skills運用セクションを追加）
- 2026-03-25: 調査記録 `reports/agents-instructions-sync-2026-03-25.md` を追加
- 2026-03-25: PR#41レビュー指摘（CT_ColorScale順序）に対応し、cfvo先行・color後続の出力順へ修正
- 2026-03-25: `RendererTests` に colorScale 子要素順序の回帰アサーションを追加
- 2026-03-25: `ExcelReportLib.Tests` 137件全通過を確認
- 2026-03-25: 調査記録 `reports/pr41-review-cfvo-order-fix-2026-03-25.md` を追加
- 2026-03-25: レビュー結果対応として expression+formulaRef 条件付き書式の OpenXML 妥当性E2Eテストを追加
- 2026-03-25: `ReportGeneratorTests` に schema validation ケースを追加
- 2026-03-25: `ExcelReportLib.Tests` 137件全通過を確認
- 2026-03-25: 調査記録 `reports/issue34-review-followup-2026-03-25.md` を追加
- 2026-03-25: issue#34追加要望対応として expression条件付き書式の `formulaRef` 指定をサポート
- 2026-03-25: `ReportGeneratorTests` に conditional formatting E2E（2色/3色/expression+formulaRef）を追加
- 2026-03-25: `ExcelReportLib.Tests` 136件全通過を確認
- 2026-03-25: 調査記録 `reports/issue34-conditional-formatting-formularef-e2e-2026-03-25.md` を追加
- 2026-03-25: issue#34追加要望対応として、expression条件一致時に cell相当書式（font/numberFormat/border/fill）を適用可能に拡張
- 2026-03-25: `conditionalFormatting` のXSD/AST/State/Rendererを拡張し、dxf生成を属性駆動で実装
- 2026-03-25: 調査記録 `reports/issue34-conditional-formatting-style-settings-2026-03-25.md` を追加
- 2026-03-25: issue#34 追加要望対応として 3色colorScale（midColor）と expression条件式（formula）を実装
- 2026-03-25: expression条件一致時の塗り変更（fillColor -> dxf/FormatId）を Renderer へ追加
- 2026-03-25: `ExcelReportLib.Tests` 133件全通過を確認
- 2026-03-25: 調査記録 `reports/issue34-conditional-formatting-extensions-2026-03-25.md` を追加
- 2026-03-25: issue#34のレビュー指摘に対応し、設計書へ条件付き書式の対応範囲（2色colorScale限定）を追記
- 2026-03-25: `Design/DslDefinition/DslDefinition_DetailDesign_v1.md` に `conditionalFormatting` 仕様節を追加
- 2026-03-25: `Design/BasicDesign_v1.md` のDSL要素一覧を `conditionalFormatting` 追加内容へ同期
- 2026-03-25: 調査記録 `reports/issue34-design-doc-update-2026-03-25.md` を追加
- 2026-03-25: issue#34対応として `sheetOptions/conditionalFormatting` を追加し、OpenXML colorScale 形式で出力可能に拡張
- 2026-03-25: `SheetAstTests` / `WorksheetStateTests` / `RendererTests` へ条件付き書式テストを追加
- 2026-03-25: `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj` 132件全通過を確認
- 2026-03-25: 調査記録 `reports/issue34-conditional-formatting-2026-03-25.md` を追加
- 2026-03-25: PR #40 再対応として、`LayoutEngine` の scopePath 連結ロジック（sheet/gridのchildIndex付与）を再修正
- 2026-03-25: 回帰テスト `Expand_RepeatGridSiblings_ShareSameScopePath` を追加
- 2026-03-25: .NET SDK 8.0.419 を導入し、`dotnet test` による実行検証を実施
- 2026-03-25: #35 の再発防止として `ReportGeneratorTests` に repeat + `formulaRefScope="local"` のE2Eテストを追加
- 2026-03-25: `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj` で追加E2E含む全件通過を確認
- 2026-03-25: 調査記録 `reports/issue35-e2e-repeat-local-scope-2026-03-25.md` を追加
- 2026-03-25: `RendererTests` の `LayoutCell` ヘルパー引数を現行コンストラクタへ追随修正
- 2026-03-25: `ExcelReportLib.Tests` 129件全通過を確認
- 2026-03-25: 実行記録 `reports/pr39-dotnet-sdk-test-run-2026-03-25.md` を追加
- 2026-03-25: PR #39 指摘対応として `formulaRefScope` を XSD列挙（`local|global`）へ制約し typo 混入を防止
- 2026-03-25: `CellAst` に不正 `formulaRefScope` の Warning 記録 + `global` 正規化を追加
- 2026-03-25: `LayoutNodeTests` に不正 `formulaRefScope` の回帰テストを追加
- 2026-03-25: 調査記録 `reports/pr39-inline-comments-fix-2026-03-25.md` を追加
- 2026-03-24: PR #38 CI失敗（`WorksheetStateBuilder` の `LastIndexOf` 呼び出し不整合）を修正し、コンパイルエラーを解消
- 2026-03-24: 調査記録 `reports/pr38-ci-fix-worksheetstate-lastindexof-2026-03-24.md` を追加
- 2026-03-24: issue#35対応として `cell@formulaRefScope`（local/global）を追加し、formulaRef の参照範囲を指定可能に拡張
- 2026-03-24: `LayoutEngine` でセルごとに `scopePath` を保持し、`WorksheetStateBuilder` で最寄りスコープ優先の placeholder 解決を実装
- 2026-03-24: 検証として `WorksheetStateTests` に local scope + global fallback の回帰テストを追加
- 2026-03-24: 調査記録 `reports/issue35-formula-ref-scope-2026-03-24.md` を追加
- 2026-03-24: `cell` の `value` を属性と子要素(`<value>`)の両方で指定可能に拡張
- 2026-03-24: `cell` の `value` 競合時（属性+子要素）に Warning を記録し、属性値を優先する互換ルールを追加
- 2026-03-24: `DslDefinition_v1.xsd` の `CellType` に `<value>` 要素を追加し、schema有効時の記法を拡張
- 2026-03-24: 調査記録 `reports/cell-value-element-support-2026-03-24.md` を追加
- 2026-03-24: repeat内の括弧付き式（@((p...))）で var 未解決となる不具合を修正
- 2026-03-24: var書き換えを先頭一致判定から式全体のRoslyn構文木置換へ一般化
- 2026-03-24: 調査記録 reports/rewrite-repeat-var-robust-2026-03-24.md を追加
- 2026-03-24: 入れ子repeat条件式 (`m.Name != "Machine1" ? m.Name : ""`) で `m` が未解決になる不具合を修正
- 2026-03-24: `LayoutEngine` の var スコープ式書き換えをRoslyn構文木ベースへ拡張し、式中の複数参照を一括置換
- 2026-03-24: 調査記録 `reports/repeat-var-conditional-expression-fix-2026-03-24.md` を追加
- 2026-03-24: ルートREADMEのbuildステータスバッジを実ワークフロー表示へ修正
- 2026-03-24: NuGet同梱READMEをリポジトリREADMEへ一本化し、専用READMEを削除
- 2026-03-24: 調査記録 `reports/readme-badge-and-package-readme-sync-2026-03-24.md` を追加
- 2026-03-24: master push時のprerelease suffixを `-pre` 固定へ変更（`-pre.<run_number>` を廃止）
- 2026-03-24: master push時に GitHub pre-release を自動作成する処理を publish workflow に追加
- 2026-03-24: 調査記録 `reports/master-push-prerelease-auto-version-2026-03-24.md` を NuGet/GitHub 両対応内容へ更新
- 2026-03-24: `sheet` / `repeat` の from/var の子要素指定を追加し、属性形式との後方互換を維持
- 2026-03-24: 属性と子要素の同時指定時に Warning を記録し、属性値を優先する排他ルールを実装
- 2026-03-24: 設計書 `Design/DslDefinition/DslDefinition_DetailDesign_v1.md` / `Design/DslParser/DslParser_DetailDesign_v1.md` を実装仕様に同期
- 2026-03-24: 調査記録 `reports/from-var-element-support-2026-03-24.md` を追加
- 2026-03-24: 調査記録 `reports/from-var-element-design-doc-sync-2026-03-24.md` を追加
- 2026-03-24: `publish-nuget.yml` を拡張し、master push時にNuGet prereleaseを自動publishする運用へ更新
- 2026-03-24: 最新安定タグ基準でpatch(3桁目)を自動増分するバージョン決定ロジックを追加
- 2026-03-24: 調査記録 `reports/master-push-prerelease-auto-version-2026-03-24.md` を追加
- 2026-03-24: master向けPRでxUnitテストを実行するCI workflow（`pr-xunit-tests.yml`）を追加
- 2026-03-24: 調査記録 `reports/pr-xunit-workflow-2026-03-24.md` を追加
- 2026-03-24: NuGetパッケージにREADMEを同梱する修正を追加（`PackageReadmeFile` + package content）
- 2026-03-24: `ExcelReportLib/README.md` を追加し、releaseトリガーpublishでのREADME警告解消に対応
- 2026-03-24: 調査記録 `reports/nuget-package-readme-fix-2026-03-24.md` を追加
- 2026-03-24: csx public型（`Submission#...`）で式評価が失敗する不具合を修正（型名構文検証 + dynamicフォールバック）
- 2026-03-24: 回帰テスト `Evaluate_TypeNameContainingHash_UsesDynamicFallback` を追加し、ExpressionEngineテスト12件の通過を確認
- 2026-03-24: 調査記録 `reports/csx-expression-hash-type-investigation-2026-03-24.md` を追加
- 2026-03-19: `sheet repeat` 対応の調査レポートを作成
- 2026-03-19: `sheet repeat` 詳細設計を Design/DslDefinition/DslDefinition_DetailDesign_v1.md に統合更新
- 2026-03-19: 実装前の先行テスト (Red) を追加して未対応ギャップを可視化
- 2026-03-19: `sheet repeat` 実装を反映し、関連テストをGreen化
- 2026-03-19: `ExcelReportLib.Tests` 105件全通過を確認
- 2026-03-19: RoslynベースのC#式評価へ移行し、ExpressionEngineテスト拡張 + 全110件テスト通過を確認
- 2026-03-19: テンプレート内LINQ式（repeat@from + cell@value）のE2Eテスト追加と、ExpressionEngineの強型付けコンパイル対応を反映
- 2026-03-19: DynamicLinqRewriteMap フォールバックを導入し、匿名型入力の template LINQ E2E をGreen化
- 2026-03-19: GitHub Release の pre-release/release と NuGet publish 版種の同期ロジックを publish-nuget.yml に追加
- 2026-03-19: READMEからNuGet公開手順を削除し、運用情報をreportsへ移管
- 2026-03-26: issue #45 の破壊的変更（named target属性の `area` 完全統一）を完了
- 2026-03-26: 回帰検証として `ExcelReportLib.Tests` 全体を実行し `Passed 161, Failed 0` を確認
- 2026-03-26: issue #45 の follow-up として DSL namespace/schema を v2（`urn:excelreport:v2`/`DslDefinition_v2.xsd`）へ完全移行
- 2026-03-26: top-level sibling scope を分離し、local formulaRef の sibling 混在不具合（P1）を修正
- 2026-03-26: 追加テスト3件 + 主要回帰 + 全体回帰を実施し `Passed 165, Failed 0` を確認
- 2026-04-05: PR #49 Codexレビュー指摘2件（chart fallback色のワークブックスコープ化 / chart座標Excel上限チェック）を反映
- 2026-04-05: 調査記録 `reports/pr49-codex-review-followup-2026-04-05.md` を追加
- 2026-04-05: 設計書 `DslDefinition_DetailDesign.md` に属性逆引き章を追加（`area` に限定しない属性単位整理）
- Completed Phases: 10 / 10
- In Progress Phases: 0 / 10
- Overall Progress: 100%

## Phase Summary

| Phase | Status | Progress | Purpose | Major Deliverables |
|---|---|---:|---|---|
| Phase 1: DSL契約の一本化 | Completed | 100% | Design/とTestDsl/のDSL記法統一、サンプルXML/XSD/実装の三者整合 | 統一済みXSD、統一済みサンプルXML、記法整合性レポート |
| Phase 2: DslParser完成 | Completed | 100% | XSD検証有効化、ValidateDsl実装、未実装属性取り込み、テスト追加 | 完成DslParser、単体テストプロジェクト |
| Phase 3: Styles + ExpressionEngine | Completed | 100% | スタイル解決・式評価の実装 | Styles resolver、ExpressionEngine |
| Phase 4: LayoutEngine | Completed | 100% | repeat/use/grid/cell展開、最終スタイル決定 | LayoutEngine、LayoutPlan |
| Phase 5: WorksheetState | Completed | 100% | LayoutPlanから最終状態への固定化 | WorksheetState |
| Phase 6: Renderer | Completed | 100% | .xlsx物理出力、Issuesシート、Auditシート | Renderer |
| Phase 7: Logger + ReportGenerator | Completed | 100% | 横断ログ導入、全体統合ファサード、E2Eテスト | Logger、ReportGenerator |
| Phase 8: Border修正+テスト拡充 | Completed | 100% | CT_Border順序修正、Grid border展開、FullTemplate E2E | Border順序修正、Grid border展開 |
| Phase 9: FullTemplate実行対応 | Completed | 100% | FullTemplate XMLが実行できるようにする | ファイルパスベース実行、外部component展開、sheetOptions名前解決、formulaRef実装、E2Eテスト |
| Phase 10: sheet repeat 対応 | Completed | 100% | sheetレベル反復のDSL拡張と実装 | 調査レポート、設計書、先行テスト、実装 |

## Status Definitions

- Not Started: 未着手
- In Progress: 実施中
- Completed: 完了
