# PR #38 CI失敗修正レポート

- 日付: 2026-03-24
- 対象PR: https://github.com/ssaattww/ExcelReport/pull/38
- 対象ジョブ: `xunit-tests`（failure）

## 事象

GitHub Actions のアノテーションにて、以下のコンパイルエラーを確認:

- `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs`
- `Argument 2: cannot convert from 'System.StringComparison' to 'int'`

## 原因

`string.LastIndexOf` の呼び出しで `char` オーバーロードに `StringComparison` を渡していた。

## 対応

`FindNamedArea` 内の以下呼び出しを修正:

- 修正前: `currentScope.LastIndexOf('/', StringComparison.Ordinal);`
- 修正後: `currentScope.LastIndexOf('/');`

`char` オーバーロードに適合させ、コンパイルエラーを解消。
