# DslParser 詳細設計書 v1（XSD ⇔ AST 対応版・省略なし）

本書は ExcelReport DSL のパーサコンポーネント **DslParser** の詳細設計を示す。  
XSD スキーマ（`DslDefinition_v1.xsd`）と AST クラス群の対応を明示し、実装および保守に必要な情報をすべて含む。

---

## 1. 概要・責務

### 1.1 モジュール名

- モジュール名: `DslParser`
- 所属アセンブリ想定: `ExcelReport.Core`（仮）

### 1.2 役割

DslParser は、ExcelReport DSL の XML 定義を入力として受け取り、以下を行う。

1. XML パース（`XDocument`）
2. XSD スキーマ（`DslDefinition_v1.xsd`）による構文検証
3. AST（抽象構文木）ノード群の構築
4. DSL 固有ルールに基づく検証
   - 定義・参照の整合性
   - repeat ノード制約
   - sheetOptions の参照妥当性
   - 静的レイアウトの制約（行列上限、formulaRef 系列の形状など）
5. 検証結果を `Issue` として収集

出力は、後続モジュール（LayoutEngine）で利用される `WorkbookAst` と、検証結果 `Issue` 一覧である。

### 1.3 責務範囲

**責務 (IN)**

- 入力フォーマット:
  - DSL XML 文字列 (`string`)
  - DSL XML ストリーム (`Stream`)
- 処理:
  - XMLパース（構文エラー検知）
  - XSD スキーマ検証（型・必須属性・構造）
  - AST 構築
  - DSL 仕様に基づく検証ロジック
- 出力:
  - `WorkbookAst`（AST ルート）
  - `Issue` 一覧

**非責務 (OUT)**

- C# 式の評価:
  - `@( ... )` 形式の式評価
  - `use@with`, `repeat@from`, `Placement.when` の式評価
  - これらは ExpressionEngine の責務
- LayoutPlan の生成:
  - AST から行列配置計画（LayoutPlan）への変換
  - repeat 展開、座標計算
  - LayoutEngine の責務
- Excel ファイル出力:
  - ClosedXML 等を用いた `.xlsx` 生成
  - Renderer の責務

### 1.4 想定ライフサイクル

- DslParser は **テンプレート読み込みフェーズ** で利用される。
- 1 テンプレートにつき 1 回以上パースされ、結果の AST はキャッシュされる想定。
- AST は読み取り専用として扱い、LayoutEngine 等が参照する。

---

## 2. 公開 API

### 2.1 オプション・結果モデル

```csharp
public sealed class DslParserOptions
{
    /// <summary>XML スキーマ検証を有効化するか。</summary>
    public bool EnableSchemaValidation { get; init; } = true;

    /// <summary>C# 式の構文エラーを Fatal として扱うか。</summary>
    public bool TreatExpressionSyntaxErrorAsFatal { get; init; } = true;
}

public enum IssueSeverity
{
    Info,
    Warning,
    Error,
    Fatal
}

public enum IssueKind
{
    // XML レベル
    XmlMalformed,
    SchemaViolation,

    // 定義・参照
    UndefinedComponent,
    UndefinedStyle,
    DuplicateComponentName,
    DuplicateStyleName,
    DuplicateSheetName,

    // スタイル
    StyleScopeViolation,

    // repeat / layout
    RepeatChildCountInvalid,
    CoordinateOutOfRange,
    FormulaRefSeriesNot1DContinuous,

    // sheetOptions
    SheetOptionsTargetNotFound,

    // 式
    ExpressionSyntaxError,
}

public sealed class Issue
{
    public IssueSeverity Severity { get; init; }
    public IssueKind Kind { get; init; }
    public string Message { get; init; } = string.Empty;
    public SourceSpan? Span { get; init; }  // ファイル名、行、列など
}

public sealed class DslParseResult
{
    public WorkbookAst? Root { get; init; }
    public IReadOnlyList<Issue> Issues { get; init; } = Array.Empty<Issue>();

    public bool HasFatal => Issues.Any(i => i.Severity == IssueSeverity.Fatal);
}
```

