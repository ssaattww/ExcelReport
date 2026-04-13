using ExcelReportLib.ExcelTemplate;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides snapshot tests for ExcelTemplate conversion outputs.
/// </summary>
public sealed class ExcelTemplateSnapshotTests
{
    private const string XmlSnapshotFile = "Issue58_StandardTemplate_Debug.xml";
    private const string DslSnapshotFile = "Issue58_StandardTemplate_Dsl.xml";

    /// <summary>
    /// Verifies that serializer output matches the checked-in debug XML snapshot.
    /// </summary>
    [Fact]
    public void Serialize_StandardWorkbook_MatchesXmlSnapshot()
    {
        var builder = new ExcelTemplateOutputContractBuilder();
        var contract = builder.Build(ExcelTemplateOutputContractFixture.CreateStandardWorkbook());
        var serializer = new XmlTemplateSerializer();

        var actual = NormalizeLineEndings(serializer.Serialize(contract).ToString());
        var expected = NormalizeLineEndings(DslTestFixtures.ReadText(XmlSnapshotFile));

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Verifies that DSL emitter output matches the checked-in DSL snapshot.
    /// </summary>
    [Fact]
    public void Emit_StandardWorkbook_MatchesDslSnapshot()
    {
        var builder = new ExcelTemplateOutputContractBuilder();
        var contract = builder.Build(ExcelTemplateOutputContractFixture.CreateStandardWorkbook());
        var emitter = new DslEmitter();

        var actual = NormalizeLineEndings(emitter.Emit(contract));
        var expected = NormalizeLineEndings(DslTestFixtures.ReadText(DslSnapshotFile));

        Assert.Equal(expected, actual);
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
}
