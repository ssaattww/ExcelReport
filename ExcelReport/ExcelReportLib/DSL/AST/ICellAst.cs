using System;
using System.Collections.Generic;
using System.Text;

namespace ExcelReportLib.DSL.AST
{
    internal interface ICellAst : IAst<ICellAst>
    {
        public IReadOnlyList<StyleRefAst> StyleRefs { get; }
    }
}