### 2.2 DslParser インターフェース

```csharp
public interface IDslParser
{
    DslParseResult ParseFromText(string xmlText, DslParserOptions? options = null);

    DslParseResult ParseFromStream(Stream xmlStream, DslParserOptions? options = null);
}
```

### 2.3 最小実装例（骨格）

```csharp
public sealed class XmlDslParser : IDslParser
{
    private readonly XmlSchemaSet _schemaSet;

    public XmlDslParser(XmlSchemaSet schemaSet)
    {
        _schemaSet = schemaSet;
    }

    public DslParseResult ParseFromText(string xmlText, DslParserOptions? options = null)
    {
        options ??= new DslParserOptions();
        using var reader = new StringReader(xmlText);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlText));
        return ParseFromStream(stream, options);
    }

    public DslParseResult ParseFromStream(Stream xmlStream, DslParserOptions? options = null)
    {
        options ??= new DslParserOptions();
        var issues = new List<Issue>();

        XDocument? doc;
        try
        {
            doc = XDocument.Load(xmlStream, LoadOptions.SetLineInfo);
        }
        catch (XmlException ex)
        {
            issues.Add(new Issue
            {
                Severity = IssueSeverity.Fatal,
                Kind = IssueKind.XmlMalformed,
                Message = ex.Message,
            });
            return new DslParseResult { Root = null, Issues = issues };
        }

        if (options.EnableSchemaValidation)
        {
            ValidateWithSchema(doc, issues);
            if (issues.Any(i => i.Severity == IssueSeverity.Fatal))
            {
                return new DslParseResult { Root = null, Issues = issues };
            }
        }

        var root = BuildWorkbookAst(doc.Root!, issues);

        ValidateDsl(root, issues, options);

        return new DslParseResult
        {
            Root = issues.Any(i => i.Severity == IssueSeverity.Fatal) ? null : root,
            Issues = issues,
        };
    }

    private void ValidateWithSchema(XDocument doc, List<Issue> issues)
    {
        // XmlReaderSettings に _schemaSet を設定し、検証イベントで Issue を追加する。
        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = _schemaSet
        };
        settings.ValidationEventHandler += (sender, e) =>
        {
            issues.Add(new Issue
            {
                Severity = IssueSeverity.Fatal,
                Kind = IssueKind.SchemaViolation,
                Message = e.Message,
            });
        };

        using var reader = doc.CreateReader();
        using var validatingReader = XmlReader.Create(reader, settings);
        while (validatingReader.Read())
        {
            // すべてのノードを読み進めることで検証を完了させる
        }
    }

    private WorkbookAst BuildWorkbookAst(XElement workbookElem, List<Issue> issues)
    {
        // ルート <workbook> 要素から各子要素を AST に変換する。
        var stylesElem = workbookElem.Element(workbookElem.Name.Namespace + "styles");
        StylesAst? stylesAst = stylesElem != null ? BuildStylesAst(stylesElem, issues) : null;

        var componentElems = workbookElem.Elements(workbookElem.Name.Namespace + "component");
        var components = componentElems.Select(e => BuildComponentAst(e, issues)).ToList();

        var sheetElems = workbookElem.Elements(workbookElem.Name.Namespace + "sheet");
        var sheets = sheetElems.Select(e => BuildSheetAst(e, issues)).ToList();

        return new WorkbookAst
        {
            Styles = stylesAst,
            Components = components,
            Sheets = sheets,
            Span = CreateSpan(workbookElem),
        };
    }

    private void ValidateDsl(WorkbookAst root, List<Issue> issues, DslParserOptions options)
    {
        // ここで DSL 固有の検証（未定義参照、repeat 制約、sheetOptions 検証、静的レイアウト検証など）を行う。
        // 具体的な検証内容は 6. エラーモデル と 7. テスト観点を参照して実装する。
    }

    private SourceSpan? CreateSpan(XElement elem)
    {
        if (elem is IXmlLineInfo li && li.HasLineInfo())
        {
            return new SourceSpan
            {
                FileName = null,
                Line = li.LineNumber,
                Column = li.LinePosition,
            };
        }
        return null;
    }
}
```

