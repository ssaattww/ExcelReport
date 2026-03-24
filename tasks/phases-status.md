# Phases Status

Last Updated: 2026-03-24

## Overall Progress

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

