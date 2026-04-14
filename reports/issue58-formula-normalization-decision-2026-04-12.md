# issue #58 数式正規化方針の確定

## 決定
- 数式セルは原則 `cell@formula` へ変換する。
- C# 側で Excel 関数の計算を再実装することは想定しない。
- したがって、数式セルを `cell@value` の計算済み値へ潰す運用は採らない。

## 反映
- `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`
  - 5章の DSL マッピングルールを `cell@formula` 原則へ修正
  - 12.1 の事前合意内容を確定事項へ修正
  - 12.6 から数式正規化の確認質問を削除

## 補足
- これは「Excel 上で持っている数式を保持する」方針であり、C# 側で Excel 関数の意味論まで再現する仕様ではない。