---

## 3. XSD ⇔ AST マッピング（全体）

この章では `DslDefinition_v1.xsd` の主要要素と AST クラスの対応を一覧で示す。

| XSD 要素/属性                           | AST ノード / フィールド                                   |
|----------------------------------------|-----------------------------------------------------------|
| `<workbook>`                           | `WorkbookAst`                                             |
| `<styles>`                             | `StylesAst`                                               |
| `<style name="" scope="">`             | `StyleAst.Name`, `StyleAst.Scope`                         |
| `<font name="" size="" bold="" ...>`   | `StyleAst._props["font.*"]` → 型付きアクセサ             |
| `<fill color="">`                      | `StyleAst._props["fill.color"]`                           |
| `<border ...>`                         | `StyleAst._props["border"]`（`List<BorderInfo>`）        |
| `<numberFormat code="">`               | `StyleAst._props["numberFormat.code"]`                    |
| `<component name="">`                  | `ComponentAst.Name`, `ComponentAst.Body`                  |
| `<grid>`                               | `GridAst`                                                 |
| `<cell r c rowspan colspan ...>`       | `CellAst` + `Placement`                                   |
| `<use component="" name="" with="">`   | `UseAst` + `Placement`                                    |
| `<repeat from="" var="" direction="">` | `RepeatAst` + `Placement`                                 |
| `<sheet name="" rows="" cols="">`      | `SheetAst.Name`, `SheetAst.Rows`, `SheetAst.Cols`         |
| `<sheetOptions>`                       | `SheetOptionsAst`                                         |
| `<freeze at="">`                       | `FreezeAst.At`                                            |
| `<groupRows at="" collapsed="">`       | `GroupRowsAst.At`, `GroupRowsAst.Collapsed`              |
| `<groupCols at="" collapsed="">`       | `GroupColsAst.At`, `GroupColsAst.Collapsed`              |
| `<autoFilter at="">`                   | `AutoFilterAst.At`                                        |

---

## 4. AST ノード定義（XSD 対応付き）

### 4.1 共通補助クラス

```csharp
public sealed class SourceSpan
{
    public string? FileName { get; init; }
    public int Line { get; init; }
    public int Column { get; init; }
}

public readonly struct Placement
{
    public static readonly Placement None = new Placement(null, null, 1, 1, null);

    public int? Row { get; }
    public int? Col { get; }
    public int RowSpan { get; }
    public int ColSpan { get; }
    public string? WhenExprRaw { get; } // @(...) 式文字列

    public Placement(int? row, int? col, int rowSpan, int colSpan, string? whenExprRaw)
    {
        Row = row;
        Col = col;
        RowSpan = rowSpan <= 0 ? 1 : rowSpan;
        ColSpan = colSpan <= 0 ? 1 : colSpan;
        WhenExprRaw = whenExprRaw;
    }
}

public sealed class StyleRefAst
{
    public string Name { get; init; } = string.Empty;
    public SourceSpan? Span { get; init; }
}
```

---

### 4.2 WorkbookAst（XSD: `<workbook>`）

XSD 上の `workbook` 要素直下に `styles`, `component`, `sheet` が並ぶ構造をそのまま表現する。

```csharp
public sealed class WorkbookAst
{
    public StylesAst? Styles { get; init; }         // <styles>（任意）
    public IReadOnlyList<ComponentAst> Components { get; init; } = Array.Empty<ComponentAst>(); // <component>*
    public IReadOnlyList<SheetAst> Sheets { get; init; } = Array.Empty<SheetAst>(); // <sheet>+
    public SourceSpan? Span { get; init; }
}
```

