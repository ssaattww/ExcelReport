# Phase 9 (Task 16-19) Code Review

## Review Scope
- Date: 2026-03-06
- Target diff: `git diff HEAD`
- Exclusion: `obj/`, `bin/`
- Reviewed changed files (excluding `obj/bin`):
  - `ExcelReport/ExcelReportLib.Tests/DslParserTests.cs`
  - `ExcelReport/ExcelReportLib.Tests/LayoutEngineTests.cs`
  - `ExcelReport/ExcelReportLib.Tests/RendererTests.cs`
  - `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs`
  - `ExcelReport/ExcelReportLib.Tests/WorksheetStateTests.cs`
  - `ExcelReport/ExcelReportLib/DSL/AST/StyleImportAst.cs`
  - `ExcelReport/ExcelReportLib/DSL/DslParser.cs`
  - `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs`
  - `ExcelReport/ExcelReportLib/ReportGenerator.cs`
  - `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs`
  - `tasks/feedback-points.md`
  - `tasks/phases-status.md`
  - `tasks/tasks-status.md`

## Severity Summary
- Critical: 0
- Major: 2
- Minor: 2
- Info: 0

## Findings

### 1) バグ・ロジックエラー

#### [Major] NamedArea の無条件上書きで既存定義を破壊する
- File: `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:184`
- 問題: `AddFormulaReferenceNamedAreas` が `namedAreas[name]` と `namedAreas[$"{name}End"]` を無条件に上書きしており、DSL 側ですでに定義済みの同名 NamedArea を静かに破壊する。
- リスク: 数式参照や `sheetOptions` が意図しない範囲に置き換わり、誤計算・誤配置の原因になる。
- 修正案:
  - 追加前に `ContainsKey` / `TryAdd` で衝突検知。
  - 衝突時は `Issue`（例: `IssueKind.DuplicateNamedArea`）として失敗扱いにする。
  - 代替として内部参照用キー名を衝突しない命名（例: `name__ref`, `name__refEnd`）へ変更する。

### 2) エッジケース未対応

#### [Minor] 終端 NamedArea が複数セル範囲のとき、終端が左上セルに縮退する
- File: `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:174`
- 問題: `#{start:end}` 展開時に `endArea.TopRow/LeftColumn` を使っており、終端が複数セル範囲でも左上セルに縮退する。
- リスク: 本来 `B5:D6` になるべき範囲が `B5:B5` などに狭まり、式レンジが不正になる。
- 修正案:
  - `endReference` を `ToCellReference(endArea.BottomRow, endArea.RightColumn)` に変更。
  - 開始側は `TopRow/LeftColumn`（左上）を維持し、開始=左上/終了=右下で統一する。

### 3) 既存機能への回帰リスク
- 明確な新規回帰は未検出。
- ただし上記 Major 指摘（NamedArea 上書き）は、既存 DSL で同名定義がある場合に回帰を引き起こすため優先修正推奨。

### 4) コード品質（命名, 重複, 保守性）

#### [Major] 数式範囲展開ロジックの一貫性欠如（終端座標の扱い）
- File: `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:154`
- 問題: `ReplaceFormulaPlaceholders` の終端座標計算が右下を使っておらず、範囲展開仕様と実装の整合が崩れている。
- 修正案:
  - 終端は常に `BottomRow/RightColumn` を使用するルールに統一。
  - 実装意図を短いコメントまたはメソッド抽出で明示（例: `GetRangeStartRef` / `GetRangeEndRef`）。

#### [Minor] テストフィクスチャ生成コードの重複
- File: `ExcelReport/ExcelReportLib.Tests/LayoutEngineTests.cs:483`
- 関連: `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs:413`
- 問題: `CreateFullTemplateData` 相当のセットアップが重複しており、今後の変更でテスト間の乖離が起きやすい。
- 修正案:
  - `DslTestFixtures` など共通ヘルパーへ抽出し、両テストから再利用する。

### 5) テストカバレッジの不足

#### [Minor] 複数セル NamedArea の終端参照を検証するテストが不足
- File: `ExcelReport/ExcelReportLib.Tests/WorksheetStateTests.cs:187`
- 問題: `#{Start:End}`（または `...End`）が複数セル NamedArea の右下を正しく使うケースを直接検証するテストがない。
- 修正案:
  - 例: `DetailHeader = B5:D6` を定義し、展開後レンジが `B5:D6` になることを検証するテストを追加。
  - 併せて単一セルケースとの比較テストを置き、退行を検知しやすくする。

### 6) XML ドキュメントコメントの欠如
- 問題なし（今回差分において、公開 API で新規に XML コメント必須となる未記載箇所は未検出）。

### 7) パフォーマンス懸念
- 明確な性能劣化要因は未検出。

## Recommended Fix Priority
1. Major 2件（NamedArea 上書き、終端座標ロジック）を先に修正。
2. その後、テスト不足を補強して回帰防止。
3. 最後にテスト重複を共通化して保守コストを削減。
