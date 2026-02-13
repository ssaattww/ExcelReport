# WorksheetState 詳細設計書 v1

## Status
- As-Is (Planned): 実装クラス/IF は未実装（証跡: `reports/implementation-inventory-2026-02-13.md:33`）。
- To-Be (Planned): LayoutEngine の出力を最終状態へ固定化し Renderer に受け渡す（証跡: `reports/issues-and-improvements-2026-02-13.md:98`）。

# 1. 概要

## 1.1 モジュール名

- モジュール名: `WorksheetState`
- 所属アセンブリ想定: `ExcelReport.Core`

## 1.2 位置づけ

WorksheetState は、LayoutEngine が構築した LayoutPlan を受け取り、  
Excel 出力直前の **最終的なシート状態（WorkbookState / WorksheetState）** を保持する中核モジュールである。

このモジュールの役割は、Renderer が追加ロジックなしで値・数式・スタイル・結合セル・シート設定を機械的に投影できるよう、  
**矛盾のない「最終状態データモデル」を生成すること** にある。

## 1.3 モジュール全体の目的

- LayoutEngine が行ったスタイル優先順位解決・座標割当・エリア決定などを  
  **確定値として統合し、永続的なデータ構造へ変換**
- Excel のシートに必要な情報（セル、結合、スタイル辞書、印刷設定 等）を  
  **Renderer が直接利用できる形式** に変換する
- 下流（Renderer）が解釈・推論・決定を行う必要がないよう、  
  **WorksheetState が完全な最終形を保持** する

---

# 2. 責務

## 2.1 コア責務

WorksheetState モジュールは、LayoutEngine が生成した LayoutPlan を基に  
Excel 出力に必要な **最終的・完全なシート状態モデル** を構築する。

主な責務は以下のとおり。

1. **セル状態（CellState）の構築**
   - 値・数式・エラー状態の確定
   - FinalStyle の適用結果を StyleSnapshot として格納
   - 結合セルヘッダ情報の付与
   - formulaRef 系列名の付与

2. **結合セル（MergedRange）の検証と確定**
   - 矩形範囲の検証
   - 重複・範囲外・欠損などの不整合検知
   - 正常な結合セルのみを確定リストへ格納

3. **名前付き領域（Area）の構築**
   - LayoutEngine の AreaLayout を変換
   - 同名領域の後勝ち処理
   - 競合時の Warning Issue 生成

4. **formulaRef 系列の展開**
   - 1 次元連続系列であることを前提に、セル座標リストを保持
   - 空系列時の Warning 追加

5. **スタイル辞書（StyleSnapshot）の構築**
   - FinalStyle から StyleSnapshot を生成
   - Workbook 全体で重複スタイルを排除（値比較による辞書化）
   - Renderer がそのまま物理スタイル ID に写像可能な形式で保持
   - 優先順位解決ロジックは持たない（LayoutEngine 側で完結）

6. **シートオプション（SheetOptions）の確定**
   - FreezePane、Print 設定、View 設定、AutoFilter などを中間モデルに写像
   - Renderer が判断なしでそのまま適用できるようにする

7. **WorkbookState の統合**
   - シートごとの WorksheetState を集約し、Workbook 全体に対して
     - スタイル辞書
     - Issue の統合
     を行い WorksheetWorkbookState を構築する

## 2.2 Issue（エラー・警告）生成責務

WorksheetState は以下の検証を行い、必要に応じて Issue（Fatal / Error / Warning）を生成する。

- セル占有の重複
- 結合セルの重複・範囲外・欠損
- Area の同名定義競合
- 空の FormulaSeries
- シートオプションの矛盾（例: FreezePane が範囲外）

※ WorksheetState は LayoutEngine が保証しない最終整合性の責務を負う。

## 2.3 下流（Renderer）に対する契約

WorksheetState は Renderer に対し以下の前提を保証する。

- セルはすべて有効範囲内であり、重複していない
- 結合セルは矩形で重複がない
- Area / FormulaSeries は確定済みで解釈不要
- StyleSnapshot は最終スタイルとして矛盾がない
- シートオプションは中間モデルとして整合性がある

