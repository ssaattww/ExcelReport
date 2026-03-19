# Dynamic LINQ方針検討レポート (2026-03-19)

## 1. 背景
`reports/design-implementation-gap-analysis-2026-03-05-ja.md` の方針に沿って Roslyn ベースの C# 式評価へ移行した結果、`root/data` が非公開型・匿名型の場合にテンプレート式の LINQ ラムダで失敗する課題が再発した。

代表症状:
- `CS1977`: dynamic 呼び出しに対するラムダ引数
- `CS1061`: `DynamicValue` を強型付けした経路で `Name/Amount` 等が解決不可

## 2. 検討した選択肢

### 案A: DynamicLinqRewriteMap（採用）
- 概要: 失敗した式のうち対象LINQ呼び出しを `__dynWhere` などのヘルパー呼び出しへ Roslyn 構文木で書き換える。
- 長所:
  - 既存アーキテクチャへの変更が小さい
  - 匿名型/非公開型でも template LINQ を段階的に復旧できる
  - 実装・検証コストが低い
- 短所:
  - 対応メソッドがマップ対象に限定される

### 案B: IEnumerable 前提の全面再設計（dynamic最小化）
- 概要: 動的アクセスを減らし、式評価入力を `IEnumerable`/公開型へ寄せる。
- 長所: 長期的には静的型の恩恵が大きい。
- 短所: 既存テンプレート互換、LayoutEngine連携、入力多様性に対する変更範囲が大きい。

### 案C: 公開プロキシ（DTO射影）方式
- 概要: 匿名型/非公開型入力を公開プロキシへ変換してから式評価。
- 長所: 静的LINQを広く維持しやすい。
- 短所: 実装が重く、プロパティ解決・ネスト・列挙体・キャッシュ整合など追加設計が必要。

### 案D: サードパーティ導入
- 候補: Dynamic LINQ系ライブラリ。
- 評価: 文字列クエリ向け最適化が中心で、現行 DSL の「C#式をそのまま評価する」方針とギャップがある。既存式資産の移行コストが高い。

## 3. 採用理由
今回は「動くこと優先」の要件に合わせ、案A（`DynamicLinqRewriteMap`）を採用。
- 既存 DSL 式をほぼ維持したまま修正できる
- E2E で効果を確認しやすい
- 将来、案C（公開プロキシ）へ拡張する際の暫定層としても利用可能

## 4. 実装内容
- `ExpressionEngine` に `DynamicLinqRewriteMap` を導入。
- 一次コンパイル失敗時、dynamic コンテキストかつ式が書き換え対象の場合のみ再コンパイル。
- 書き換え例:
  - `root.Lines.Where(x => x.Amount >= 150m)`
  - `=> __dynWhere((object)(root.Lines), x => x.Amount >= 150m)`
- `__dynWhere/__dynSelect/__dynSum/...` の helper local function をスクリプトへ注入。
- `DynamicValue` 型は強型付け候補から除外し、常に dynamic 束縛で扱うよう修正（`it.Name` 失敗の防止）。

## 5. 設計反映
- 更新先: `Design/ExpressionEngine/ExpressionEngine.md`
- 反映内容:
  - DynamicLinqRewriteMap のフロー
  - 対応メソッド範囲
  - 制限事項（未対応メソッド・IEnumerable前提・Sumのdynamic加算）

## 6. 検証
対象テスト（13件）成功:
- `ExpressionEngineTests`
- `ReportGeneratorTests.Generate_TemplateWithLinqExpressions_ProducesExpectedCells`
- `ReportGeneratorTests.Generate_TemplateWithLinqExpressions_AnonymousRoot_ProducesExpectedCells`

## 7. 残課題
- `OrderBy/ThenBy/GroupBy/...` など map 未対応LINQの扱い
- 公開プロキシ方式の採用是非（コスト見積りと効果比較）

