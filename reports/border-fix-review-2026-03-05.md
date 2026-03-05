# border-fix review (2026-03-05)

## 対象
- `ExcelReport/ExcelReportLib/Renderer/XlsxRenderer.cs`
- `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs`
- `ExcelReport/ExcelReportLib.Tests/RendererTests.cs`
- `ExcelReport/ExcelReportLib.Tests/LayoutEngineTests.cs`
- `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs`

## Critical
- 指摘なし

## Major
1. **`mode=outer/all` が有効に適用される一方で、`IssueKind.StyleScopeViolation` 警告がセル数分発生し続ける**
- 根拠:
  - セル展開時は常に `StyleTarget.Cell` で `BuildPlan` を実行しているため、grid scope style が警告対象になる: `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:158`
  - その警告ロジックは `StyleResolver` 側で維持されたまま: `ExcelReport/ExcelReportLib/Styles/StyleResolver.cs:339`, `ExcelReport/ExcelReportLib/Styles/StyleResolver.cs:361`
  - しかし今回の差分で `ApplyGridBorders()` が同じ grid border を後段で再適用しており、警告メッセージと実挙動が矛盾する: `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:678`
- 影響:
  - `mode=outer/all` を使う正当なテンプレートでも、`Issues` が大量の Warning で汚染される。
  - `ReportGenerator` 利用側で監視/品質ゲートをしている場合、誤検知につながる。

2. **expanded grid border の追記位置により、セル個別 border より grid border が後勝ちになる（優先順位逆転）**
- 根拠:
  - expanded border は既存 border の後ろに常に連結される: `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:889`
  - レンダラでは「後勝ち」で side/color をマージする: `ExcelReport/ExcelReportLib/Renderer/XlsxRenderer.cs:907`
- 影響:
  - 本来「より局所的なセルスタイルが優先」すべきケースで、grid由来 border が上書きする可能性が高い。
  - 例: grid style(outer) + cell inline style(cell top=double) の同居時に、top が grid 側へ戻る。

## Minor
1. **`StylePlan.Borders` と `StylePlan.BorderTraces` の整合が崩れる**
- 根拠:
  - border は追加後配列で再構築しているが: `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:889`
  - trace は既存 `stylePlan.BorderTraces` をそのまま引き継いでいる: `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:922`
- 影響:
  - デバッグ/説明用途で trace を参照する処理が将来入ると、出所情報の欠落や件数不一致を招く。

2. **テストの検証不足で、上記 Major 問題を検出できない**
- 根拠:
  - `LayoutEngineTests` の新規2ケースは border 値のみ検証し、`plan.Issues` を検証していない: `ExcelReport/ExcelReportLib.Tests/LayoutEngineTests.cs:214`, `ExcelReport/ExcelReportLib.Tests/LayoutEngineTests.cs:252`
  - `RendererTests` は side/color の後勝ちを検証しているが、grid展開後の優先順位（grid vs cell）を検証していない: `ExcelReport/ExcelReportLib.Tests/RendererTests.cs:189`
  - `ReportGeneratorTests` の統合ケースは値読取り中心で、border優先順位・Issue健全性を検証していない: `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs:160`

## 観点別サマリ
1. **ToBorder() の CT_Border 子要素順**
- `Left, Right, Top, Bottom, Diagonal` 順になっており正しい: `ExcelReport/ExcelReportLib/Renderer/XlsxRenderer.cs:891`

2. **MergeBorders() 後勝ちマージ**
- side 単位後勝ちの実装自体は妥当。
- ただし `ApplyGridBorders()` 側の追記順により、意図しない優先順位逆転（Major-2）が発生。

3. **ApplyGridBorders() の mode=outer/all 展開**
- 単純な 2x2 ケースでは期待どおり。
- ただし warning整合性（Major-1）と優先順位（Major-2）に問題あり。

4. **テスト十分性**
- 不十分（Minor-2）。

5. **既存コード整合性（StylePlan ctor, LayoutCell生成）**
- 引数順・受け渡しは整合している: `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:863`, `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:908`

6. **XMLドキュメントコメント（FP19）**
- 今回追加された production code の public/protected API はなし。
- 追加 private メソッドには XML コメント付与済み（不足なし）。

7. **バグ/論理エラー**
- Major 2件（warning矛盾、border優先順位逆転）、Minor 2件（trace不整合、テスト不足）。

## 実行確認
- `dotnet test` はローカル SDK が `.NET 10.0` ターゲット未対応のため実行不可。
  - エラー: `NETSDK1045`（SDK 8.0.416）
