# README同期対応レポート (2026-03-24)

## 要件
- リポジトリ README の build ステータス表示を正しい状態にする。
- NuGet 同梱 README を不親切な専用ファイルではなく、リポジトリ README に追従させる。

## 実施内容
- ルート `README.md`
  - プレースホルダの Build バッジを削除。
  - GitHub Actions の実ワークフローバッジに置換:
    - `pr-xunit-tests.yml`
    - `publish-nuget.yml`
- `ExcelReport/ExcelReportLib/ExcelReportLib.csproj`
  - NuGet package に同梱する README を `ExcelReport/ExcelReportLib/README.md` から `../../README.md` に変更。
  - `Link="README.md"` を設定し、パッケージ内の readme 名を維持。
- `ExcelReport/ExcelReportLib/README.md`
  - 専用 README を削除（README の二重管理を解消）。

## 期待効果
- README の表示と CI 実行状態の乖離を解消。
- NuGet README とリポジトリ README の内容差分・更新漏れを防止。