---

### 4.3 StylesAst（XSD: `<styles>` → `<style>`）

```csharp
public sealed class StylesAst
{
    public IReadOnlyList<StyleAst> Styles { get; init; } = Array.Empty<StyleAst>();
    public SourceSpan? Span { get; init; }
}
```

---

### 4.4 StyleAst（XSD: `<style>` / `<font>` / `<fill>` / `<border>` / `<numberFormat>`）

#### 対応表

| XSD 要素/属性                        | AST フィールド                               |
|-------------------------------------|----------------------------------------------|
| `<style name="" scope="">`          | `StyleAst.Name`, `StyleAst.Scope`           |
| `<font name="" size="" bold="">`    | `_props["font.name"]`, `_props["font.size"]`, `_props["font.bold"]` 等 |
| `<fill color="">`                   | `_props["fill.color"]`                       |
| `<border mode="" top="" ...>`       | `_props["border"]`（`List<BorderInfo>`）   |
| `<numberFormat code="">`            | `_props["numberFormat.code"]`               |

#### AST 定義

```csharp
public enum StyleScope
{
    Cell,
    Grid,
    Both
}

public sealed class StyleAst
{
    public string Name { get; init; } = string.Empty;
    public StyleScope Scope { get; init; } = StyleScope.Both;
    public SourceSpan? Span { get; init; }

    private readonly IReadOnlyDictionary<string, object?> _props;

    public StyleAst(
        string name,
        StyleScope scope,
        IReadOnlyDictionary<string, object?> props,
        SourceSpan? span = null)
    {
        Name = name;
        Scope = scope;
        _props = props;
        Span = span;
    }

    // Font 系アクセサ
    public string? FontName      => Get<string>("font.name");
    public double? FontSize      => Get<double>("font.size");
    public bool?   FontBold      => Get<bool>("font.bold");
    public bool?   FontItalic    => Get<bool>("font.italic");
    public bool?   FontUnderline => Get<bool>("font.underline");

    // Fill
    public string? FillColor     => Get<string>("fill.color");

    // NumberFormat
    public string? NumberFormatCode => Get<string>("numberFormat.code");

    // Border 一覧
    public IReadOnlyList<BorderInfo> Borders
        => Get<IReadOnlyList<BorderInfo>>("border") ?? Array.Empty<BorderInfo>();

    // デバッグ用途
    public IReadOnlyDictionary<string, object?> RawProperties => _props;

    private T? Get<T>(string key)
    {
        if (_props.TryGetValue(key, out var v) && v is T t)
            return t;
        return default;
    }
}

public sealed class BorderInfo
{
    public string? Mode   { get; init; }   // "cell" / "outer" / "all"
    public string? Top    { get; init; }
    public string? Bottom { get; init; }
    public string? Left   { get; init; }
    public string? Right  { get; init; }
    public string? Color  { get; init; }
}
```

---

### 4.5 ComponentAst（XSD: `<component name="">`）

XSD 側では `<component>` 直下に `grid` / `use` / `repeat` のいずれか一つが入る `choice` 構造。

```csharp
public sealed class ComponentAst
{
    public string Name { get; init; } = string.Empty;  // @name
    public LayoutNodeAst Body { get; init; } = default!; // <grid>|<use>|<repeat>
    public SourceSpan? Span { get; init; }
}
```

---

### 4.6 SheetAst（XSD: `<sheet name="" rows="" cols="">`）

#### 対応表

| XSD 要素/属性            | AST フィールド                         |
|-------------------------|----------------------------------------|
| `@name`                 | `SheetAst.Name`                        |
| `@rows`                 | `SheetAst.Rows`                        |
| `@cols`                 | `SheetAst.Cols`                        |
| `<styleRef>`            | `SheetAst.Styles`（StyleRefAst の一覧）|
| `<grid>/<cell>/...`     | `SheetAst.Children`                    |
| `<sheetOptions>`        | `SheetAst.Options`                     |

