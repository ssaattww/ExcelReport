# Issue #58 詳細設計（承認依頼版）

- 作成日: 2026-04-07
- ステータス: **レビュー依頼中（実装前）**
- 対象Issue: https://github.com/ssaattww/ExcelReport/issues/58

## 1. Issue #58 で合意したいこと（要約）
Issue本文（2026-04-07作成）から、以下を設計対象とする。

1. Excelテンプレートを入力にして、最終的に現行DSL/生成パイプラインへ接続したい
2. デバッグ用途として「Excel -> XML Template -> DSL -> Excel」の中間可視化を持ちたい
3. 本番では中間XMLを省略し「Excel -> DSL（直接変換）」も可能にしたい
4. コンポーネント定義方式は次の2案を比較検討する
   - A案: シート単位定義（特殊シート名）
   - B案: 名前付き範囲定義（シート内で定義）

## 2. 方針
### 2.1 段階導入（破壊的変更なし）
- **Phase A (Debug Path)**: Excel -> XmlTemplate を実装し、変換内容を可視化
- **Phase B (Production Path)**: Excel -> DSL 直接変換を追加
- 既存の `DSL -> ReportGenerator` パイプラインは再利用し、既存API互換を維持する

### 2.2 コンポーネント定義方式の採用
- **採用方針: A案（シート単位定義）を初期採用**
  - 理由: issue本文の「実装が簡単そう」と一致し、短期で安定運用に到達しやすい
- B案（名前付き範囲）は将来拡張として残す
  - 理由: 直感的だが、入れ子運用で複雑化リスクが高い

## 3. 対象範囲
### In Scope
- ExcelTemplate 入力モデルの追加
- Excel -> XmlTemplate 変換器（デバッグ用途）
- Excel -> DSL 直接変換器（本番用途）
- シート単位コンポーネント抽出ルール
- 変換ログ（どのセル/範囲をどのDSL要素へ写像したか）
- 主要テスト（unit + e2e）

### Out of Scope（今回）
- 名前付き範囲ベースのコンポーネント定義（B案本実装）
- GUI/CLIツールの本格追加
- 既存DSL構文の大幅拡張

## 4. アーキテクチャ設計

```text
[Excel Workbook]
   | (Phase A)
   v
[ExcelTemplateExtractor] -> [XmlTemplateSerializer]
   | (Phase B bypass possible)
   v
[DslEmitter]
   v
[Existing DslParser/Layout/Renderer]
   v
[Excel Output]
```

### 4.1 新規コンポーネント（案）
- `ExcelTemplate/ExcelTemplateExtractor`
  - Excelブックを読み取り、シート/セル/スタイル/名前情報を中間モデル化
- `ExcelTemplate/XmlTemplateSerializer`
  - 中間モデルをXML templateとして出力（デバッグ向け）
- `ExcelTemplate/DslEmitter`
  - 中間モデル（またはExcel直接）からDSL文字列へ変換

### 4.2 既存コンポーネントとの責務境界
- 既存 `DSL` パーサ以降には変更を極力入れない
- 変換層（ExcelTemplate系）で解決できる仕様はそこで閉じる

## 5. DSLマッピングルール（初版）
- シート名 `__component_<Name>` をコンポーネント定義シートとして扱う
- 通常シートは `<sheet>` に変換
- 値セルは `cell@value` へ変換
- 数式セルは `cell@formula` または `cell@value` の既存規約へ正規化（既存仕様優先）
- 最小限のスタイル属性（font/fill/numberFormat）から段階対応

## 6. 失敗時ポリシー
- 未対応Excel要素を検出した場合:
  - 変換を継続可能なら Warning として `ReportGeneratorResult.Issues` へ集約
  - 継続不能なら Error で停止
- どのセルが未対応だったかを座標付きでログ出力

## 7. テスト戦略（TDD）
1. Unit
   - シート分類（通常/コンポーネント）
   - セル値/数式/最小スタイルの抽出
   - DslEmitterの要素生成
2. Integration
   - Excel -> DSL 変換結果のスナップショット比較
3. E2E
   - Excel -> DSL -> ReportGenerator で最終Excel生成まで確認

## 8. 互換性
- 既存公開APIは非変更
- 既存DSL実行機能に影響なし
- 破壊的変更は現時点では想定しない

## 9. 実装前の承認ポイント
- [ ] A案（シート単位コンポーネント定義）で着手して良いか
- [ ] デバッグ経路（Excel -> XML Template）を同時に入れるか（先行実装の要否）
- [ ] 初版マッピング対象（値/数式/最小スタイル）でよいか

---

**承認依頼**: 上記3点がOKであれば「承認」と返信ください。承認後、TDDで実装フェーズへ移行します。
