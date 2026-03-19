# NuGet 公開手順メモ

- Date: 2026-03-19
- Package: `ExcelReportLib`
- NuGet: https://www.nuget.org/packages/ExcelReportLib/
- Workflow: `.github/workflows/publish-nuget.yml`

## 公開トリガー

- GitHub Release の `published` イベントで自動公開
- 必要に応じて `workflow_dispatch` で手動実行可能

## バージョン対応ルール

- `pre-release=false` の Release: NuGet 正式版として公開
- `pre-release=true` の Release: NuGet プレリリース版として公開
- 手動実行: `<VersionPrefix>-ci.<GitHub Run Number>`

## 必須シークレット

- `NUGET_API_KEY`

## 補足

README には公開手順を残さず、運用情報は本レポートで管理する。
