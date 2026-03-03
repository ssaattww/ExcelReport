using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace ExcelReportLib.Tests;

internal static class DslTestFixtures
{
    internal const string FullTemplateFile = "DslDefinition_FullTemplate_Sample_v1.xml";
    internal const string ExternalComponentFile = "DslDefinition_FullTemplate_SampleExternalComponent_v1.xml";
    internal const string ExternalStyleFile = "DslDefinition_FullTemplate_SampleExternalStyle_v1.xml";

    private static readonly XNamespace Namespace = "urn:excelreport:v1";
    private static readonly string ProjectDirectory = ResolveProjectDirectory();

    internal static string FixtureDirectory { get; } = Path.GetFullPath(
        Path.Combine(ProjectDirectory, "..", "ExcelReportLibTest", "TestDsl"));

    internal static string GetPath(string fileName) => Path.Combine(FixtureDirectory, fileName);

    internal static string ReadText(string fileName) => File.ReadAllText(GetPath(fileName));

    internal static XDocument LoadDocument(string fileName) => XDocument.Load(GetPath(fileName), LoadOptions.SetLineInfo);

    internal static XElement GetRequiredRootElement(string fileName) =>
        LoadDocument(fileName).Root ?? throw new InvalidOperationException($"Root element not found in fixture: {fileName}");

    internal static XElement GetRequiredChildElement(XElement parent, string localName) =>
        parent.Element(Namespace + localName)
        ?? throw new InvalidOperationException($"Child element '{localName}' was not found.");

    internal static XElement GetRequiredDescendant(
        string fileName,
        string localName,
        Func<XElement, bool>? predicate = null)
    {
        var query = LoadDocument(fileName).Descendants(Namespace + localName);
        var match = predicate is null ? query.FirstOrDefault() : query.FirstOrDefault(predicate);
        return match ?? throw new InvalidOperationException(
            $"Descendant element '{localName}' was not found in fixture: {fileName}");
    }

    private static string ResolveProjectDirectory([CallerFilePath] string callerFilePath = "") =>
        Path.GetDirectoryName(callerFilePath)
        ?? throw new InvalidOperationException("Unable to determine the test project directory.");
}
