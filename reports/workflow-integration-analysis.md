
# Workflow Integration Analysis
対象: `claude-code-workflows` と現行 `codex-workflow` 実装の統合分析
日付: 2026-02-13

## 0. 調査サマリ

本調査の結論は以下です。

1. 元 `claude-code-workflows` は「オーケストレーション（commands + orchestration skill）」と「
実務（agents）」を厳密分離した設計。
2. 現在の `.claude/skills` は、`backend-*`（Claude単体実行）と `codex-*`（Codex移譲）の2系統が
並立し、入口が二重化している。
3. 元実装で強かった統制（TodoWrite前提、`subagent_type`契約、明示Stop点、JSON応答契約）は、現
行実装で抽象化され、運用強制力が弱くなっている。
4. Codex移譲は「tmux経由実行」「意図別プロンプト」「sandbox切替」の骨格はあるが、`design/plan/
update-doc/reverse-engineer` が `read-only` 指定になっており、ドキュメント生成要件と矛盾してい
る。
5. 統合方針としては、Claudeを制御プレーン、Codexを実務プレーンに固定し、ワークフローごとに「停
止点」「品質ゲート」「Codex呼び出し契約」を明文化するのが最短。

---

## 1. `claude-code-workflows` 全体構造分析

### 1.1 ディレクトリ役割

- `claude-code-workflows/commands`
  - 役割: オーケストレーション定義（実行順序、停止点、承認フロー、委譲先指定）
  - 例: `implement.md`, `design.md`, `build.md`, `diagnose.md`, `reverse-engineer.md`
- `claude-code-workflows/agents`
  - 役割: 実務実行ユニット（要件分析、設計、実装、品質修正、調査など）
  - 例: `task-executor.md`, `quality-fixer.md`, `investigator.md`, `verifier.md`
- `claude-code-workflows/skills`
  - 役割: 共通ルール/判断基準（開発原則、テスト原則、文書基準、オーケストレーション規約）
  - 例: `subagents-orchestration-guide/SKILL.md`, `documentation-criteria/SKILL.md`
- `claude-code-workflows/backend`
  - 役割: backend plugin パッケージ。`commands/agents/skills` はルート実体へのシンボリックリン
ク
  - 実体参照設定: `backend/.claude-plugin/plugin.json`
- `claude-code-workflows/frontend`
  - 役割: frontend plugin パッケージ。backend同様、ルート実体へのシンボリックリンク
  - 実体参照設定: `frontend/.claude-plugin/plugin.json`

### 1.2 オーケストレーション機能 vs 実務機能の分類

オーケストレーション機能（Claude Code層向き）
- フロー制御: `commands/*.md`
- 停止点/承認: `commands/*.md` と `skills/subagents-orchestration-guide/SKILL.md`
- タスク進捗管理: TodoWrite運用
- 品質ゲート通過判定: `quality-fixer` の `approved` を起点とするコミット制御
- 要件変更時の再分析: requirement-analyzer 再起動規約

実務機能（Codex層へ移譲しやすい）
- コード実装: `agents/task-executor.md`, `agents/task-executor-frontend.md`
- 品質修正実行: `agents/quality-fixer.md`, `agents/quality-fixer-frontend.md`
- バグ調査/検証/解法: `agents/investigator.md`, `agents/verifier.md`, `agents/solver.md`
- 文書生成/検証: `agents/prd-creator.md`, `agents/technical-designer*.md`, `agents/code-verifi
er.md`

---

## 2. `.claude/skills` 現在実装の確認

### 2.1 実装済みスキル群

Codex系
- `codex-workflow-entry`
- `codex-lifecycle-orchestration`
- `codex-task-execution-loop`
- `codex-diagnose-and-review`
- `codex-document-flow`
- `codex`（Codex CLI実行）
- `tmux-sender`

Backend系（Claude単体実行）
- `backend-workflow-entry`
- `backend-lifecycle-execution`
- `backend-task-quality-loop`
- `backend-document-workflow`
- `backend-diagnose-workflow`
- `backend-integration-tests-workflow`

### 2.2 特徴

- `backend-*` は「サブエージェントなしでClaude単体実行」方針。
- `codex-*` は「Codex CLIに実務委譲」方針。
- 同じ対象ワークフローに2系統入口があり、どちらを正とするかが未統一。

### 2.3 backend-* 実装状況

