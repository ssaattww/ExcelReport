# 基本設計

最終更新: 2025-11-16  
Optimizer Excel Report Library — Basic Design

---

# 0. 変更概要 (Changelog)


---

# 1. 詳細設計の粒度ガイドライン (共通)

## 1.1 記述レベル (Do’s)
- 公開APIの一覧と契約条件（前提／事後／例外動作）。  
- データモデル（主要型のフィールド表、不変条件）。  
- モジュール内部の高レベル処理フロー（ステップ列挙）。  
- エラー分類（IssueKind）と対応方針。  
- 性能方針（オーダー・ボトルネック候補）。  
- テスト観点（正常/境界/異常/負荷/回復）。

## 1.2 除外レベル (Don’ts)
- 私的メソッドの網羅、細かい最適化、  
- 必要以上の擬似コード（例外：WorksheetState の座標割付など事故リスクが高い箇所のみ骨子可）、  
- XSD の全文定義と細粒度命名規則（詳細設計側で扱う）。

## 1.3 成果物フォーマット
- モジュール単位で 1 章構成（概要 → API → データモデル → フロー → エラー → 性能 → テスト）。  
- 図は任意（ASCII で可）。

---

# 2. モジュール別「詳細設計で書くべき範囲」

## 2.1 入力オブジェクトパーサ (ReportGenerator)
**MUST**
- 公開 API（例 `GenerateAsync`）。  
- 入力オブジェクト構造の前提（必須項目・既定値）。  
- 読込 → 構文解釈 → 検証 → Issues 生成の全体フロー。  
- エラーモデル（I/O／構文／未知タグ／不整合）。

---

## 2.2 DSL 定義 (DslDefinition)

**MUST**
- DSL 要素の種類（詳細設計が提供する最新仕様に従う）。  
  現時点での要素集合（参考・詳細設計を正とする）:  
  `workbook / styles / style / sheet / component / grid / cell / use / repeat / sheetOptions`
- 主要属性の意味と排他関係（value/Excel数式/C#式、formulaRef、when）。  
- C# 式 `@( … )` の評価主体が ExpressionEngine であること。  
- XSD を提供すること（全文は詳細設計で管理）。  
- 命名規則/正規表現など仕様上必要な規範の提示。  

**NICE**
- 10〜20行規模の簡易 DSL サンプル。  

---

## 2.3 DSL パーサ XML→AST (DslParser)
**MUST**
- API（Parse/Validate）。  
- AST ノードの主要構造と関係図。  
- 検証順序（構文 → 意味 → 参照解決 → 静的レイアウト検証）。  
- C# 式の事前コンパイルとキャッシュ方針。

**OUT**
- ノード全プロパティの網羅。

---

## 2.4 C# 式評価 (ExpressionEngine)
**MUST**
- 許可名前空間（System/LINQ など）と  
  グローバル `root` / `data` / `vars` の参照設計。  
- 例外発生時 `#ERR(...)` 戻り値と Issues 記録。  
- 式→デリゲートのキャッシュ。

---

## 2.5 LayoutEngine（レイアウト計画生成）
**MUST**
- **入力: AST**  
- **出力: LayoutPlan（論理レイアウト）**  
- 責務:
  - 行列座標の割付  
  - データバインディング（AST と C# オブジェクトの紐づけ）
  - repeat 展開  
  - セル結合（merge）判定  
  - スタイル適用の計画（論理レベル）  
  - 名前付き範囲（Area）や formulaRef 計画の作成  
- LayoutEngine は Excel オブジェクトを触らない（ClosedXML 不使用）。  
- キャンセルポイントは基本不要（CPU 計算中心）。

---

## 2.6 WorksheetState（シート状態管理）
**MUST**
- **入力: LayoutPlan**  
- 責務:
  - セル占有管理（重複の禁止）  
  - 結合セルの矩形性保証  
  - スタイル結果の保持（あくまで論理 → 物理前の状態）  
  - Issues.Error の生成（衝突や不整合）  
- WorksheetState 自体は Excel 操作を行わない。

**NICE**
- 座標割付の疑似コード骨子（5〜10行）。

---

## 2.7 Renderer（Excel 出力）
**MUST**
- **入力: WorksheetState**  
- **出力: xlsx（ClosedXML による物理生成）**  
- 責務:
  - セル値、Excel 数式、スタイル、塗り、罫線、結合の物理適用  
  - AutoFilter / Freeze / Group の実装  
  - Issues（Fatal以外）の注記シート生成  
  - ファイル保存・ストリーム出力  
  - 進捗レポートとキャンセル応答（I/O を含む唯一の層）

---

## 2.8 Styles（スタイル管理）
**MUST**
- スタイルは以下の 2 系統から読み込める:
  1. **workbook 内 `<styles>`**  
  2. **外部 XML ファイル** からの読込  
     - ※具体的な DSL 構文（例: `href` など）は **詳細設計で定義する**。  
       BasicDesign では「外部化あり」の要求だけを記述する。
- スタイル適用順（sheet → component → grid → cell）の一方向性。  
- scope（cell/grid/both）と警告の扱い（詳細設計に従う）。

**NICE**
- 代表スタイル例（Header / Body）。

---

## 2.9 進捗・ログ・監査 (Logger)
**MUST**
- レポート進捗粒度（Book→Sheet→Region→セルバッチ）。  
- 監査情報の格納先（隠しシート）。  

---

# 3. 詳細設計の成果物（期待）

| 章 | タイトル | 期待成果（MUST） |
|----|----------|------------------|
| 1 | ReportGenerator | API 契約／ドメイン型表／フロー／エラーモデル／テスト観点 |
| 2 | DslDefinition | 要素一覧／属性意味／排他関係／サンプル／テスト観点 |
| 3 | DslParser | API／AST／検証順序／エラーモデル／事前コンパイル方針 |
| 4 | ExpressionEngine | 許可NS／Globals／エラーポリシー／キャッシュ／テスト観点 |
| 5 | LayoutEngine | 論理レイアウト手順／継続条件／テスト観点 |
| 6 | WorksheetState | 占有管理／不変条件／疑似コード骨子／エラー／テスト観点 |
| 7 | Renderer | 物理反映手順／I/O／進捗／エラー／テスト観点 |
| 8 | Styles | 外部化要件／優先順位／テスト観点 |
| 9 | Logger | 進捗粒度／監査メタ格納方針／テスト観点 |

---

# 4. 受入基準 (Definition of Done)
- 各章の MUST が全て埋まっている。  
- 公開 API の契約が明確で矛盾がない。  
- Fatal 条件・警告条件が曖昧なく記述されている。  

---

# 5. モジュール一覧

| モジュール | 役割 | 境界 |
|------------|------|--------|
| ReportGenerator | 外部 API、進捗、全体調整 | 入力→AST→LayoutPlan→WorksheetState→xlsx |
| DslDefinition | DSL の意味論・構文・XSD | 詳細設計で完全定義 |
| DslParser | XML→AST＋静的検証 | String/File→AST/Issues |
| ExpressionEngine | C#式評価 | 文字列式→値 |
| LayoutEngine | 論理レイアウト計画 | AST→LayoutPlan |
| WorksheetState | 占有・結合・状態管理 | LayoutPlan→状態 |
| Renderer | Excel 物理出力 | WorksheetState→xlsx |
| Styles | スタイル定義・外部化読込 | style名→StyleSpec |

---

(以上)