#### AST

```csharp
public sealed class SheetAst
{
    public string Name { get; init; } = string.Empty;
    public int Rows { get; init; }
    public int Cols { get; init; }

    public IReadOnlyList<StyleRefAst> Styles { get; init; } = Array.Empty<StyleRefAst>();
    public IReadOnlyList<LayoutNodeAst> Children { get; init; } = Array.Empty<LayoutNodeAst>();
    public SheetOptionsAst? Options { get; init; }

    public SourceSpan? Span { get; init; }
}
```

---

### 4.7 LayoutNodeAst（抽象基底）

```csharp
public abstract class LayoutNodeAst
{
    public Placement Placement { get; init; } = Placement.None;
    public SourceSpan? Span { get; init; }
}
```

---

### 4.8 GridAst（XSD: `<grid>`）

```csharp
public sealed class GridAst : LayoutNodeAst
{
    public IReadOnlyList<StyleRefAst> Styles { get; init; } = Array.Empty<StyleRefAst>();
    public IReadOnlyList<LayoutNodeAst> Children { get; init; } = Array.Empty<LayoutNodeAst>();
}
```

---

### 4.9 CellAst（XSD: `<cell>`）

#### XSD 対応

| XSD 属性               | AST フィールド                       |
|------------------------|--------------------------------------|
| `@r`                   | `Placement.Row`                     |
| `@c`                   | `Placement.Col`                     |
| `@rowspan`             | `Placement.RowSpan`                 |
| `@colspan`             | `Placement.ColSpan`                 |
| `@when`                | `Placement.WhenExprRaw`             |
| `@value`               | `CellAst.ValueRaw`                  |
| `@styleRef`            | `CellAst.StyleRefShortcut`          |
| `@formulaRef`          | `CellAst.FormulaRef`                |
| `<styleRef>`           | `CellAst.Styles`                    |

#### AST

```csharp
public sealed class CellAst : LayoutNodeAst
{
    public string? ValueRaw { get; init; }
    public string? StyleRefShortcut { get; init; }
    public string? FormulaRef { get; init; }

    public IReadOnlyList<StyleRefAst> Styles { get; init; } = Array.Empty<StyleRefAst>();
}
```

---

### 4.10 UseAst（XSD: `<use>`）

#### XSD 対応

| XSD 属性           | AST フィールド            |
|--------------------|---------------------------|
| `@component`       | `ComponentName`          |
| `@name`            | `InstanceName`           |
| `@with`            | `WithExprRaw`            |
| Placement 属性群   | `Placement`              |
| `<styleRef>`       | `Styles`                 |

#### AST

```csharp
public sealed class UseAst : LayoutNodeAst
{
    public string ComponentName { get; init; } = string.Empty;
    public string? InstanceName { get; init; }
    public string? WithExprRaw { get; init; }

    public IReadOnlyList<StyleRefAst> Styles { get; init; } = Array.Empty<StyleRefAst>();
}
```

---

### 4.11 RepeatAst（XSD: `<repeat>`）

#### XSD 対応

| XSD 属性           | AST フィールド       |
|--------------------|----------------------|
| `@name`            | `Name`               |
| `@from`            | `FromExprRaw`        |
| `@var`             | `VarName`            |
| `@direction`       | `Direction`          |
| Placement 属性群   | `Placement`          |
| `<styleRef>`       | `Styles`             |
| 子要素（単一）     | `Body`               |

#### AST

```csharp
public enum RepeatDirection
{
    Down,
    Right
}

public sealed class RepeatAst : LayoutNodeAst
{
    public string Name { get; init; } = string.Empty;
    public string FromExprRaw { get; init; } = string.Empty;
    public string VarName { get; init; } = "item";
    public RepeatDirection Direction { get; init; }

    public IReadOnlyList<StyleRefAst> Styles { get; init; } = Array.Empty<StyleRefAst>();
    public LayoutNodeAst Body { get; init; } = default!;
}
```

