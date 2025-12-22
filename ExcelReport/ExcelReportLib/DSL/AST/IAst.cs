using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    internal interface IAst<TSelf> where TSelf : IAst<TSelf>
    {
        public static string TagName { get; } = default!;
        public SourceSpan? Span { get; }
    }
}
