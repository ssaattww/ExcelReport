# Tasks Status

Last Updated: 2026-03-24
Scope: ExcelReport開発 - Phase 10: sheet repeat対応

## Progress Summary

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


## Additional Work (2026-03-19)

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