Renderer は追加判断・推論をせず、WorksheetState の内容をそのまま適用するだけでよい。

---

# 3. 非責務

## 3.1 WorksheetState が行わないこと

WorksheetState モジュールは「最終状態の保持」に特化しており、  
以下の処理は **担当しない**。

1. **スタイル優先順位の決定**
   - FinalStyle は LayoutEngine がすべての候補スタイル（styleRef / inline / scope 情報等）から決定済み。
   - WorksheetState は **合成済みの結果だけ** を StyleSnapshot に格納する。

2. **レイアウト計算**
   - 行高さ・列幅
   - 繰返し展開（repeat）
   - 行列の座標計算
   - これらは LayoutEngine の責務。

3. **DSL 構文解析**
   - SheetAst / CellAst / StyleAst 等の構築や XSD 検証は DslParser の責務。

4. **外部スタイル・外部コンポーネントの読込**
   - Styles モジュールおよび DslParser が行う。
   - WorksheetState はグローバルスタイル定義を参照しない。

5. **式評価（ExpressionEngine）**
   - 値変換・関数評価・C# 式解釈は ExpressionEngine の責務。
   - WorksheetState は評価後の値または式（文字列）を保持するのみ。

6. **Excel の物理書き込み（Renderer）**
   - セルの書式設定、値設定、結合操作、FreezePane 等は Renderer が行う。
   - WorksheetState は必要な中間モデルを提供するだけで、具体的な Excel API を呼ばない。

7. **Issue の全体統合・ログ出力**
   - WorksheetState は Issue を生成するが、
     - Issue の集約
     - ReportGenerator への通知
     - ログ・監査出力  
     は ReportGenerator / Logger の責務。

## 3.2 非責務の明確化理由

WorksheetState は Renderer が「機械的に出力できる最終状態」を提供するため、  
途中の判断・推論・合成は上流モジュール（LayoutEngine / Styles / DslParser）が担当する。

役割を固定することで：

- モジュール間依存の明確化
- 変更時の影響範囲の限定
- Renderer の単純化
- LayoutEngine の責務集中（スタイル合成や適用順序決定）

を実現するためである。

---

# 4. データモデル

## 4.1 WorkbookState（全体構造）

WorkbookState は Renderer が直接利用する「最終的な Excel 投影用データ」のルートモデルである。

```
WorksheetWorkbookState
  - Sheets: List<WorksheetState>
  - Styles: List<StyleSnapshot>            // Workbook 全体の最終スタイル辞書
  - Issues: List<Issue>                    // 全シートから集約
```

- **Styles** は Workbook 単位で重複排除済みスタイル辞書  
  Renderer はこれをベースに Excel のスタイル ID を採番する。

## 4.2 WorksheetState（シート単位モデル）

```
WorksheetState
  - Name: string
  - Rows: int
  - Cols: int
  - Cells: List<CellState>
  - MergedRanges: List<MergedRange>
  - NamedAreas: Dictionary<string, Area>
  - FormulaSeriesMap: Dictionary<string, FormulaSeries>
  - SheetOptions: SheetOptions
  - Issues: List<Issue>
```

- レンダラーは **WorksheetState の内容を変更しない**
- 座標はすべて **1-based**
- セルの整合性は WorksheetStateBuilder により保証される

## 4.3 CellState（セルの最終状態）

```
CellState
  - Row: int
  - Col: int
  - ValueKind: Constant / Formula / Error / Blank
  - ConstantValue: object?
  - Formula: string?
  - ErrorText: string?
  - Style: StyleSnapshot            // 最終スタイル
  - IsMergedHead: bool
  - MergedRange: MergedRange?
  - FormulaRefName: string?
```

### ポイント  
- **FinalStyle の合成は LayoutEngine 側で完了済み**
- WorksheetState は FinalStyle → StyleSnapshot の変換のみ行う

## 4.4 StyleSnapshot（最終スタイル）

LayoutEngine が決定した最終スタイル（FinalStyle）を  
Excel 投影可能な論理形式に変換したもの。

