# 設計書修正方針提案（フェーズ5）

- 作成日: 2026-02-13
- 対象: `Design/` 一式（特に `DslDefinition` / `DslParser`）
- 入力根拠:
- `reports/design-inventory-2026-02-13.md`
- `reports/implementation-inventory-2026-02-13.md`
- `reports/design-implementation-alignment-2026-02-13.md`
- `reports/issues-and-improvements-2026-02-13.md`

## 1. 前提と意思決定条件

### 1.1 現状の重要事実

1. 実装は初期段階で、実質 `DslParser + AST` が中心。
2. High課題は「仕様混在（本文/XSD/サンプル）」「検証未完成（XSD/DSL検証未整備）」。
3. 後段モジュール（ExpressionEngine, LayoutEngine, WorksheetState, Renderer, Logger, ReportGenerator）は設計先行。

根拠:
- `reports/implementation-inventory-2026-02-13.md`
- `reports/issues-and-improvements-2026-02-13.md`

### 1.2 評価軸

- 誤読リスク低減
- 現実装との整合性
- 将来拡張性（後段モジュール設計資産の保全）
- 変更コスト
- 運用しやすさ（顧客・開発者が読み分け可能か）

## 2. 3戦略の比較

## 2.1 戦略A: 実装準拠（As-Isへ全面寄せ）

### 概要
設計書全体を現行実装に合わせ、未実装モジュール記述を縮退・保留扱いにする。

### メリット
1. 現在のコード読解と設計記述が最短距離で一致する。
2. DslParser周辺の誤読を早く減らせる。
3. 設計レビュー時の「実装との差分指摘」が減る。

### デメリット
1. 後段モジュールの設計資産を弱めるため、将来実装時に再設計コストが上がる。
2. 「将来像」が文書から消え、アーキの一貫性が見えづらくなる。
3. 実装進捗に設計の価値が引きずられ、長期計画を壊しやすい。

### 適用リスク
- 設計先行で積み上げた `LayoutEngine/Renderer/ReportGenerator` の仕様を失うリスク。

## 2.2 戦略B: 将来設計維持（To-Be中心）

### 概要
既存設計を将来正として維持し、実装との差は「未実装」と注記して吸収する。

### メリット
1. 長期アーキテクチャを保持できる。
2. 後段モジュールの実装開始時に設計がそのまま使える。
3. ADR/Design主導の開発に向く。

### デメリット
1. 当面の DslParser 実装利用者に誤読を与えやすい。
2. High課題（仕様混在、検証未完成）を即時解決しづらい。
3. 「実装できているもの」と「理想仕様」の境界が曖昧になる。

### 適用リスク
- 顧客・開発者が現実装を過大評価する運用事故。

## 2.3 戦略C: 二層化（As-Is / To-Be 分離）

### 概要
設計書を「現行実装仕様（As-Is）」と「将来設計（To-Be）」の二層で管理し、同一ドキュメント内または分冊で明示分離する。

### メリット
1. 現在の実装整合と将来設計資産を両立できる。
2. High課題を As-Is 側で先に封じ込めできる。
3. 読者が目的別に参照可能（運用/実装/将来検討）。

### デメリット
1. 文書運用ルール（同期責任、更新手順）が必要。
2. 初期整備コストはA/Bより高い。
3. 二重管理に失敗すると逆に複雑化する。

### 適用リスク
- ラベル運用が曖昧だと二層が形骸化する。

## 2.4 比較表

| 観点 | A 実装準拠 | B 将来維持 | C 二層化 |
|---|---:|---:|---:|
| 誤読リスク低減 | 高 | 低 | 高 |
| 現実装整合 | 高 | 低 | 高 |
| 将来拡張性 | 低 | 高 | 高 |
| 変更コスト（初期） | 中 | 低 | 高 |
| 運用しやすさ（中長期） | 中 | 低 | 高 |

## 3. 推奨戦略

## 3.1 推奨: 戦略C（二層化）

### 推奨理由

1. 現状の主要問題が「仕様混在」と「検証未完成」であり、As-Isの明確化が必須。
2. 同時に、実装未着手モジュールが多く To-Be 設計資産の破棄は損失が大きい。
3. 実装初期フェーズでは、短期正確性（As-Is）と中長期設計（To-Be）の併存が最も合理的。

根拠:
- `reports/issues-and-improvements-2026-02-13.md`
- `reports/implementation-inventory-2026-02-13.md`
- `reports/design-implementation-alignment-2026-02-13.md`

