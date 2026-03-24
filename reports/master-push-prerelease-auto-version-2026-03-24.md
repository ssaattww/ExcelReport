# master push時のNuGet/GitHub pre-release自動化対応 (2026-03-24)

## 要件
- `master` へ push されたとき、NuGet package を自動で prerelease 発行する。
- GitHub の「Releases」画面にも pre-release を自動表示する。
- バージョンは 3 桁目（patch）を自動増分する。
- prerelease suffix は `-pre` 固定とし、`-pre.<run_number>` は付与しない。
- 通常 release（正式版）は手動運用を継続する。

## 変更内容
- `.github/workflows/publish-nuget.yml`
  - トリガーに `push: [master]` を追加済み。
  - `actions/checkout` を `fetch-depth: 0` に変更済み（タグ参照のため）。
  - `permissions.contents` を `write` に変更（GitHub Release 作成のため）。
  - バージョン解決ロジック:
    - release イベント: 手動 release の tag を基準に version を決定。
    - master push イベント: 最新安定タグ `X.Y.Z` を基準に `patch + commits_since_tag` を算出し、`X.Y.<next>-pre` を採用。
    - タグ未存在時は `VersionPrefix` を基準に同様に算出。
  - master push 時に `gh release create --prerelease` を実行し、GitHub pre-release を自動作成。
  - 同一 tag が既に存在する場合は作成をスキップ。

## 期待動作
- 例: 最新安定タグが `1.1.0` のとき
  - タグ以降1コミット目の master push -> `1.1.1-pre`
  - タグ以降2コミット目の master push -> `1.1.2-pre`
- 各 master push ごとに NuGet prerelease が publish され、同名 tag の GitHub pre-release が作成される。
- 手動 release (`release.published`) は従来どおり正式版/指定済みprereleaseを発行。