- `/implement` 相当: `backend-lifecycle-execution` + `backend-task-quality-loop`
- `/design|/plan|/update-doc|/reverse-engineer` 相当: `backend-document-workflow`
- `/diagnose` 相当: `backend-diagnose-workflow`
- `/add-integration-tests` 相当: `backend-integration-tests-workflow`

---

## 3. 元実装との差分（失われた機能）

### 3.1 失われた/弱化したオーケストレーション機能

1. 厳密なタスク管理プロトコルの弱化
- 元実装: TodoWriteを各フェーズ/各サブエージェントで必須化
- 現行 `.claude/skills`: TodoWrite規約がほぼ消失（抽象記述中心）

2. 承認フローの強制力低下
- 元実装: `[Stop: ...]` マーカーに基づく明示停止と承認待ち
- 現行 `.claude/skills`: 「approval stop」概念はあるが、停止点の実装粒度が粗い

3. 構造化I/O契約の欠落
- 元実装: `subagent_type` 呼び出し + JSONフィールド（`approved`, `status`, `requiredFixes` な
ど）
- 現行 `.claude/skills`: 置換方針はあるが、実行時契約が弱い

4. 自律実行モードの運用規約簡略化
- 元実装: batch approval 後の厳密ループ、requirement change 検知時停止
- 現行 `.claude/skills`: 概念保持はあるが、オペレーション手順が簡素

### 3.2 失われた単純作業スキル

`claude-code-workflows/skills` にある以下の基礎スキル群が `.claude/skills` には未配置。
- `ai-development-guide`
- `coding-principles`
- `documentation-criteria`
- `implementation-approach`
- `integration-e2e-testing`
- `task-analyzer`
- `testing-principles`
- `frontend-ai-guide`
- `typescript-rules`
- `typescript-testing`
- `subagents-orchestration-guide`（元版）
- `codex-skill-direct-mode`（元版）

備考:
- 現在は workflow特化スキルで代替しているが、単純作業（小修正・単発レビュー・軽い調査）で再利
用可能な基礎スキル層が薄い。

### 3.3 差分の要点

- 元: 「commands + subagents + base skills」の三層構造
- 現: 「workflow skill + codex wrapper」の二層寄り
- 結果: 実装速度は上がるが、統制・再現性・手順の監査性が下がりやすい

---

## 4. Codex移譲の実装状況

### 4.1 Codex呼び出し方法

実装経路
- `codex-workflow-entry` で意図分類
- `codex` skill へ委譲
- デフォルト: `tmux send-keys` で別pane実行
- 明示要求時のみ直接実行

主要仕様（`codex/SKILL.md`）
- `codex exec --skip-git-repo-check`
- `--model` と `model_reasoning_effort`
- `--sandbox`（`read-only`/`workspace-write`/`danger-full-access`）
- `--full-auto`（書き込み系で利用）
- stderr抑制 `2>/dev/null`（既定）

### 4.2 プロンプト構築

`codex-workflow-entry` の意図別テンプレートで構築。
- implement/build/task: 実装 + 品質ゲート + テスト
- review/diagnose: 分析型プロンプト
- document系: 文書作成/更新プロンプト

### 4.3 サンドボックス使い分け（現状）

現状定義
- `workspace-write`: implement/build/task, add-integration-tests
- `read-only`: design/plan/update-doc/review/diagnose/reverse-engineer

課題
- `design/plan/update-doc/reverse-engineer` は文書を「作成/更新」するため、`read-only` では実
行要件と矛盾。
- `review/diagnose` は初期 `read-only` が妥当だが、修正フェーズへ移る場合は `workspace-write`
へ昇格が必要。

---

## 5. 統合計画（提案）

## 5.1 方針（役割分担の固定）

Claude Code層（保持）
- ワークフロー進行管理
- タスク分解と進捗管理
- 承認フロー（Stop点）
- 品質ゲート判定と完了判定
- 単純作業スキル実行

Codex層（移譲）
- コード読解/影響調査
- 実装・修正
- バグ原因調査と再現検証
- テスト追加・修正

## 5.2 実装アーキテクチャ

1. Control Plane（Claude）
- `workflow-entry` を単一化（`backend-workflow-entry` と `codex-workflow-entry` の二重入口を解
消）
- Stop点、承認、ゲート、タスク状態遷移をここで固定

