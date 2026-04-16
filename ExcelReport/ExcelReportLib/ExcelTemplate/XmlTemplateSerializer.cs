using System.Text;
using System.Xml.Linq;
using ExcelReportLib.DSL;
using ExcelReportLib.ExcelTemplate.Model;

namespace ExcelReportLib.ExcelTemplate;

/// <summary>
/// Serializes the normalized ExcelTemplate output contract into DSL-compatible XML.
/// </summary>
public sealed class XmlTemplateSerializer
{
    private static readonly XNamespace DslNamespace = DslContract.NamespaceUri;

    /// <summary>
    /// Serializes the output contract to a DSL-compatible XML document.
    /// </summary>
    /// <param name="contract">The normalized output contract.</param>
    /// <returns>The serialized XML document.</returns>
    public XDocument Serialize(ExcelTemplateOutputContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var workbook = new XElement(DslNamespace + "workbook");

        foreach (var issue in contract.Issues)
        {
            workbook.Add(new XComment(SanitizeComment($"issue severity={issue.Severity} kind={issue.Kind} message={issue.Message}")));
        }

        foreach (var component in contract.Components)
        {
            workbook.Add(new XComment(SanitizeComment(BuildComponentDebugComment(component))));
            if (!component.IsRangeResolved)
            {
                continue;
            }

            workbook.Add(CreateComponentElement(component));
        }

        foreach (var sheet in contract.Sheets)
        {
            workbook.Add(CreateSheetElement(sheet));
        }

        return new XDocument(new XDeclaration("1.0", "utf-8", null), workbook);
    }

    private static XElement CreateComponentElement(ExcelTemplateOutputComponent component)
    {
        var element = new XElement(
            DslNamespace + "component",
            new XAttribute("name", component.Name));

        element.Add(CreateGridElement(component));
        return element;
    }

    private static XElement CreateGridElement(ExcelTemplateOutputComponent component)
    {
        var grid = new XElement(DslNamespace + "grid");
        if (component.IsRangeResolved &&
            TryParseDimensions(component.RangeReference, out var rows, out var cols))
        {
            grid.SetAttributeValue("rows", rows);
            grid.SetAttributeValue("cols", cols);
        }

        foreach (var item in component.Items)
        {
            grid.Add(CreateLayoutElement(item, includePlacement: true));
        }

        return grid;
    }

    private static XElement CreateSheetElement(ExcelTemplateOutputSheet sheet)
    {
        var element = new XElement(
            DslNamespace + "sheet",
            new XAttribute("name", sheet.Name));
        if (!string.IsNullOrWhiteSpace(sheet.FromExpression))
        {
            element.SetAttributeValue("from", sheet.FromExpression);
        }

        if (!string.IsNullOrWhiteSpace(sheet.VariableName))
        {
            element.SetAttributeValue("var", sheet.VariableName);
        }

        foreach (var item in sheet.Items)
        {
            element.Add(CreateLayoutElement(item, includePlacement: true));
        }

        return element;
    }

    private static XElement CreateLayoutElement(ExcelTemplateOutputItem item, bool includePlacement)
    {
        return item switch
        {
            ExcelTemplateOutputCell cell => CreateCellElement(cell, includePlacement),
            ExcelTemplateOutputUse use => CreateUseElement(use, includePlacement),
            ExcelTemplateOutputRepeatUse repeatUse => CreateRepeatElement(repeatUse, includePlacement),
            _ => throw new InvalidOperationException($"Unsupported output item type: {item.GetType().FullName}"),
        };
    }

    private static XElement CreateCellElement(ExcelTemplateOutputCell cell, bool includePlacement)
    {
        var element = new XElement(DslNamespace + "cell");
        AddPlacementAttributes(element, cell, includePlacement);

        if (cell.Formula is not null)
        {
            element.SetAttributeValue("formula", cell.Formula);
        }
        else if (cell.Value is not null)
        {
            element.SetAttributeValue("value", cell.Value);
        }

        return element;
    }

    private static XElement CreateUseElement(ExcelTemplateOutputUse use, bool includePlacement)
    {
        var element = new XElement(
            DslNamespace + "use",
            new XAttribute("component", use.ComponentName));
        AddPlacementAttributes(element, use, includePlacement);

        if (!string.IsNullOrWhiteSpace(use.StyleOverflow))
        {
            element.SetAttributeValue("styleOverflow", use.StyleOverflow);
        }

        return element;
    }

