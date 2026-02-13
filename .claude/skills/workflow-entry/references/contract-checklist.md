# 契約準拠チェックリスト

`codex-execution-contract.md` 準拠を機械的に検証するためのチェックリスト。

## 1. 入力検証項目

### 1.1 必須フィールド充足確認

| チェックID | 項目 | 判定条件 | 結果 |
|---|---|---|---|
| IN-REQ-01 | `objective` | フィールドが存在し、空文字でない | `pass` / `fail` |
| IN-REQ-02 | `scope` | フィールドが存在する | `pass` / `fail` |
| IN-REQ-03 | `constraints` | フィールドが存在する | `pass` / `fail` |
| IN-REQ-04 | `acceptance_criteria` | フィールドが存在する | `pass` / `fail` |
| IN-REQ-05 | `allowed_commands` | フィールドが存在する | `pass` / `fail` |
| IN-REQ-06 | `sandbox_mode` | フィールドが存在する | `pass` / `fail` |

### 1.2 各フィールド妥当性確認基準

| チェックID | 項目 | 妥当性基準 | 結果 |
|---|---|---|---|
| IN-VAL-01 | `objective` | 文字列型かつ 1 文字以上 | `pass` / `fail` |
| IN-VAL-02 | `scope` | オブジェクト型で `in_scope` と `out_of_scope` を配列として保持 | `pass` / `fail` |
| IN-VAL-03 | `constraints` | 配列型（0件以上可、各要素は文字列） | `pass` / `fail` |
| IN-VAL-04 | `acceptance_criteria` | 配列型（1件以上推奨、各要素は文字列） | `pass` / `fail` |
| IN-VAL-05 | `allowed_commands` | 配列型（各要素は文字列、空配列不可） | `pass` / `fail` |
| IN-VAL-06 | `sandbox_mode` | 文字列型で有効値（例: `read-only`, `workspace-write`, `danger-full-access`） | `pass` / `fail` |
| IN-VAL-07 | 任意: `context_files` | 存在する場合は配列型（各要素は文字列パス） | `pass` / `fail` / `n/a` |
| IN-VAL-08 | 任意: `known_risks` | 存在する場合は配列型（各要素は文字列） | `pass` / `fail` / `n/a` |
| IN-VAL-09 | 任意: `stop_conditions` | 存在する場合は配列型（各要素は文字列） | `pass` / `fail` / `n/a` |

## 2. 出力検証項目

### 2.1 必須フィールド充足確認

| チェックID | 項目 | 判定条件 | 結果 |
|---|---|---|---|
| OUT-REQ-01 | `status` | フィールドが存在し、空文字でない | `pass` / `fail` |
| OUT-REQ-02 | `summary` | フィールドが存在し、空文字でない | `pass` / `fail` |
| OUT-REQ-03 | `changed_files` | フィールドが存在する | `pass` / `fail` |
| OUT-REQ-04 | `tests` | フィールドが存在する | `pass` / `fail` |
| OUT-REQ-05 | `quality_gate` | フィールドが存在する | `pass` / `fail` |
| OUT-REQ-06 | `blockers` | フィールドが存在する | `pass` / `fail` |
| OUT-REQ-07 | `next_actions` | フィールドが存在する | `pass` / `fail` |

### 2.2 `status` 妥当性確認

| チェックID | 項目 | 妥当性基準 | 結果 |
|---|---|---|---|
| OUT-VAL-01 | `status` | 次のいずれか: `completed` / `needs_input` / `blocked` / `failed` | `pass` / `fail` |

### 2.3 形式確認（`changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`）

| チェックID | 項目 | 形式基準 | 結果 |
|---|---|---|---|
| OUT-VAL-02 | `changed_files` | 配列型。各要素はオブジェクトで `path`(string), `change_type`(string) を保持 | `pass` / `fail` |
| OUT-VAL-03 | `tests` | 配列型。各要素はオブジェクトで `name`(string), `result`(string) を保持 | `pass` / `fail` |
| OUT-VAL-04 | `quality_gate` | オブジェクト型で `result`(string), `evidence`(array[string]) を保持 | `pass` / `fail` |
| OUT-VAL-05 | `blockers` | 配列型。要素は文字列、または `{code, detail}` 形式オブジェクト | `pass` / `fail` |
| OUT-VAL-06 | `next_actions` | 配列型（各要素は文字列） | `pass` / `fail` |

## 3. 実行種別ごとの特記事項

### 3.1 implement

| チェックID | 必須項目 | 判定条件 | 結果 |
|---|---|---|---|
| TYPE-IMP-01 | テスト実行結果 | `tests` が空でない、かつ少なくとも1件の実行結果を含む | `pass` / `fail` |
| TYPE-IMP-02 | 品質ゲート | `quality_gate.result` が存在し、`evidence` が1件以上 | `pass` / `fail` |

### 3.2 review

| チェックID | 必須項目 | 判定条件 | 結果 |
|---|---|---|---|
| TYPE-REV-01 | 差分分析結果 | `summary` または `quality_gate.evidence` に差分分析結果が明示される | `pass` / `fail` |
| TYPE-REV-02 | 修正提案 | `next_actions` に具体的な修正提案が1件以上ある | `pass` / `fail` |

### 3.3 diagnose

| チェックID | 必須項目 | 判定条件 | 結果 |
|---|---|---|---|
| TYPE-DIA-01 | 原因特定 | `summary` または `quality_gate.evidence` に原因の記述がある | `pass` / `fail` |
| TYPE-DIA-02 | 再現手順 | `next_actions` もしくは `tests` に再現手順が明示される | `pass` / `fail` |

## 4. 検証手順

### 4.1 チェックリストの使用方法

1. 対象実行の入出力ペイロードを取得する。
2. `1. 入力検証項目` を上から順に評価し、`fail` が1件でもあれば停止する。
3. `2. 出力検証項目` を評価し、`status` と形式違反を確認する。
4. 実行種別（`implement` / `review` / `diagnose`）に応じて `3. 実行種別ごとの特記事項` を追加評価する。
5. 全チェック `pass` の場合のみ準拠 (`compliant=true`) と判定する。

### 4.2 不適合時の対応フロー

1. `fail` 項目を `missing_fields` または `invalid_fields` として列挙する。
2. 入力不適合の場合:
   - Codex実行を開始しない。
   - `[Stop: contract-missing-field]` を発行し、入力補完を要求する。
3. 出力不適合の場合:
   - 実行結果を受理しない。
   - `quality_gate.result=fail` として再出力を要求する。
4. `status=needs_input` の場合:
   - 直ちに次フェーズ遷移を停止する。
   - `[Stop: needs-input]` と対応する承認要求を発行する。
5. 再試行後も不適合が解消しない場合:
   - `status=blocked` として停止し、`blockers` に原因を記録する。