---

### 4.12 SheetOptionsAst 系（XSD: `<sheetOptions>` 以下）

#### XSD 対応

| XSD 要素                        | AST ノード          | 主フィールド           |
|---------------------------------|---------------------|------------------------|
| `<sheetOptions>`               | `SheetOptionsAst`  | -                      |
| `<freeze at="">`               | `FreezeAst`        | `At`                   |
| `<groupRows at="" collapsed="">` | `GroupRowsAst`     | `At`, `Collapsed`      |
| `<groupCols at="" collapsed="">` | `GroupColsAst`     | `At`, `Collapsed`      |
| `<autoFilter at="">`           | `AutoFilterAst`    | `At`                   |

#### AST

```csharp
public sealed class SheetOptionsAst
{
    public FreezeAst? Freeze { get; init; }
    public IReadOnlyList<GroupRowsAst> GroupRows { get; init; } = Array.Empty<GroupRowsAst>();
    public IReadOnlyList<GroupColsAst> GroupCols { get; init; } = Array.Empty<GroupColsAst>();
    public AutoFilterAst? AutoFilter { get; init; }

    public SourceSpan? Span { get; init; }
}

public sealed class FreezeAst
{
    public string At { get; init; } = string.Empty;
    public SourceSpan? Span { get; init; }
}

public sealed class GroupRowsAst
{
    public string At { get; init; } = string.Empty;
    public bool Collapsed { get; init; }
    public SourceSpan? Span { get; init; }
}

public sealed class GroupColsAst
{
    public string At { get; init; } = string.Empty;
    public bool Collapsed { get; init; }
    public SourceSpan? Span { get; init; }
}

public sealed class AutoFilterAst
{
    public string At { get; init; } = string.Empty;
    public SourceSpan? Span { get; init; }
}
```

---

## 5. 処理フロー詳細

### 5.1 全体フロー

1. `ParseFromText` / `ParseFromStream` で XML 入力を受け取る。
2. `XDocument.Load` で XML パース（`XmlException` を捕捉し Fatal Issue）。
3. `EnableSchemaValidation` が true の場合、XSD 検証を行い、スキーマ違反を Fatal Issue にする。
4. ルート要素 `<workbook>` から `BuildWorkbookAst` を呼び出し、AST 全体を構築する。
5. 構築された AST に対して `ValidateDsl` を実行し、DSL 固有ルールを検証する。
6. Fatal Issue が存在する場合は、`Root` を null にして返却する。

### 5.2 AST 構築処理の概要

- `BuildWorkbookAst`:
  - `<styles>` → `BuildStylesAst`
  - `<component>` → `BuildComponentAst`
  - `<sheet>` → `BuildSheetAst`
- `BuildStylesAst`:
  - `<style>` ごとに `BuildStyleAst`
- `BuildSheetAst`:
  - sheet の属性（name, rows, cols）を読む
  - `<styleRef>` を `StyleRefAst` に変換
  - `<grid>/<cell>/<use>/<repeat>` を順に読み、対応する AST を生成
  - `<sheetOptions>` → `BuildSheetOptionsAst`
- `BuildComponentAst`:
  - `@name` を読み取り
  - 子要素の `grid/use/repeat` を `BuildGridAst` / `BuildUseAst` / `BuildRepeatAst` に委譲

---

### 5.3 StyleAst 構築の最小実装例

