using ExcelReportLib.Logger;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for the <c>Logger</c> feature.
/// </summary>
public sealed class LoggerTests
{
    /// <summary>
    /// Verifies that log info records entry.
    /// </summary>
    [Fact]
    public void Log_Info_RecordsEntry()
    {
        var logger = new ReportLogger();

        logger.Info("info message");

        var entry = Assert.Single(logger.GetEntries());
        Assert.Equal(LogLevel.Info, entry.Level);
        Assert.Equal("info message", entry.Message);
    }

    /// <summary>
    /// Verifies that log warning records entry.
    /// </summary>
    [Fact]
    public void Log_Warning_RecordsEntry()
    {
        var logger = new ReportLogger();

        logger.Warning("warning message");

        var entry = Assert.Single(logger.GetEntries());
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("warning message", entry.Message);
    }

    /// <summary>
    /// Verifies that log error records entry.
    /// </summary>
    [Fact]
    public void Log_Error_RecordsEntry()
    {
        var logger = new ReportLogger();

        logger.Error("error message");

        var entry = Assert.Single(logger.GetEntries());
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("error message", entry.Message);
    }

    /// <summary>
    /// Verifies that log with phase includes phase info.
    /// </summary>
    [Fact]
    public void Log_WithPhase_IncludesPhaseInfo()
    {
        var logger = new ReportLogger();

        logger.Log(LogLevel.Debug, "phase message", ReportPhase.Parsing);

        var entry = Assert.Single(logger.GetEntries());
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Equal(ReportPhase.Parsing, entry.Phase);
    }

    /// <summary>
    /// Verifies that get entries returns all entries.
    /// </summary>
    [Fact]
    public void GetEntries_ReturnsAllEntries()
    {
        var logger = new ReportLogger();

        logger.Info("info");
        logger.Warning("warning");
        logger.Error("error");

        var entries = logger.GetEntries();

        Assert.Equal(3, entries.Count);
        Assert.Equal(
            new[] { LogLevel.Info, LogLevel.Warning, LogLevel.Error },
            entries.Select(entry => entry.Level).ToArray());
    }

    /// <summary>
    /// Verifies that get entries filter by level returns filtered.
    /// </summary>
    [Fact]
    public void GetEntries_FilterByLevel_ReturnsFiltered()
    {
        var logger = new ReportLogger();

        logger.Info("info");
        logger.Warning("warning 1");
        logger.Warning("warning 2");
        logger.Error("error");

        var entries = logger.GetEntries(LogLevel.Warning);

        Assert.Equal(2, entries.Count);
        Assert.All(entries, entry => Assert.Equal(LogLevel.Warning, entry.Level));
    }

    /// <summary>
    /// Verifies that get audit trail returns chronological.
    /// </summary>
    [Fact]
    public void GetAuditTrail_ReturnsChronological()
    {
        var logger = new ReportLogger();
        var later = DateTimeOffset.UtcNow;
        var earlier = later.AddMinutes(-5);

        logger.Log(new LogEntry
        {
            Timestamp = later,
            Level = LogLevel.Info,
            Message = "later",
        });
        logger.Log(new LogEntry
        {
            Timestamp = earlier,
            Level = LogLevel.Warning,
            Message = "earlier",
        });

        var auditTrail = logger.GetAuditTrail();

        Assert.Equal(2, auditTrail.Count);
        Assert.Equal(new[] { "earlier", "later" }, auditTrail.Select(entry => entry.Message).ToArray());
    }

    /// <summary>
    /// Verifies that clear removes all entries.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var logger = new ReportLogger();

        logger.Info("info");
        logger.Warning("warning");

        logger.Clear();

        Assert.Empty(logger.GetEntries());
        Assert.Empty(logger.GetAuditTrail());
    }
}
