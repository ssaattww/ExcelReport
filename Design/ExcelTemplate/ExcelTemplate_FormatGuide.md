# ExcelTemplate Format Guide (with Examples)

This guide explains how to author **ExcelTemplate** workbooks (`.xlsx`) that can be converted into the ExcelReport DSL and rendered to final reports.

## 1. What ExcelTemplate Is

ExcelTemplate is an authoring style where you design report structure in Excel and let `ExcelTemplateConverter` translate it into DSL (`urn:excelreport:v2`).

Conversion flow:

1. Read workbook (`.xlsx`)
2. Extract sheets, cell values/formulas, component boundaries, and optional workbook metadata
3. Convert to DSL
4. Render final workbook with `ReportGenerator`

## 2. Workbook Structure Conventions

### 2.1 Normal Sheets

- Any sheet that does **not** start with `__component_` is treated as a normal output sheet template.

### 2.2 Component Sheets

- Component sheets must be named: `__component_<ComponentName>`
- Example: `__component_ItemRow`
- Use these sheets via `{{use:ItemRow}}` triggers from other sheets.

### 2.3 Optional Meta Sheet for Sheet Repeat

- Use sheet name: `__sheet_meta`
- Place one shape with fixed name: `__workbook_meta`
- Put workbook-level XML in the shape text (see section 4).

## 3. Cell-Level Syntax

### 3.1 Plain Values and Formulas

- A regular cell value is copied as a DSL `cell@value`.
- A formula cell is copied as a DSL `cell@formula`.

### 3.2 Expression Shorthand

- `@foo` -> normalized to `@(root.Foo)`
- `@item.Name` -> normalized to `@(item.Name)`

### 3.3 Use Trigger Syntax

Use this form in a cell text:

```text
{{use:ComponentName}}
```

Repeat-use form:

```text
{{use:ComponentName, from:@items, var:item}}
```

With style overflow option:

```text
{{use:ComponentName, from:@items, var:item, styleOverflow:edge}}
```

Notes:

- `from` and `var` must be specified together in use-trigger repeat.
- Repeat direction is normalized to `down`.

## 4. Sheet Repeat via `__workbook_meta` Shape

Inside `__sheet_meta` -> shape `__workbook_meta`, place XML like this:

```xml
<workbook>
  <sheets>
    <sheet templateSheet="InvoiceTemplate"
           name="@(grp.Name)"
           from="@(root.Groups)"
           var="grp" />
  </sheets>
</workbook>
```

Rules:

- `templateSheet` is required.
- `name` is required.
- `from` is optional.
- `var` is optional.
- If `var` is specified, `from` is required.
- If `from` is specified and `var` is omitted, runtime uses the default variable name `item`.

## 5. End-to-End Example

### 5.1 Workbook layout

- `__component_Header`
- `__component_ItemRow`
- `InvoiceTemplate`
- `__sheet_meta` (contains shape `__workbook_meta`)

### 5.2 Example trigger cells

In `InvoiceTemplate`:

- `A1 = @grp.Name`
- `A3 = {{use:Header}}`
- `A5 = {{use:ItemRow, from:@grp.Items, var:item, styleOverflow:edge}}`

### 5.3 Meta shape XML

```xml
<workbook>
  <sheets>
    <sheet templateSheet="InvoiceTemplate"
           name="@(grp.Name)"
           from="@(root.Groups)"
           var="grp" />
  </sheets>
</workbook>
```

### 5.4 Input data (C#)

```csharp
var data = new
{
    Groups = new[]
    {
        new
        {
            Name = "North",
            Items = new[]
            {
                new { Name = "Mouse", Qty = 2, Price = 1200 },
                new { Name = "Keyboard", Qty = 1, Price = 980 }
            }
        },
        new
        {
            Name = "South",
            Items = new[]
            {
                new { Name = "Display", Qty = 1, Price = 24500 }
            }
        }
    }
};
```

Expected result:

- Sheet `North` generated from `InvoiceTemplate`
- Sheet `South` generated from `InvoiceTemplate`

## 6. Conversion APIs

### 6.1 Convert only (`xlsx` -> DSL text)

```csharp
var converter = new ExcelTemplateConverter();
var conversion = converter.ConvertToDsl("template.xlsx");

Console.WriteLine(conversion.Text);
foreach (var issue in conversion.Issues)
{
    Console.WriteLine($"[{issue.Severity}] {issue.Kind}: {issue.Message}");
}
```

### 6.2 Generate final workbook (`xlsx` -> DSL -> output xlsx)

```csharp
var generator = new ExcelTemplateReportGenerator();
var result = generator.GenerateFromExcelTemplate("template.xlsx", data);

if (result.Succeeded)
{
    File.WriteAllBytes("report.xlsx", result.Output!.ToArray());
}
```

## 7. Common Validation Errors

- `sheet '__sheet_meta' is missing fixed shape '__workbook_meta'`
- Meta XML root is not `<workbook>`
- `workbook/sheets/sheet` is missing
- `templateSheet` does not exist in workbook
- Duplicate `templateSheet` definitions
- `var` specified without `from`

## 8. Recommended Authoring Tips

- Keep one responsibility per component sheet (`__component_*`).
- Keep `__workbook_meta` focused on workbook-level concerns (such as sheet repeat).
- Prefer explicit expressions (`@(root.X)`) for readability when templates grow.
- Validate generated DSL in CI via unit/integration tests.
