using ExcelReportLib.Logger;

namespace ExcelReportLib.Tests;

public sealed class LoggerTests
{
    [Fact]
    public void Log_Info_RecordsEntry()
    {
        var logger = new ReportLogger();

        logger.Info("info message");

        var entry = Assert.Single(logger.GetEntries());
        Assert.Equal(LogLevel.Info, entry.Level);
        Assert.Equal("info message", entry.Message);
    }

    [Fact]
    public void Log_Warning_RecordsEntry()
    {
        var logger = new ReportLogger();

        logger.Warning("warning message");

        var entry = Assert.Single(logger.GetEntries());
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("warning message", entry.Message);
    }

    [Fact]
    public void Log_Error_RecordsEntry()
    {
        var logger = new ReportLogger();

        logger.Error("error message");

        var entry = Assert.Single(logger.GetEntries());
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("error message", entry.Message);
    }

    [Fact]
    public void Log_WithPhase_IncludesPhaseInfo()
    {
        var logger = new ReportLogger();

        logger.Log(LogLevel.Debug, "phase message", ReportPhase.Parsing);

        var entry = Assert.Single(logger.GetEntries());
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Equal(ReportPhase.Parsing, entry.Phase);
    }

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