```
StyleSnapshot
  - FontName: string?
  - FontSize: double?
  - FontBold: bool?
  - FontItalic: bool?
  - FontUnderline: bool?
  - FillColor: string?
  - NumberFormatCode: string?
  - Border: BorderSnapshot?
  - AppliedStyleNames: List<string>        // 合成元スタイルの履歴（トレース用）
```

- **Workbook 単位で値比較し、重複排除**
- Excel の物理スタイル ID は Renderer が付与する

## 4.5 BorderSnapshot

```
BorderSnapshot
  - Top: string?
  - Bottom: string?
  - Left: string?
  - Right: string?
  - Color: string?
```

## 4.6 SheetOptions（印刷・ビュー・AutoFilter 設定）

```
SheetOptions
  - Print: PrintOptions?
  - View: ViewOptions?
  - AutoFilter: AutoFilterOptions?
```

### PrintOptions
```
PrintOptions
  - PrintArea: string?         // "A1:D50"
  - FitToPage: bool?
  - Landscape: bool?
```

### ViewOptions
```
ViewOptions
  - FreezeTopRow: int?
  - FreezeLeftColumn: int?
  - ZoomScale: double?
```

### AutoFilterOptions
```
AutoFilterOptions
  - HeaderRow: int?
  - FirstColumn: int?
  - LastColumn: int?
```

- Excel API へそのまま写像可能な構造のみ採用

## 4.7 MergedRange（結合セル）

```
MergedRange
  - Top: int
  - Left: int
  - RowSpan: int
  - ColSpan: int
  - Bottom = Top + RowSpan - 1
  - Right  = Left + ColSpan - 1
```

- WorksheetStateBuilder で検証済  
- 重複・範囲外・欠損は Issue 発行

## 4.8 Area（名前付き領域）

```
Area
  - Name: string
  - Top: int
  - Bottom: int
  - Left: int
  - Right: int
```

- 同名 Area が複数ある場合は後勝ち  
- Warning Issue を追加

## 4.9 FormulaSeries（formulaRef 系列）

```
FormulaSeries
  - Name: string
  - Orientation: Row / Column
  - Cells: List<(Row, Col)>
```

- LayoutEngine が 1次元連続を保証  
- 空系列は Warning

---

# 5. 処理モデル（Build フロー）

## 5.1 Build 全体フロー概要

WorksheetStateBuilder は LayoutPlan を受け取り、  
Renderer が直接利用できる WorksheetWorkbookState を構築する。

主要ステップは以下のとおり。

1. 各シート（LayoutPlan.Sheets）を順に処理
2. LayoutCell から CellState を構築
3. 結合セル（MergedRange）の検証と確定
4. AreaLayout（名前付き領域）の構築
5. FormulaSeriesLayout の展開
6. FinalStyle → StyleSnapshot への変換
7. SheetOptions の写像
8. Workbook 単位でのスタイル辞書（StyleSnapshot）の重複排除
9. Issue の集約と WorksheetWorkbookState の構築

## 5.2 セル処理の詳細

- `(Row, Col)` の占有重複を検知  
- FinalStyle を StyleSnapshot に変換  
- 値・数式・エラー状態を CellState に設定  
- RowSpan / ColSpan が 1 より大きい場合は結合候補として登録

## 5.3 結合セルの検証

- シート範囲外 → Fatal
- 結合領域の重複 → Error
- 結合領域の欠損 → Error
- 正常な領域のみ MergedRanges に登録

## 5.4 Area の構築

- 同名 Area は後勝ち
- 競合発生時は Warning
- 位置情報は LayoutEngine で決まっており WorksheetState 側ではコピーのみ

## 5.5 FormulaSeries の構築

- LayoutEngine が 1D 連続性を保証
- 空系列は Warning
- 方向（Row / Column）とセル座標列を保持

## 5.6 StyleSnapshot の構築

- FinalStyle（LayoutEngine 決定済）を StyleSnapshot に変換
- Workbook 単位で内容比較による重複排除
- Renderer が Excel スタイル ID を付与する前段階

## 5.7 SheetOptions の構築

- FreezePane / Print / View / AutoFilter をモデル化
- 不整合は Warning または Error