### 非推奨の理由（A/B）

- Aは短期整合に強いが、未実装モジュールの設計価値を毀損。
- Bは設計資産を守るが、直近の誤読事故を減らせない。

## 4. 推奨戦略Cに基づく具体的修正方針

## 4.1 文書構造方針

1. 各設計書の先頭に `Status` セクションを追加。
- `As-Is (Implemented)`
- `As-Is (Partial)`
- `To-Be (Planned)`
2. 仕様記述に `As-Is` / `To-Be` ラベルを明示。
3. 例示DSL（サンプル）は `as-is-fixture` と `to-be-example` を分離。

## 4.2 DslDefinition 修正方針（最優先）

1. 単一の正規仕様を `As-Is DSL Contract` として確定。
- `use` 識別属性
- importタグ
- span属性名
2. `To-Be DSL Contract` を別節で定義し、差分理由を追記。
3. XSD・本文・サンプルの対応表を追加し、差分ゼロを目標化。

## 4.3 DslParser 修正方針（最優先）

1. API章を `As-Is API` と `To-Be API` に分離。
2. `EnableSchemaValidation` の実効状態を明記（現状: 無効）。
3. `ValidateDsl` の未実装範囲をチェックリスト化し、未対応を可視化。

## 4.4 他モジュール修正方針（第2優先）

1. `ExpressionEngine` 以降は `To-Be` 主体で維持。
2. 各文書に「未実装であること」「依存先実装待ち」を冒頭明記。
3. `ReportGenerator` は統合観点で、各依存モジュールの成熟度表を追加。

## 4.5 横断方針

1. 用語統一表（attribute名、Issue種別、モジュール名）を新設。
2. 全文書に「証跡リンク（実装パス）」を追加。
3. 将来仕様のみの項目は `To-Be` タグ必須化。

## 5. 修正順序（実行シーケンス）

1. Step 1: 基準規約の確定
- 対象: `Design/BasicDesign_v1.md`
- 実施: As-Is/To-Beラベル規約、用語統一規約、証跡記載規約を追記

2. Step 2: DslDefinition 正規化
- 対象: `Design/DslDefinition/DslDefinition_DetailDesign_v1.md`, `Design/DslDefinition/DslDefinition_v1.xsd`, `Design/DslDefinition/*Sample*.xml`
- 実施: 本文/XSD/サンプルの不整合を解消し As-Is/To-Be を分離

3. Step 3: DslParser 設計の二層化
- 対象: `Design/DslParser/DslParser_DetailDesign_v1.md`
- 実施: API・検証・Issue運用を As-Is/To-Be で分離

4. Step 4: 後段モジュールへの適用
- 対象: `Design/ExpressionEngine/*` 〜 `Design/ReportGenerator/*`
- 実施: Status宣言、依存成熟度、未実装注記を追加

5. Step 5: 横断レビューと凍結
- 対象: `Design/` 全体
- 実施: 用語、リンク、ラベル、矛盾チェックを実施して版を凍結

## 6. レビュー基準（受入条件）

## 6.1 必須レビュー項目

1. 一貫性レビュー
- 同一概念の表記ゆれがない（本文/XSD/サンプル/AST対応表）

2. 層分離レビュー
- すべての主要節が `As-Is` か `To-Be` のどちらかに分類される
- 無ラベル仕様が残っていない

3. 証跡レビュー
- 実装言及箇所にファイルパス（必要に応じ行番号）を付与

4. 可用性レビュー
- 実装者が `As-Is` のみで実装判断可能
- 設計者が `To-Be` のみで将来計画可能

## 6.2 定量ゲート

- Gate-1: High課題3件に対する修正方針が文書化されている
- Gate-2: `DslDefinition` の本文/XSD/サンプルで主要属性の矛盾が 0 件
- Gate-3: `DslParser` 章に未実装項目リストが存在し、優先度が付与されている
- Gate-4: 全主要設計書に `Status` セクションが存在する

## 6.3 レビューアウトプット

- `Accepted`: 次フェーズ（実際の設計書修正）へ進行
- `Needs Update`: 差戻し（対象ファイルと差戻し理由を明記）

## 7. 顧客向け意思決定ガイド

最終選択時の判断基準:

1. 直近1〜2スプリントで実装整合を最優先するなら A
2. 実装予定がまだ遠く設計資産保全を最優先するなら B
3. 今回のように「実装初期 + 仕様混在 + 将来設計が重い」なら C

本調査結果に照らすと、推奨は `C: 二層化`。