```csharp
private StylesAst? BuildStylesAst(XElement stylesElem, List<Issue> issues)
{
    var list = new List<StyleAst>();
    foreach (var styleElem in stylesElem.Elements(stylesElem.Name.Namespace + "style"))
    {
        list.Add(BuildStyleAst(styleElem, issues));
    }

    return new StylesAst
    {
        Styles = list,
        Span = CreateSpan(stylesElem),
    };
}

private StyleAst BuildStyleAst(XElement elem, List<Issue> issues)
{
    var name = (string?)elem.Attribute("name") ?? string.Empty;
    var scopeValue = (string?)elem.Attribute("scope");
    var scope = scopeValue switch
    {
        "cell" => StyleScope.Cell,
        "grid" => StyleScope.Grid,
        _      => StyleScope.Both,
    };

    var props = new Dictionary<string, object?>();

    var ns = elem.Name.Namespace;

    var font = elem.Element(ns + "font");
    if (font != null)
    {
        props["font.name"]      = (string?)font.Attribute("name");
        props["font.size"]      = TryParseDouble((string?)font.Attribute("size"));
        props["font.bold"]      = TryParseBool((string?)font.Attribute("bold"));
        props["font.italic"]    = TryParseBool((string?)font.Attribute("italic"));
        props["font.underline"] = TryParseBool((string?)font.Attribute("underline"));
    }

    var fill = elem.Element(ns + "fill");
    if (fill != null)
    {
        props["fill.color"] = (string?)fill.Attribute("color");
    }

    var nf = elem.Element(ns + "numberFormat");
    if (nf != null)
    {
        props["numberFormat.code"] = (string?)nf.Attribute("code");
    }

    var borderElems = elem.Elements(ns + "border").ToList();
    if (borderElems.Count > 0)
    {
        var borders = new List<BorderInfo>();
        foreach (var b in borderElems)
        {
            borders.Add(new BorderInfo
            {
                Mode   = (string?)b.Attribute("mode"),
                Top    = (string?)b.Attribute("top"),
                Bottom = (string?)b.Attribute("bottom"),
                Left   = (string?)b.Attribute("left"),
                Right  = (string?)b.Attribute("right"),
                Color  = (string?)b.Attribute("color"),
            });
        }
        props["border"] = borders;
    }

    return new StyleAst(name, scope, props, CreateSpan(elem));
}

private double? TryParseDouble(string? raw)
    => double.TryParse(raw, System.Globalization.NumberStyles.Any,
                       System.Globalization.CultureInfo.InvariantCulture, out var v)
       ? v : null;

private bool? TryParseBool(string? raw)
    => bool.TryParse(raw, out var v) ? v : null;
```

---

## 6. エラーモデル詳細

### 6.1 Severity の使い分け

- `Fatal`
  - XML 構文エラー（`XmlMalformed`）
  - XSD スキーマ違反（`SchemaViolation`）
  - 未定義 component/style の参照（`UndefinedComponent`, `UndefinedStyle`）
  - repeat 子要素数の不正（`RepeatChildCountInvalid`）
  - sheetOptions の参照先不明（`SheetOptionsTargetNotFound`）
  - formulaRef 系列が 1 次元連続でない（`FormulaRefSeriesNot1DContinuous`）
  - Excel 行・列の上限超過（`CoordinateOutOfRange`）
- `Warning`
  - スタイル scope 違反（`StyleScopeViolation`）
    - 例: scope=grid のスタイルを cell に適用した場合
- `Error`
  - C# 式構文エラー（`ExpressionSyntaxError`）で、`TreatExpressionSyntaxErrorAsFatal = false` の場合
- `Info`
  - 実装上必要であれば解析用情報に利用可能（必須ではない）

### 6.2 検証フェーズごとの Issue 例

1. **XML パースフェーズ**
   - 例外 `XmlException` → `XmlMalformed` + Fatal

2. **XSD スキーマ検証フェーズ**
   - スキーマ違反 → `SchemaViolation` + Fatal

