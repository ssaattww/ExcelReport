using System.Text;
using System.Xml;
using ExcelReportLib.ExcelTemplate.Model;

namespace ExcelReportLib.ExcelTemplate;

/// <summary>
/// Emits DSL text from the normalized ExcelTemplate output contract.
/// </summary>
public sealed class DslEmitter
{
    private readonly XmlTemplateSerializer xmlTemplateSerializer = new();

    /// <summary>
    /// Emits DSL-compatible XML text from the output contract.
    /// </summary>
    /// <param name="contract">The normalized output contract.</param>
    /// <returns>The DSL text.</returns>
    public string Emit(ExcelTemplateOutputContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var document = xmlTemplateSerializer.Serialize(contract);
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            OmitXmlDeclaration = false,
            NewLineChars = Environment.NewLine,
            NewLineHandling = NewLineHandling.Replace,
        };

        using var stringWriter = new Utf8StringWriter();
        using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
        {
            document.Save(xmlWriter);
        }

        return stringWriter.ToString();
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }
}