## 5.8 Workbook 統合処理

- 全 WorksheetState を集約  
- StyleSnapshot を dedupe（値比較）して Styles を作成  
- Issue をすべて統合  
- WorksheetWorkbookState として返却

---

# 6. API

## 6.1 IWorksheetStateBuilder

WorksheetStateBuilder は WorksheetState モジュールの唯一の公開 API であり、  
LayoutPlan から最終的な WorksheetWorkbookState を生成する責務を持つ。

```
public interface IWorksheetStateBuilder
{
    WorksheetWorkbookState Build(LayoutPlan layoutPlan);
}
```

### 特徴
- **純粋関数的**：入力 LayoutPlan に基づき、常に同じ WorksheetWorkbookState を生成する  
- **副作用なし**：外部ファイルアクセス・式評価・レンダリング処理は行わない  
- **Issue 生成機能を内包**：不整合検出は build プロセス内で実施

## 6.2 WorksheetWorkbookState（戻り値の契約）

```
WorksheetWorkbookState
  - Sheets: IReadOnlyList<WorksheetState>
  - Styles: IReadOnlyList<StyleSnapshot>
  - Issues: IReadOnlyList<Issue>
```

### 契約

1. **Sheets は LayoutPlan.Sheets と同じ論理順**  
2. **Styles は Workbook 全体で重複排除済みの最終スタイル辞書**  
3. **Issues は WorksheetStateBuilder が生成したすべての Issue を統合したもの**

## 6.3 IWorksheetStateBuilder の使用例（最小）

```
var builder = new WorksheetStateBuilder();
var workbookState = builder.Build(layoutPlan);

// 下流では workbookState を Renderer に渡すだけでよい
renderer.Render(workbookState, outputPath);
```

- 下流処理は **WorksheetWorkbookState の内容を変更しないこと**  
- 再評価・再レイアウト・再スタイル合成は行われない

---

# 7. エラーモデル

## 7.1 Issue の役割

WorksheetState は LayoutEngine では検出しきれない最終整合性の検証を行い、  
不整合があれば Issue を生成する。

Issue は下流で ReportGenerator によって統合され、  
監査情報（AuditLog）にも転送される。

## 7.2 IssueSeverity の使い分け

- Fatal  
  シートの構築を継続できない致命的な不整合。  
  例: 結合セルがシート範囲を超える、座標値が Excel 上限を超える。

- Error  
  一部のセルや領域が欠損するが、WorkbookState の構築自体は可能。  
  例: 結合セルが欠損、結合セル同士の重複。

- Warning  
  出力は可能だが注意を要する事象。  
  例: 同名 Area の競合、空の FormulaSeries、問題のある SheetOptions 値。

## 7.3 WorksheetStateBuilder が生成する Issue 種別

1. セル占有重複  
   - CellOverlap(row, col)

2. 結合セル関連  
   - MergeRangeOutOfSheet  
   - MergeRangeConflict  
   - MergeRangeIncomplete

3. Area  
   - AreaDuplicateName(name)

4. FormulaSeries  
   - FormulaSeriesEmpty(name)

5. SheetOptions  
   - SheetOptionOutOfRange  
   - SheetOptionInvalidValue

## 7.4 下流（Renderer）との整合性

WorksheetState 内で Fatal が発生したシートは、  
Renderer が読み取る前に ReportGenerator 側で処理停止となる。

Warning と Error は Renderer に渡されるが、  
Renderer は WorksheetState が保証する整合性範囲内で出力を続行する。

---

# 8. 性能

## 8.1 設計上の前提

WorksheetState はレンダリング直前のシート状態を保持するだけであり、  
計算量を増加させるロジック（レイアウト計算や優先順位解決）は存在しない。

そのため、性能要件は以下の点に限定される。

## 8.2 時間計算量の目安

1. セル処理  
   O(N)  
   N はシート内の LayoutCell 数。  
   CellState の構築および占有チェックは辞書アクセスによる O(1) で行われる。

2. 結合セル検証  
   O(M log M)  
   M は結合セル候補数。  
   領域の重複検出にソートを用いるため。

