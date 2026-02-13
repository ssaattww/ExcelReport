# Codex実行契約仕様

## 1. 目的

本契約は、Claude層とCodex層の責任分界を固定し、実行ごとの入出力を機械的に検証可能にするために定義する。  
主な目的は次のとおり。

- 入口 (`workflow-entry`) からCodexへの入力形式を標準化し、解釈差分をなくす
- Codexの実行結果を標準化し、品質ゲート・停止判定を自動化する
- `status=needs_input` を明示的な停止トリガーとして扱い、承認/追加入力フローに確実に接続する

## 2. 適用範囲

本契約は、`workflow-entry` 経由でCodexを実行する全フローに適用する。

- `implement`
- `task`
- `build`
- `review`
- `diagnose`
- `design`
- `plan`
- `update-doc`
- `reverse-engineer`
- `add-integration-tests`

互換アダプタ (`codex-workflow-entry` など) を経由する場合も同一契約を適用する。

## 3. 責任分界

- Claude層:
  - 契約準拠の入力を生成してCodexに渡す
  - 出力の必須フィールド検証を行う
  - `status` に応じて継続・停止・承認要求を制御する
- Codex層:
  - 契約準拠の出力を返す
  - 実施内容、変更ファイル、検証結果、ブロッカーを明示する
  - 追加情報が必要な場合は `status=needs_input` を返す

## 4. 入力スキーマ（タスク3.2）

### 4.1 必須フィールド

- `objective`
- `scope`
- `constraints`
- `acceptance_criteria`
- `allowed_commands`
- `sandbox_mode`

### 4.2 任意フィールド

- `context_files`
- `known_risks`
- `stop_conditions`

### 4.3 例（YAML）

```yaml
objective: "タスク3.1を実装し、契約仕様ドキュメントを作成する"
scope:
  in_scope:
    - ".claude/skills/workflow-entry/references/codex-execution-contract.md の新規作成"
  out_of_scope:
    - "他スキルの改修"
constraints:
  - "既存仕様（Phase1計画）に整合すること"
  - "必須フィールドを省略しないこと"
acceptance_criteria:
  - "入力/出力スキーマ必須項目が明記されている"
  - "違反時の扱いが定義されている"
allowed_commands:
  - "rg"
  - "sed"
  - "apply_patch"
sandbox_mode: "workspace-write"
context_files:
  - "reports/integration-implementation-plan.md"
known_risks:
  - "契約未準拠出力による停止漏れ"
stop_conditions:
  - "required field欠落"
```

## 5. 出力スキーマ（タスク3.3）

### 5.1 必須フィールド

- `status`
- `summary`
- `changed_files`
- `tests`
- `quality_gate`
- `blockers`
- `next_actions`

### 5.2 `status` の許容値

- `completed`
- `needs_input`
- `blocked`
- `failed`

### 5.3 例（YAML）

```yaml
status: "completed"
summary: "Codex実行契約仕様を新規作成し、入出力と違反時動作を定義した"
changed_files:
  - path: ".claude/skills/workflow-entry/references/codex-execution-contract.md"
    change_type: "added"
tests:
  - name: "manual-contract-review"
    result: "passed"
quality_gate:
  result: "pass"
  evidence:
    - "必須フィールド一覧を明記"
    - "status遷移と停止条件を明記"
blockers: []
next_actions:
  - "codex/SKILL.md に本契約参照を埋め込む（タスク3.4）"
```

## 6. 違反時の扱い

### 6.1 必須フィールド欠落時の動作

- 入力側欠落（Claude -> Codex）:
  - Codex実行を開始しない
  - `status=blocked` 相当として欠落フィールドを列挙して差し戻す
- 出力側欠落（Codex -> Claude）:
  - 実行結果を受理しない
  - `quality_gate.result=fail` とし、再出力を要求する
  - 再出力不能な場合は `status=blocked` として停止する

### 6.2 `status=needs_input` の扱い

- Claude層は直ちに次フェーズ遷移を停止する
- `[Stop: needs-input]` を発行し、承認または追加入力を要求する
- 受領した入力を `constraints` / `scope` / `acceptance_criteria` に反映し、再実行する
- 追加情報が解消されるまで `completed` へ遷移してはならない

## 7. 検証観点

- implement/review/diagnose の3実行種別で必須項目が常に充足されること
- `status=needs_input` がStop/Approvalフローへ確実に接続されること
- 出力必須フィールド欠落率が0%であること
