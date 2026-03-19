# GitHub Release と NuGet Publish 同期調査レポート

- Date: 2026-03-19
- Scope: `.github/workflows/publish-nuget.yml`

## 調査結果

- 既存ワークフローは `release: published` 起点で NuGet publish を実行。
- ただし package version はタグ文字列をそのまま使用しており、GitHub Release の `pre-release` フラグとは未連動。
- そのため、`pre-release=true` でもタグにサフィックスが無い場合に stable 版として publish されるリスクがあった。

## 対応内容

- `github.event.release.prerelease` を参照して package version を分岐。
- `pre-release=true` の場合:
  - タグに `-` を含む場合はそのまま利用。
  - 含まない場合は `-pre.<GITHUB_RUN_NUMBER>` を付与して pre-release 化。
- `pre-release=false` の場合:
  - タグに `-` がある場合は不整合として fail。
  - タグを stable version として利用。

## 期待される動作

- GitHub pre-release -> NuGet pre-release
- GitHub release -> NuGet release
