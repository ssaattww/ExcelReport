# issue #16 follow-up: xl formula helper 追加（2026-04-05）

## 背景

`sheet repeat` の動的シート参照で、式中に `it.SourceSheet.Replace("'", "''")` を毎回書く必要があり可読性が低い。

## 対応内容

1. ExpressionEngine に `xl` ヘルパーを追加
- `xl.Sheet(name)` : `'Sheet Name'`
- `xl.Ref(name, "A1")` : `'Sheet Name'!A1`
- `xl.FormulaRef(name, "A1")` : `='Sheet Name'!A1`

2. 実装
- `ScriptGlobals` に `xl`（dynamic）を追加し、全式から `xl` として参照可能化。
- `ExpressionEngine` に `ExcelFormulaHelpers` を実装。

3. テスト
- `ExpressionEngineTests.Evaluate_XlFormulaHelper_BuildsEscapedFormulaReference`
- `ReportGeneratorTests.Generate_SheetRepeat_CrossSheetFormulaFromExpression_E2E` を `xl.FormulaRef` 記法へ更新
- `ExpressionEngineTests.Evaluate_XlFormulaHelper_WithInterpolatedString_Works` を追加（`$"..."` 補間記法）
- `ReportGeneratorTests.Generate_SheetRepeat_CrossSheetFormulaWithInterpolatedString_E2E` を追加（`$"..."` + `xl.Ref`）
- `ExpressionEngineTests.Evaluate_LinqLambdaParameterNamedXl_DoesNotConflictWithHelperBinding` を追加（`root.Items.Select(xl => ...)` 回帰）

4. ドキュメント
- `Design/DslDefinition/DslDefinition_DetailDesign.md` の式言語章へ `xl` ヘルパーを記載
- `Design/SheetReference/SheetReference_DetailDesign.md` の完全例を `xl.FormulaRef` へ更新

## 実行結果

- `dotnet test ... --filter "Evaluate_XlFormulaHelper_BuildsEscapedFormulaReference|Generate_SheetRepeat_CrossSheetFormulaFromExpression_E2E"`: Passed 2
- `dotnet test ... --filter "Evaluate_XlFormulaHelper_WithInterpolatedString_Works|Generate_SheetRepeat_CrossSheetFormulaWithInterpolatedString_E2E"`: Passed 2
- `dotnet test ... --filter "Evaluate_LinqLambdaParameterNamedXl_DoesNotConflictWithHelperBinding|Evaluate_XlFormulaHelper_WithInterpolatedString_Works"`: Passed 2
- `dotnet test ... --filter "ExpressionEngineTests|ReportGeneratorTests"`: Passed 61

## PR #57 レビュー対応

- 指摘: `dynamic xl = ...` のローカル宣言がユーザー式のラムダ変数 `xl` と衝突し、`CS0136` を引き起こす
- 修正:
  - スクリプト前置きの `dynamic xl = ...` 宣言を削除
  - `ScriptGlobals.xl` を `dynamic` プロパティとして提供
  - ラムダ変数 `xl` 利用の回帰テストを追加