2. Execution Plane（Codex）
- `codex` skill を実行アダプタ化
- 受け渡し契約を統一
  - 入力: objective, scope, constraints, acceptance, commands
  - 出力: status, changed_files, tests, blockers, next_actions

3. Skill Plane（単純作業）
- 基礎スキル群（coding/testing/doc/implementation/task-analyzer 等）をClaude層に復元
- 小規模作業はCodexを呼ばずにClaude単体で完結可能にする

## 5.3 ワークフロー別 最適役割分担

### implement
- Claude: 要件確定 → スケール判定 → 文書承認 → タスク分解 → 各タスクのゲート管理
- Codex: タスク単位で実装・テスト追加・修正
- Gate: タスクごとに build/test/lint + 承認済みで次へ

### design
- Claude: 設計論点整理、ADR要否判断、レビュー承認
- Codex: 既存コード読解、設計ドラフト素材抽出
- Gate: Design Doc 承認で終了

### review
- Claude: レビュー基準設定、修正要否判断、最終合否
- Codex: 差分レビュー、修正案適用（許可時）
- Gate: 再レビューで基準達成

### diagnose
- Claude: 事象定義、調査ループ管理、意思決定
- Codex: 証拠収集、仮説検証、根本原因候補提示、修正実装
- Gate: 再現消失 + 回帰確認 + 残余リスク明記

### reverse-engineer
- Claude: 対象スコープと承認運用、文書品質判定
- Codex: コード構造抽出、PRD/Design下書き生成
- Gate: doc review 合格 + 不整合解消

### add-integration-tests
- Claude: skeleton方針承認、レビュー判定、品質ゲート判定
- Codex: テスト実装・修正
- Gate: 追加テスト全緑 + 非回帰 + カバレッジ非劣化

## 5.4 不足機能の補完優先順位

P0（最優先）
1. 入口一本化（backend系とcodex系の競合解消）
2. Stop点・承認フローの明文化（phase単位）
3. Codex実行契約（入出力JSON相当）定義
4. sandboxマトリクス修正
   - design/plan/update-doc/reverse-engineer: `workspace-write`（生成時）
   - review/diagnose: `read-only` 開始、修正フェーズで `workspace-write` へ昇格

P1
1. 基礎スキル群の `.claude/skills` への復元
2. 品質ゲート結果の標準フォーマット化
3. 要件変更時の再計画ルール強化

P2
1. frontendワークフローの同等統合
2. メトリクス（Stop通過率、再オープン率、ゲート失敗率）可視化

## 5.5 受け入れ基準

- すべての対象ワークフローで、開始時に「Claude担当」「Codex担当」が明示される
- 文書系/実装系のStop点が固定され、承認なし遷移が起きない
- Codex実行が必ず `status + evidence + blockers` を返す
- 完了宣言前に品質ゲート結果が必ず残る
- 要件変更時に再分析フェーズへ戻る

---

## 6. 主要参照ファイル

- `claude-code-workflows/README.md`
- `claude-code-workflows/commands/implement.md`
- `claude-code-workflows/commands/design.md`
- `claude-code-workflows/commands/plan.md`
- `claude-code-workflows/commands/build.md`
- `claude-code-workflows/commands/review.md`
- `claude-code-workflows/commands/diagnose.md`
- `claude-code-workflows/commands/reverse-engineer.md`
- `claude-code-workflows/commands/add-integration-tests.md`
- `claude-code-workflows/commands/update-doc.md`
- `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md`
- `claude-code-workflows/backend/.claude-plugin/plugin.json`
- `claude-code-workflows/frontend/.claude-plugin/plugin.json`
- `.claude/skills/codex-workflow-entry/SKILL.md`
- `.claude/skills/codex-lifecycle-orchestration/SKILL.md`
- `.claude/skills/codex-task-execution-loop/SKILL.md`
- `.claude/skills/codex-diagnose-and-review/SKILL.md`
- `.claude/skills/codex-document-flow/SKILL.md`
- `.claude/skills/codex/SKILL.md`
- `.claude/skills/backend-workflow-entry/SKILL.md`
- `.claude/skills/backend-lifecycle-execution/SKILL.md`
- `.claude/skills/backend-task-quality-loop/SKILL.md`
- `.claude/skills/backend-document-workflow/SKILL.md`
- `.claude/skills/backend-diagnose-workflow/SKILL.md`
- `.claude/skills/backend-integration-tests-workflow/SKILL.md`

