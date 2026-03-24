# CSX Expression Type Name Investigation (2026-03-24)

## Summary

`csx` 上で `public` 型を定義して `@(root.Pairs)` を評価すると、`ExpressionSyntaxError (CS1040)` が発生し、結果として `repeat from はコレクションである必要があります` になる事象を確認。

## Reproduction Signal

- `ExpressionSyntaxError: (1,41): error CS1040 ...`
- 続けて `InvalidAttributeValue: repeat from はコレクションである必要があります: @(root.Pairs)`

## Root Cause

`ExpressionEngine` は `root`/`data` の実行時型から C# スクリプト用の型名を生成して強型付けバインドを作る。

`csx` で定義された `public` 型は Roslyn 生成名として `Submission#0+TypeName` のような `#` を含む `FullName` になることがあり、これをそのまま `global::...` 型名として埋め込むと C# 構文として不正になってコンパイル失敗する。

## Fix

- `ExpressionEngine.TryFormatScriptTypeName` で生成した型名を `SyntaxFactory.ParseTypeName` で構文検証するよう変更。
- 型名が不正な場合は `TryGetScriptTypeName` が失敗扱いになり、既存の `dynamic` バインド経路へフォールバックする。

## Tests

`ExpressionEngineTests` に回帰テストを追加。

- `Evaluate_TypeNameContainingHash_UsesDynamicFallback`
- `#` を含む公開型名 (`Submission#0Root`) を `Reflection.Emit` で生成し、`@(root.Pairs)` がエラーなしで列挙値を返すことを検証。

## Verification

- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter "FullyQualifiedName~ExpressionEngineTests"`
- Result: Passed 12 / Failed 0