3. **AST 構築後の DSL 検証フェーズ**
   - `UndefinedComponent`:
     - すべての `UseAst.ComponentName` が `WorkbookAst.Components` の `Name` に含まれているかをチェック
   - `UndefinedStyle`:
     - すべての `StyleRefAst.Name` が `StylesAst.Styles` 内の `StyleAst.Name` に含まれているかチェック
   - `RepeatChildCountInvalid`:
     - `RepeatAst.Body` が 1 ノードでない場合に発行
   - `SheetOptionsTargetNotFound`:
     - `FreezeAst.At`, `GroupRowsAst.At`, `GroupColsAst.At`, `AutoFilterAst.At` が sheet 内の `UseAst.InstanceName` または `RepeatAst.Name` 等として存在しない場合に発行
   - `FormulaRefSeriesNot1DContinuous`:
     - formulaRef によるセル系列が「縦一列」または「横一行」で連続していない場合に発行
   - `CoordinateOutOfRange`:
     - Excel のサポート行列数を超える座標に到達する可能性がある場合に発行
   - `StyleScopeViolation`:
     - scope=grid のスタイルを cell に適用した等の scope 違反時に Warning を発行

---

## 7. 式とキャッシュの扱い

### 7.1 C# 式が現れる箇所

以下の属性値に `@( ... )` 形式の C# 式が現れる。

- `cell@value`
- `use@with`
- `repeat@from`
- `Placement.when`（任意要素の `when=""` 属性）

DslParser はこれらを評価せず、**文字列として AST に保持**する。

### 7.2 DslParser の責務範囲

- 式文字列を `ValueRaw` / `WithExprRaw` / `FromExprRaw` / `WhenExprRaw` に格納する。
- ExpressionEngine が提供する「構文チェック API」が存在する場合、オプションに応じて構文チェックを行い、`ExpressionSyntaxError` Issue を生成する。
- 式のコンパイル・キャッシュ・評価は ExpressionEngine の責務とする。

---

## 8. テスト観点

### 8.1 正常系テスト

- **最小 DSL**
  - `<workbook>` に sheet が 1 つだけ存在し、styles/component なし
  - AST が構築され、Issue が 0 件であること
- **代表的フルテンプレート**
  - styles / components / sheet / repeat / sheetOptions をすべて含む DSL
  - AST 上でノード構造が想定どおりになっていること（手動確認＋ユニットテスト）
  - StyleAst の `_props` とアクセサが期待どおりの値を返すこと

### 8.2 異常系テスト（Fatal 想定）

- XML 構文エラー
  - タグ閉じ忘れ等で `XmlMalformed` が発行され、Root が null になること
- XSD スキーマ違反
  - 必須属性欠落（例: sheet の rows 欠落）、未知要素などで `SchemaViolation` が発行されること
- 未定義参照
  - 存在しない component を `use` で参照 → `UndefinedComponent`
  - 存在しない style 名を参照 → `UndefinedStyle`
- repeat 子要素不正
  - repeat 内に子要素が 0 または 2 以上 → `RepeatChildCountInvalid`
- sheetOptions.at の参照先不明
  - at で指定した name を持つ use/repeat が存在しない → `SheetOptionsTargetNotFound`
- formulaRef 系列不正
  - 非 1 次元（2D）や非連続な系列 → `FormulaRefSeriesNot1DContinuous`
- Excel 行列上限超過
  - rows/cols および Placement を組み合わせた結果、Excel の最大行・列を超えるケース → `CoordinateOutOfRange`

### 8.3 Warning 系テスト

- scope 違反
  - scope=grid の style を cell に適用
  - `StyleScopeViolation` の Warning が出ること
  - border 適用のみ無視される（LayoutEngine/Renderer 側の挙動は別テスト）

### 8.4 式関連テスト

- 正常な式
  - `@(...)` を含む DSL で、文字列が AST に正しく格納されていること
- 構文エラー式
  - 意図的に構文エラーを含む式を使用し、ExpressionEngine の構文チェックが `ExpressionSyntaxError` を発行すること
  - `TreatExpressionSyntaxErrorAsFatal` の ON/OFF 切り替えに応じて Severity が Fatal / Error になること

### 8.5 性能テスト

- 1万行規模の DSL を用意し、Parse の所要時間を計測
  - XSD 検証 ON/OFF の比較
- 大量の style / component / repeat を含む DSL でのメモリ使用量と速度を評価

---