3. スタイル辞書（StyleSnapshot）重複排除  
   O(N)  
   ハッシュ比較によるスタイル同値チェックを想定。

4. Area / FormulaSeries / SheetOptions  
   O(K)  
   K は各要素数で、いずれも LayoutEngine の出力に依存し小規模。

## 8.3 メモリ利用量

- WorksheetState は Excel のセル情報を中間表現として保持するため、  
  少なくとも LayoutCell 相当のメモリを消費する。
- StyleSnapshot は Workbook 全体で dedupe されるため、  
  スタイル定義が多くても重複を抑制できる。

## 8.4 大規模シートに対する考慮

- 10 万セル規模では O(N) の処理が支配的となり、  
  実行時間は主に CellState 構築とスタイル辞書化で決まる。
- WorksheetState は不変データ構造として扱うため、  
  スレッドロックによる性能劣化は発生しない。

## 8.5 下流（Renderer）への影響

- WorksheetState の構造は Renderer が線形走査するだけで済むよう設計されている。
- Renderer の性能ボトルネックは Excel API 側にあるため、  
  WorksheetState が追加の負荷を与えることはない。

---

# 9. テスト観点

## 9.1 正常系テスト

1. **セル配置の基本動作**
   - LayoutCell から CellState が正しく構築されること
   - 値・数式・エラーが正しく反映されること

2. **結合セルの正常構築**
   - 正しい RowSpan / ColSpan の結合が MergedRange として登録されること
   - IsMergedHead が適切に設定されていること

3. **Area / FormulaSeries / SheetOptions の反映**
   - LayoutEngine の出力と WorksheetState の各要素が一致すること
   - シートオプションが中間モデルに正しく写像されること

4. **StyleSnapshot の構築**
   - FinalStyle が StyleSnapshot として正しく変換されていること
   - Workbook 単位で重複排除されること

5. **複数シートの WorkbookState**
   - 複数シートが論理順に格納されること
   - スタイル辞書が Workbook 単位で共有されること


## 9.2 異常系テスト（Error / Fatal）

1. **セル占有重複**
   - 同じ (Row, Col) に複数の LayoutCell がある場合に Issue(CellOverlap) が出ること

2. **結合セル不整合**
   - 範囲外 → Fatal
   - 領域重複 → Error
   - 結合セルの一部セル欠損 → Error

3. **シートオプション不整合**
   - FreezePane が範囲外 → Error
   - PrintArea が不正 → Warn または Error

4. **座標範囲外**
   - Excel の行列上限を超えた場合に Fatal


## 9.3 Warning 系

1. **同名 Area の競合**
   - 後勝ちで上書きされ、Warning が出ること

2. **空の FormulaSeries**
   - Warning が出ること

3. **軽微な SheetOptions 不整合**
   - 不適切な Zoom 値などを Warning として受理


## 9.4 性能テスト

1. **大規模シート（10 万セル級）**
   - WorksheetStateBuilder.Build が許容時間内に完了すること
   - スタイル辞書化が O(N) で終わること

2. **大量結合セル・大量 Area**
   - 結合セル検証が O(M log M) で収束すること


## 9.5 モジュール連携テスト（上流・下流）

1. **LayoutEngine のモック**
   - 正常な LayoutPlan を渡した場合の WorkbookState 構築を確認
   - 不正な FinalStyle や座標を渡し Warning / Error の発行を確認

2. **Renderer との協調**
   - Renderer が WorksheetState をそのまま利用できること
   - WorksheetState の前提（結合セル整合性など）が守られていることを確認
 
 ---

 # 10. 最小実装例

## 10.1 WorksheetStateBuilder の最小構造

WorksheetStateBuilder は LayoutPlan を入力とし、  
WorksheetWorkbookState を構築する唯一の実装クラスである。

