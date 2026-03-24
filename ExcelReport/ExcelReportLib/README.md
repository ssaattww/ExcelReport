# ExcelReportLib

ExcelReportLib is a .NET library that generates `.xlsx` files from an XML DSL template and runtime data.

## Install

```bash
dotnet add package ExcelReportLib
```

## Quick Example

```csharp
using ExcelReportLib;
using ExcelReportLib.Renderer;

var generator = new ReportGenerator();
var result = generator.Generate(
    templateXmlText,
    data,
    new ReportGeneratorOptions
    {
        EnableSchemaValidation = false,
        RenderOptions = new RenderOptions
        {
            TemplateName = "Sample",
            DataSource = "demo",
            GeneratedAt = DateTimeOffset.Now,
        }
    });

if (result.Output is not null)
{
    using var file = File.Create("report.xlsx");
    result.Output.Position = 0;
    result.Output.CopyTo(file);
}
```

## Docs

- Repository: https://github.com/ssaattww/ExcelReport
- Design docs: https://github.com/ssaattww/ExcelReport/tree/master/Design
- Usage samples: https://github.com/ssaattww/ExcelReport/tree/master/ExcelReport/ExcelReportExe

