using System;
using System.Collections.Generic;
using System.Linq;

namespace ExcelReport.SampleInput
{
    /// <summary>
    /// ExcelReport DSL のサンプル入力ルート。
    /// </summary>
    public class ReportRoot
    {
        public string JobName { get; set; } = string.Empty;
        public Summary Summary { get; set; } = new Summary();
        public List<Line> Lines { get; set; } = new List<Line>();
    }

    public class Summary
    {
        public string Owner { get; set; } = string.Empty;
        /// <summary>
        /// 成功率 (0.0 .. 1.0)
        /// </summary>
        public double SuccessRate { get; set; }
    }

    public class Line
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    public static class SampleDataFactory
    {
        public static ReportRoot Create()
        {
            var rnd = new Random(42);

            var lines = Enumerable.Range(1, 10)
                .Select(i => new Line
                {
                    Name  = $"Line-{i:00}",
                    Value = Math.Round(rnd.NextDouble() * 100, 1),
                    Code  = $"C{i:000}"
                })
                .ToList();

            return new ReportRoot
            {
                JobName = "Optimizer Run 2025-11-06 10:00",
                Summary = new Summary
                {
                    Owner = "Operator A",
                    SuccessRate = 0.873
                },
                Lines = lines
            };
        }
    }
}
