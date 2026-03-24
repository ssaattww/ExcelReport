# master push時のNuGet prerelease自動採番対応 (2026-03-24)

## 要件
- `master` へ push されたとき、NuGet package を自動で prerelease 発行する。
- バージョンは 3 桁目（patch）を自動増分する。
- 通常 release（正式版）は手動運用を継続する。

## 変更内容
- `.github/workflows/publish-nuget.yml`
  - トリガーに `push: [master]` を追加。
  - `actions/checkout` を `fetch-depth: 0` に変更（タグ参照のため）。
  - バージョン解決ロジックを拡張:
    - release イベント: 既存どおりタグ基準で version を決定。
    - master push イベント: 最新安定タグ `vX.Y.Z` を基準に `patch + commits_since_tag` を算出し、`X.Y.<next>-pre.<run_number>` を採用。
    - タグ未存在時は `VersionPrefix` を基準に同様に算出。

## 期待動作
- 例: 最新安定タグが `v1.1.0` のとき
  - タグ以降1コミット目の master push -> `1.1.1-pre.<run>`
  - タグ以降2コミット目の master push -> `1.1.2-pre.<run>`
- 手動 release (`release.published`) は従来どおり正式版/指定済みprereleaseを発行。

## 補足
- 同一コミットで再実行されても `-pre.<run_number>` で一意性を確保。

