# issue #58 sub-agent 設計レビュー結果

## 実施条件
- reviewer: `gpt-5.3-codex`
- reasoning: `high`
- 対象: `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md` の 10.8 / 10.9 / 10.10 と関連SVG
- 観点:
  - `repeat + use` の展開規則
  - 親フレーム拡張 / `styleOverflow` / component定義範囲の関係
  - 行方向 / 列方向ケースの詰め
  - 図 / 表 / 本文の整合

## Findings

### 1. High: `repeat + use` 時の `H` 定義が未確定
- 該当:
  - `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md:353`
  - `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md:390`
  - `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md:393`
- 内容:
  - 10.8.9 では親フレームを `A3:E10` と明示しているが、10.9.1 の拡張式は単一の `H x W` を前提にしている。
  - `repeat` の場合に `H` を「1インスタンス高」とみなすのか、「repeat 全体の連結総高」とみなすのかが未定義。
  - このままだと `A3:E8` と `A3:E10` の両方が解釈上成立する。
- 影響:
  - 親フレーム拡張の実装が実装者依存になる。

### 2. High: `GroupBlock` の有効幅定義が 3列 / 4列で不一致
- 該当:
  - `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md:265`
  - `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md:293`
  - `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md:364`
  - `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md:448`
  - `Design/ExcelTemplate/assets/insert-source-cell-values.svg:13`
  - `Design/ExcelTemplate/assets/insert-source-cell-values.svg:55`
  - `Design/ExcelTemplate/assets/expanded-insert-source-from-csharp.svg:29`
  - `Design/ExcelTemplate/assets/expanded-insert-source-from-csharp.svg:43`
- 内容:
  - 本文は `3x4` / `A1:D3` / `A1:D4` を使う一方、状態整理では `W=3` を前提に `A:E` 拡張を説明している。
  - SVG も `A:D` を示す入力図と、実質 `A:C` 外周で描かれた展開後図が混在している。
- 影響:
  - 列方向拡張、親フレーム幅、`styleOverflow` 評価基準が不安定になる。

### 3. Medium: `styleOverflow=edge` が右方向列拡張しか定義されていない
- 該当:
  - `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md:371`
  - `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md:398`
  - `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md:417`
- 内容:
  - 10.9 の宣言は行 / 列の両方向対応だが、具体アルゴリズムは `deltaCols > 0` の右方向だけ。
  - `deltaRows > 0`、上方向、左方向への拡張規則がない。
- 影響:
  - 行方向 overflow や逆方向拡張で実装差異が出る。

### 4. Medium: 連続インスタンス境界の罫線競合解決順が未固定
- 該当:
  - `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md:362`
  - `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md:363`
  - `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md:486`
- 内容:
  - `B7:D7` と `B8:D9` のような連続配置では、前インスタンスの `bottom` と次インスタンスの `top` が同一境界で衝突しうる。
  - 11章では辺単位解決までは書かれているが、同優先度同士の決着規則がない。
- 影響:
  - 連続展開時の境界線が renderer 実装に依存する。

### 5. Low: 10.8.8 SVG の注記が現行章構成とずれている
- 該当:
  - `Design/ExcelTemplate/assets/expanded-insert-source-from-csharp.svg:14`
- 内容:
  - 図内説明が「10.10 サンプルデータから生成」となっているが、現在のサンプル提示位置は 10.8 内。
- 影響:
  - 読み手が参照位置を誤認しやすい。

## 総評
- 現時点で、実装に入る前に固定すべき論点が残っている。
- 優先度は次の順序が妥当:
  1. `repeat` に対する `H` / `W` の定義を固定
  2. `GroupBlock` の有効幅を 3列か4列かで統一
  3. `styleOverflow=edge` の行方向 / 左右上下方向ルールを補完
  4. 連続インスタンス境界の罫線競合順を固定
