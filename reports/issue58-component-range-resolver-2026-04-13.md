# issue #58 ComponentRangeResolver 実装記録

- 作成日: 2026-04-13
- 対象: issue #58 ExcelTemplate 対応の実装第3段
- ブランチ: `codex/create-design-document-for-approval`

## 1. 実施内容

ExcelTemplate 変換層の次段として、`__component_<Name>` シートの定義範囲を解決する `ExcelTemplateComponentRangeResolver` を追加した。

今回反映した内容:

- `ExcelTemplateComponentRangeResolver` を追加
- component シート判定
  - シート名 `__component_<Name>` を対象化
- 明示範囲解決
  - workbook defined name `__component_range_<Name>` を最優先で採用
  - `'__component_Header'!$A$1:$C$3` のような sheet-qualified range を正規化
  - 別シート参照や不正 range は `InvalidComponentRange` Error
- 自動判定
  - 値 / 数式 / style を持つセル
  - merged range
  を candidate とし、最小外接矩形を component range として解決
- 空範囲検出
  - 明示範囲・自動判定の両方で candidate 0 件は `EmptyComponentRange` Error
- `IssueKind.InvalidComponentRange` / `IssueKind.EmptyComponentRange` を追加

## 2. テスト

TDD:

1. 先に `ExcelTemplateComponentRangeResolverTests` を追加して Red を確認
2. 最小実装で Green 化
3. 明示範囲が空でも通ってしまう穴を追加テストで再現
4. candidate 0 件時の `EmptyComponentRange` 判定を補完して再度 Green 化

実施:

- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter "FullyQualifiedName~ExcelTemplateComponentRangeResolverTests"`
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj`

結果:

- resolver 対象テスト: 5 passed
- 全体テスト: 223 passed, 0 failed

## 3. 次の実装候補

次は設計 13.5 順序どおり、以下のどちらかへ進む。

1. `UseTriggerParser`
   - `{{use:...}}` / `repeat + use` を解析し、DSL へ落とす入力契約を固定する
2. `ExcelTemplateValidator`
   - merged cell 境界 / 初版対象外機能 / invalid component range を Error/Warning 化する
