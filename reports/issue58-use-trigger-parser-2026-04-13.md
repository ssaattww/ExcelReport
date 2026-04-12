# issue #58 UseTriggerParser 実装記録

- 作成日: 2026-04-13
- 対象: issue #58 ExcelTemplate 対応の実装第4段
- ブランチ: `codex/create-design-document-for-approval`

## 1. 実施内容

ExcelTemplate の `{{use:...}}` セル値トリガを構造化する `UseTriggerParser` を追加した。

今回反映した内容:

- `UseTriggerParser` を追加
- trigger モデルを追加
  - `ExcelTemplateUseTrigger`
  - `ExcelTemplateUseTriggerParseResult`
- 初期対応文法
  - `{{use:Header}}`
  - `{{use:ItemRow, from:@items, var:item}}`
- repeat trigger の正規化
  - `from` と `var` が両方ある場合のみ repeat とみなす
  - repeat は `direction="down"` を parser 結果で固定
- 異常系
  - `from` だけ / `var` だけ
  - 未対応 key
  - 閉じ括弧欠落
  を `InvalidAttributeValue` Error として返す
- 非 trigger 文字列は通常セル値扱いのため `IsTrigger = false` / issue なしで返す

## 2. テスト

TDD:

1. `ExcelTemplateUseTriggerParserTests` を先に追加して Red を確認
2. 最小文法だけ実装して Green 化

実施:

- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter "FullyQualifiedName~ExcelTemplateUseTriggerParserTests"`
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj`

結果:

- parser 対象テスト: 5 passed
- 全体テスト: 228 passed, 0 failed

## 3. 次の実装候補

次は `ExcelTemplateValidator` に進み、以下を Error/Warning として固定する。

1. merged cell が component range 境界を跨ぐケース
2. malformed defined name / unsupported trigger の診断集約
3. 初版対象外機能の Warning/Error 化
