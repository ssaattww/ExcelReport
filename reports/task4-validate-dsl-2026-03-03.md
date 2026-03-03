# Task 4: ValidateDsl実装とXSD検証有効化

## 実装した検証項目

- `EnableSchemaValidation=true` 時に XSD 検証を実行し、違反を `SchemaViolation` / `Fatal` Issue として返す
- XSD ローダーを追加
- 優先順: 組み込みリソース (`ExcelReportLib.DSL.DslDefinition_v1.xsd`)
- フォールバック: 実行ディレクトリ起点の相対探索 (`Design/DslDefinition/DslDefinition_v1.xsd`)
- `ParseFromFile` で `RootFilePath` 未設定時に自動補完
- `ValidateDsl` で sheet 名重複を検出 (`DuplicateSheetName`)
- `ValidateDsl` で style 定義重複を検出 (`DuplicateStyleName`)
- `ValidateDsl` で component 定義重複を検出 (`DuplicateComponentName`)
- `ValidateDsl` で未解決 `styleRef` を検出 (`UndefinedStyle`)
- 対象: `sheet/styleRef`, `component/styleRef`, layout 配下の `styleRef`, `cell@styleRef`
- `ValidateDsl` で style scope 不整合を警告 (`StyleScopeViolation`)
- `ValidateDsl` で未解決 `componentRef` (`use@component`) を検出 (`UndefinedComponent`)
- `ValidateDsl` で `repeat@from` 欠落/空文字を検出 (`UndefinedRequiredAttribute`)
- `ValidateDsl` で `sheetOptions` の `at` 参照先未解決を検出 (`SheetOptionsTargetNotFound`)
- 対象: `freeze`, `groupRows`, `groupCols`, `autoFilter`
- 解決対象: 同一 sheet 内の `use@instance` と `repeat@name`
- `ValidateDsl` で静的に算出可能な配置について座標範囲を検証 (`CoordinateOutOfRange`)
- `SheetAst` で `sheet@name` 欠落/空文字を検出
- `GroupRowsAst` で `groupRows@at` 欠落を検出
- `StyleRefAst` で `styleRef@name` 欠落を `UndefinedRequiredAttribute` として扱うよう修正

## 設計書との整合性確認

- XSD 検証を AST 構築前に実行し、Fatal 時は `Root=null` を返す挙動に合わせた
- ValidateDsl の L1 相当として、重複名・未解決参照・style scope を実装した
- ValidateDsl の L2 相当として、`repeat@from` と `sheetOptions@at` の検証を実装した
- ValidateDsl の L3 相当として、静的に確定できる範囲の座標チェックを実装した
- 既存の参照解決フェーズは Issue を二重発行しないよう、解決のみを担当させた
- duplicate の To-Be 仕様（last wins + Warning）には変更していない
- 現状は既存実装に合わせて `first wins + Error` を維持している

## テストケース一覧

- `ValidateDsl_DuplicateSheetName_ReturnsError`
- `ValidateDsl_UnresolvedStyleRef_ReturnsError`
- `ValidateDsl_UnresolvedComponentRef_ReturnsError`
- `ValidateDsl_ValidDocument_NoErrors`
- `XsdValidation_InvalidXml_ReturnsIssues`

## 残存する未実装・部分実装項目

- `repeat@from` が `IEnumerable` を返すかの意味検証は未実装
- `FormulaRefSeriesNot1DContinuous` は未実装
- C# 式の構文検証 (`ExpressionSyntaxError`) は未実装
- 静的レイアウト検証は「位置が静的に確定できるノード」のみ対象
- `repeat` 展開回数を踏まえた厳密な範囲検証は未実装
- `sheetOptions@at` は `use@instance` / `repeat@name` のみを参照対象として扱う
- `componentImport` / `styleImport` で読み込む外部 DSL ファイル自体への XSD 検証は未実装
