# Integration Implementation Plan
対象: `claude-code-workflows` と現行 `codex-workflow` の統合実装  
日付: 2026-02-13  
前提分析: `reports/workflow-integration-analysis.md`

## Executive Summary

本計画は、役割分担を以下に固定するための段階的統合を定義する。

- Claude Code層: オーケストレーション、タスク管理、承認フロー、品質ゲート、単純作業スキル
- Codex層: コード読解・分析、実装・修正、調査・診断

最優先は P0 の4項目である。

1. 入口一本化（`backend-*` と `codex-*` の競合解消）
2. Stop点・承認フローの明文化
3. Codex実行契約（入出力仕様）定義
4. sandboxマトリクス修正

この4項目を Phase 1 で先行実装し、Phase 2 で全ワークフローへ展開、Phase 3 で互換運用を収束する。

## Implementation Phases (Phase 1/2/3)

| Phase | 目的 | 対象期間目安 | Go条件 |
|---|---|---|---|
| Phase 1: P0基盤統合 | 入口統合、Stop/承認、契約、sandbox是正 | 3-5日 | `/implement` と `/design` の代表シナリオが新入口で通る |
| Phase 2: 全フロー展開 | 主要全コマンドへ契約とゲートを適用 | 4-7日 | 対象フロー全てで契約出力と承認停止が再現可能 |
| Phase 3: 収束・最適化 | 後方互換の段階的縮退、運用品質強化 | 3-5日 | 互換ルート未使用化、ロールバック手順の演習完了 |

## Detailed Steps per Phase

### Phase 1: P0基盤統合（最優先）

#### 1. 入口一本化（backend系とcodex系の統合）

Step-by-step:

1. 新規エントリースキル `workflow-entry` を作成する。  
   想定ファイル: `.claude/skills/workflow-entry/SKILL.md`
2. 既存の振り分け表を `workflow-entry` に移植し、実行先を「Claude制御 + Codex実行」に一本化する。
3. `backend-workflow-entry` と `codex-workflow-entry` は互換アダプタへ変更する。  
   想定変更: `.claude/skills/backend-workflow-entry/SKILL.md`, `.claude/skills/codex-workflow-entry/SKILL.md`
4. 互換アダプタは「新入口へ委譲するだけ」に制限し、独自ルーティングを禁止する。
5. ルーティング優先順位を明文化する。  
   優先順: `implement/build/task` > `review/diagnose` > `design/plan/update-doc/reverse-engineer` > `add-integration-tests`
6. 代表シナリオ（implement/design/diagnose）を実行し、二重解釈が起きないことを確認する。

成果物:

- `.claude/skills/workflow-entry/SKILL.md`
- 互換アダプタ化された既存2エントリースキル
- ルーティング整合テーブル（referenceファイル）

検証:

- 同一入力に対して常に同一ルートになる（3回再実行で一致）
- 旧エントリー経由でも実行経路が新入口に集約される

#### 2. Stop点・承認フローの明文化

Step-by-step:

1. 全フロー共通の停止タグを定義する。  
   形式: `[Stop: reason]`, `[Approve: phase-name]`
2. 各フローの必須Stop点を定義する。  
   最低必須: 設計承認前、実装開始前、高リスク変更前、品質ゲート失敗時
3. `codex-lifecycle-orchestration` と `codex-document-flow` に停止条件を追記する。  
   対象: `.claude/skills/codex-lifecycle-orchestration/SKILL.md`, `.claude/skills/codex-document-flow/SKILL.md`
4. 承認応答フォーマットを固定する。  
   例: `approved: true/false`, `scope_changes`, `constraints`
5. 「承認なし遷移禁止」を明文化し、例外条件を列挙する。  
   例外: 緊急停止、破壊的操作の中止のみ
6. 代表シナリオで Stop -> Approval -> Resume が再現できることを確認する。

成果物:

- Stop/Approval規約（reference）
- 各フロースキルのStop点定義更新

検証:

- Stop点で必ず停止し、承認入力後のみ次フェーズに遷移する
- 承認拒否時に安全停止し、代替アクションを返す

#### 3. Codex実行契約（入出力仕様）の定義

Step-by-step:

1. 契約仕様 `codex-execution-contract.md` を追加する。  
   想定: `.claude/skills/workflow-entry/references/codex-execution-contract.md`