    private static XElement CreateRepeatElement(ExcelTemplateOutputRepeatUse repeatUse, bool includePlacement)
    {
        var element = new XElement(
            DslNamespace + "repeat",
            new XAttribute("from", repeatUse.FromExpression),
            new XAttribute("var", repeatUse.VariableName),
            new XAttribute("direction", repeatUse.Direction));
        AddPlacementAttributes(element, repeatUse, includePlacement);
        element.Add(CreateUseElement(
            new ExcelTemplateOutputUse(
                repeatUse.Reference,
                repeatUse.Row,
                repeatUse.Column,
                repeatUse.StyleIndex,
                repeatUse.ComponentName,
                repeatUse.StyleOverflow),
            includePlacement: false));
        return element;
    }

    private static void AddPlacementAttributes(XElement element, ExcelTemplateOutputItem item, bool includePlacement)
    {
        if (!includePlacement)
        {
            return;
        }

        element.SetAttributeValue("r", item.Row);
        element.SetAttributeValue("c", item.Column);
    }

    private static string BuildComponentDebugComment(ExcelTemplateOutputComponent component)
    {
        var builder = new StringBuilder();
        builder.Append("component");
        builder.Append(" name=").Append(component.Name);
        builder.Append(" sourceSheet=").Append(component.SourceSheetName);
        builder.Append(" rangeResolved=").Append(component.IsRangeResolved ? "true" : "false");

        if (!string.IsNullOrWhiteSpace(component.RangeReference))
        {
            builder.Append(" range=").Append(component.RangeReference);
        }

        if (!component.IsRangeResolved)
        {
            builder.Append(" unresolved-component");
        }

        return builder.ToString();
    }

    private static bool TryParseDimensions(string? rangeReference, out int rows, out int cols)
    {
        rows = 0;
        cols = 0;

        if (string.IsNullOrWhiteSpace(rangeReference) ||
            !TryParseRange(rangeReference, out var topRow, out var leftColumn, out var bottomRow, out var rightColumn))
        {
            return false;
        }

        rows = bottomRow - topRow + 1;
        cols = rightColumn - leftColumn + 1;
        return rows > 0 && cols > 0;
    }

    private static bool TryParseRange(
        string reference,
        out int topRow,
        out int leftColumn,
        out int bottomRow,
        out int rightColumn)
    {
        topRow = 0;
        leftColumn = 0;
        bottomRow = 0;
        rightColumn = 0;

        var parts = reference.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2 || !TryParseCellReference(parts[0], out topRow, out leftColumn))
        {
            return false;
        }

        if (parts.Length == 1)
        {
            bottomRow = topRow;
            rightColumn = leftColumn;
            return true;
        }

        if (!TryParseCellReference(parts[1], out bottomRow, out rightColumn))
        {
            return false;
        }

        return bottomRow >= topRow && rightColumn >= leftColumn;
    }

    private static bool TryParseCellReference(string token, out int row, out int column)
    {
        row = 0;
        column = 0;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var trimmed = token.Trim().Replace("$", string.Empty, StringComparison.Ordinal);
        var letters = new StringBuilder();
        var digits = new StringBuilder();

        foreach (var character in trimmed)
        {
            if (char.IsLetter(character))
            {
                if (digits.Length > 0)
                {
                    return false;
                }

                letters.Append(char.ToUpperInvariant(character));
                continue;
            }

            if (!char.IsDigit(character))
            {
                return false;
            }

            digits.Append(character);
        }

        if (letters.Length == 0 || digits.Length == 0 || !int.TryParse(digits.ToString(), out row))
        {
            return false;
        }

        foreach (var character in letters.ToString())
        {
            column = checked((column * 26) + (character - 'A' + 1));
        }

        return column > 0;
    }

    private static string SanitizeComment(string value)
    {
        var sanitized = value.Replace("--", "- -", StringComparison.Ordinal);
        if (sanitized.EndsWith("-", StringComparison.Ordinal))
        {
            sanitized += " ";
        }

        return sanitized;
    }
}
