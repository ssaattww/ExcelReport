using System;
using System.Collections.Generic;
using System.Text;

namespace ExcelReportLib.DSL.AST
{
    internal interface ICellAst : IAst<ICellAst>
    {
        /// <summary>
        /// スタイル参照
        /// </summary>
        public IReadOnlyList<StyleRefAst> StyleRefs { get; }
        
        /// <summary>
        /// 直接定義されたスタイル
        /// </summary>
        public IReadOnlyList<StyleAst> Style{ get; }

        public Placement Placement { get; }
    }
}