2. 入力スキーマを定義する。
   - 必須: `objective`, `scope`, `constraints`, `acceptance_criteria`, `allowed_commands`, `sandbox_mode`
   - 任意: `context_files`, `known_risks`, `stop_conditions`
3. 出力スキーマを定義する。
   - 必須: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`
   - `status` 値: `completed | needs_input | blocked | failed`
4. `codex/SKILL.md` に「契約に従うこと」を明記し、プロンプトテンプレートを契約準拠へ修正する。
5. `codex-workflow-entry` 互換アダプタも同契約を参照するよう更新する。
6. 契約準拠チェックリストを作成し、3種類の実行（implement/review/diagnose）で検証する。

成果物:

- Codex実行契約ドキュメント
- 契約参照が埋め込まれた `codex` 関連スキル
- 契約準拠チェックリスト

検証:

- 出力に必須フィールド欠落がない
- `status=needs_input` 時に停止して承認/追加入力を要求する

#### 4. sandboxマトリクス修正

Step-by-step:

1. 現行マトリクスを「読取専用」と「書込可能」で再分類する。
2. 文書生成系を `workspace-write` に修正する。  
   対象: `design`, `plan`, `update-doc`, `reverse-engineer`
3. `review`/`diagnose` は `read-only` 開始を維持し、修正フェーズで `workspace-write` へ昇格する2段階方式に変更する。
4. 昇格条件を明文化する。  
   例: 修正適用が必要で、承認が得られた場合のみ
5. `codex-workflow-entry/SKILL.md` と `codex/SKILL.md` の表記を同期する。
6. 5ケース以上の実行テストで期待sandboxが選ばれることを確認する。

成果物:

- 改定sandboxマトリクス
- 昇格ルール（read-only -> workspace-write）

検証:

- 文書更新タスクで書き込み可能モードが選択される
- review/diagnoseで未承認のまま書き込みモードに遷移しない

#### Phase 1 ロールバック

- 新規 `workflow-entry` を無効化し、互換アダプタの直接ルーティングへ即時復帰できる状態を維持する。
- 契約仕様は削除せず「非強制モード」に戻し、出力欠落時は警告のみへ降格する。
- sandboxは旧表を `references/sandbox-matrix.legacy.md` として保持し、即時参照可能にする。

---

### Phase 2: 全フロー展開（運用移行）

実装項目:

1. すべての対象フローに共通契約を適用する。  
   対象: `implement`, `task`, `build`, `review`, `diagnose`, `design`, `plan`, `update-doc`, `reverse-engineer`, `add-integration-tests`
2. 各フローに必須Stop点を埋め込み、フェーズ遷移条件を統一する。
3. 品質ゲート出力を標準フォーマット化する。  
   例: `gate_name`, `result`, `evidence`, `retry_count`
4. 基礎スキル群を `.claude/skills` に復元し、`Always-Use` 群を明示する。  
   対象: `ai-development-guide`, `coding-principles`, `testing-principles`, `documentation-criteria`, `implementation-approach`, `integration-e2e-testing`, `task-analyzer`
5. 互換期間中は旧スキル名を維持し、内部で新仕様へ委譲する。

成果物:

- 全フロー統一版スキルセット
- 標準品質ゲートレポート雛形
- 復元された基礎スキル群

検証方法:

- フロー別E2Eシナリオ（最低1件/フロー）を実行
- Stop/Approvalのトレースログが採取できる
- 契約必須フィールド欠落率 0%

Phase 2 ロールバック:

- フロー単位で旧実装へ戻せるよう、互換アダプタを削除しない
- 問題発生フローのみ旧ルートへ部分ロールバックする

---

### Phase 3: 収束・最適化（後方互換の段階的終了）

実装項目:

1. 互換アダプタの利用統計を確認し、未使用期間を満たしたルートから廃止候補化する。
2. 旧入口の直接ルーティング記述を削除し、参照ドキュメントのみ残す。
3. 運用メトリクスを導入する。  
   指標: Stop通過率、承認差戻し率、品質ゲート失敗率、再オープン率
4. ロールバック演習を実施し、手順の実効性を確認する。
5. 最終版運用Runbookを確定する。

成果物:

- 収束後スキル構成（単一入口 + 互換最小化）
- 運用メトリクス定義と初期計測結果
- 最終Runbook

検証方法:

- 旧入口経由が0件であることを確認
- 主要フローで承認停止と品質ゲートが維持される

Phase 3 ロールバック:

- 互換アダプタ削除前のタグを保存し、必要時にリリース単位で巻き戻す
- メトリクス悪化時は廃止を延期し、Phase 2運用へ戻す

## Migration Strategy

### 段階移行方針

1. Dual-Route期間を設ける。  
   期間中は「旧入口名は有効、実体は新入口へ委譲」とする。
2. 互換アダプタに移行警告を付与する。  
   例: 「Deprecated: backend-workflow-entry -> workflow-entry」
3. フロー単位でカットオーバーする。  
   推奨順: `design/plan/update-doc/reverse-engineer` -> `implement/task/build` -> `review/diagnose` -> `add-integration-tests`
4. 機能フラグを導入する。  
   例: `workflow_entry_mode = unified | legacy-fallback`
5. 連続2営業日で重大障害なしを満たしたフローのみ次段階へ進める。

### 後方互換性への配慮

既存スキルの取り扱い:

- 既存スキル名は維持し、少なくとも1リリースは互換アダプタとして残す。
- 旧記法の入力でも新契約へ変換する変換レイヤを提供する。
- 旧ドキュメント参照パスを壊さないよう、参照ファイルは段階的に移動する。

移行期間中の動作保証:

- 保証対象: 既存10フローの呼び出し成立、Stop点停止、品質ゲート結果出力
- 非保証対象: 旧独自フォーマットの完全再現
- 障害時: フロー単位で legacy-fallback へ戻せることをSLOに含める

## Risk Assessment

| 実装項目 | リスクレベル | 主要リスク | 依存関係 | 緩和策 |
|---|---|---|---|---|
| 入口一本化 | High | ルーティング誤判定で誤フロー実行 | `workflow-entry` 新設、既存2入口改修 | 互換アダプタ維持、代表シナリオ再実行 |
| Stop/承認明文化 | High | 無承認遷移、運用停止漏れ | lifecycle/doc/quality系スキル更新 | Stopタグ強制、承認必須フィールド |
| Codex実行契約 | High | 出力不整合で制御不能 | `codex`/entry双方更新 | 必須フィールド検証、欠落時`blocked`扱い |
| sandboxマトリクス | Medium-High | read-onlyで文書更新失敗、過剰権限 | `codex-workflow-entry`, `codex` 同期 | 2段階昇格、承認なし昇格禁止 |
| 基礎スキル復元 | Medium | 指針不一致、記述重複 | `.claude/skills` 配置整理 | Always-Use定義の単一化 |
| メトリクス導入 | Medium | 測定不能、解釈不一致 | ログ仕様、出力契約 | 指標定義固定、収集方法をRunbook化 |

依存関係の要点:

1. `Codex実行契約` 完了前に `全フロー展開` は開始しない。
2. `sandboxマトリクス修正` は `入口一本化` と同時に適用する。
3. `Stop/承認明文化` は `品質ゲート標準化` より先に実装する。

## Rollback Plan

### ロールバックトリガー

- 承認なし遷移が1件でも検出された場合
- 主要フローのいずれかで契約必須フィールド欠落率が5%以上
- sandbox誤判定によるタスク失敗が連続2回発生

### ロールバック手順（段階）

1. 問題フローを `legacy-fallback` に切り替える。
2. 直前安定版タグへ参照を戻す。
3. 契約を strict から warn モードに一時緩和する。
4. 原因修正後、対象フローのみ再リリースする。
5. 24時間監視で再発がなければ統合モードへ復帰する。

### ロールバック責務

- Claude層: フロー停止判断、承認フロー再設定、切替宣言
- Codex層: 失敗ログと再現条件の提示、修正案の実装

## Acceptance Criteria

1. 新入口 `workflow-entry` から全10フローを起動できる。
2. 旧入口2種は互換アダプタとして動作し、新入口へ必ず委譲される。
3. `design/plan/update-doc/reverse-engineer` 実行時に `workspace-write` が選択される。
4. `review/diagnose` は `read-only` 開始で、承認後のみ `workspace-write` へ昇格する。
5. 全フローで Stop点が発火し、承認なしで次フェーズへ遷移しない。
6. Codex出力が契約必須項目（`status`, `changed_files`, `tests`, `quality_gate`, `blockers`）を常に含む。
7. 品質ゲート結果が完了報告に添付される。
8. 要件変更時に再分析フェーズへ戻る分岐が全フローで定義されている。
9. 互換モードへのロールバックがフロー単位で実行可能である。
10. 主要シナリオ（implement/design/diagnose/review）で連続3回の実行が安定する。