```csharp
public sealed class WorksheetStateBuilder : IWorksheetStateBuilder
{
    public WorksheetWorkbookState Build(LayoutPlan layoutPlan)
    {
        var sheets = new List<WorksheetState>();
        var issues = new List<Issue>();
        var styleMap = new Dictionary<StyleSnapshot, StyleSnapshot>();

        foreach (var sheetLayout in layoutPlan.Sheets)
        {
            var sheetIssues = new List<Issue>();
            var cellMap = new Dictionary<(int Row, int Col), CellState>();
            var mergedCandidates = new List<MergedRange>();

            foreach (var lc in sheetLayout.Cells)
            {
                var key = (lc.Row, lc.Col);

                if (cellMap.ContainsKey(key))
                {
                    sheetIssues.Add(Issue.CellOverlap(sheetLayout.Name, lc.Row, lc.Col));
                    continue;
                }

                var styleSnapshot = ConvertFinalStyle(lc.FinalStyle, sheetIssues);
                if (!styleMap.ContainsKey(styleSnapshot))
                    styleMap[styleSnapshot] = styleSnapshot;

                var cellState = new CellState(
                    lc.Row,
                    lc.Col,
                    GetKind(lc),
                    lc.ConstantValue,
                    lc.Formula,
                    lc.ErrorText,
                    styleMap[styleSnapshot],
                    lc.IsMergedHead,
                    null,
                    lc.FormulaRefName
                );

                cellMap[key] = cellState;

                if (lc.RowSpan > 1 || lc.ColSpan > 1)
                {
                    mergedCandidates.Add(new MergedRange(
                        lc.Row,
                        lc.Col,
                        lc.RowSpan,
                        lc.ColSpan
                    ));
                }
            }

            var mergedRanges = ValidateMergedRanges(
                sheetLayout,
                mergedCandidates,
                cellMap,
                sheetIssues
            );

            var namedAreas = BuildAreas(sheetLayout, sheetIssues);
            var formulaSeries = BuildFormulaSeries(sheetLayout, sheetIssues);
            var sheetOptions = BuildSheetOptions(sheetLayout, sheetIssues);

            sheets.Add(new WorksheetState(
                sheetLayout.Name,
                sheetLayout.Rows,
                sheetLayout.Cols,
                cellMap.Values.OrderBy(x => (x.Row, x.Col)).ToList(),
                mergedRanges,
                namedAreas,
                formulaSeries,
                sheetOptions,
                sheetIssues
            ));

            issues.AddRange(sheetIssues);
        }

        return new WorksheetWorkbookState(
            sheets,
            styleMap.Keys.ToList(),
            issues
        );
    }

    private CellValueKind GetKind(LayoutCell lc) =>
        lc.ErrorText != null ? CellValueKind.Error :
        lc.Formula != null ? CellValueKind.Formula :
        lc.ConstantValue != null ? CellValueKind.Constant :
        CellValueKind.Blank;
}
```

## 10.2 各補助メソッド（概念レベル）

### ConvertFinalStyle

FinalStyle を StyleSnapshot に変換する。

```csharp
private StyleSnapshot ConvertFinalStyle(FinalStyle final, List<Issue> issues)
{
    return new StyleSnapshot(
        final.FontName,
        final.FontSize,
        final.FontBold,
        final.FontItalic,
        final.FontUnderline,
        final.FillColor,
        final.NumberFormatCode,
        final.Border != null
            ? new BorderSnapshot(
                final.Border.Top,
                final.Border.Bottom,
                final.Border.Left,
                final.Border.Right,
                final.Border.Color)
            : null,
        final.AppliedStyleNames
    );
}
```

### ValidateMergedRanges（結合セル検証）

```csharp
private List<MergedRange> ValidateMergedRanges(
    SheetLayout sheet,
    List<MergedRange> candidates,
    Dictionary<(int Row, int Col), CellState> cellMap,
    List<Issue> issues)
{
    var result = new List<MergedRange>();

    foreach (var mr in candidates)
    {
        if (mr.Bottom > sheet.Rows || mr.Right > sheet.Cols)
        {
            issues.Add(Issue.MergeRangeOutOfSheet(sheet.Name, mr.Top, mr.Left));
            continue;
        }

        result.Add(mr);
    }

    return result;
}
```

### BuildAreas / BuildFormulaSeries / BuildSheetOptions

各レイアウト要素を WorksheetState モデルへ変換する。  
実際の変換内容は LayoutEngine の出力仕様に依存するため省略される。
