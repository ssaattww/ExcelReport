using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    /// <summary>
    /// Specifies style scope values.
    /// </summary>
    public enum StyleScope
    {
        /// <summary>
        /// Represents the cell option.
        /// </summary>
        Cell,
        /// <summary>
        /// Represents the grid option.
        /// </summary>
        Grid,
        /// <summary>
        /// Represents both.
        /// </summary>
        Both
    }

    /// <summary>
    /// セルの書式設定を表すASTノード
    /// </summary>
    public sealed class StyleAst : IAst<StyleAst>
    {
        /// <summary>
        /// Gets the DSL element tag name.
        /// </summary>
        public static string TagName => "style";
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the scope.
        /// </summary>
        public StyleScope Scope { get; init; }
        /// <summary>
        /// Gets or sets the span.
        /// </summary>
        public SourceSpan? Span { get; init; }

        private IReadOnlyDictionary<string, object?> _props { get; init; }

        /// <summary>
        /// Initializes a new instance of the style ast type.
        /// </summary>
        /// <param name="styleElem">The source XML element.</param>
        /// <param name="issues">The collection used to collect discovered issues.</param>
        public StyleAst(XElement styleElem, List<Issue> issues)
        {
            // <style> 要素から StyleAst を構築する。
            var nameAttr = styleElem.Attribute("name");
            string name = nameAttr?.Value ?? string.Empty;

            var scopeValue = (string?)styleElem.Attribute("scope");
            var scope = scopeValue switch
            {
                "cell" => StyleScope.Cell,
                "grid" => StyleScope.Grid,
                _ => StyleScope.Both,
            };

            var props = new Dictionary<string, object?>();
            var ns = styleElem.Name.Namespace;

            var fontElem = styleElem.Element(ns + "font");
            if (fontElem != null)
            {
                var fontNameAttr = fontElem.Attribute("name");
                if (fontNameAttr != null)
                    props["font.name"] = fontNameAttr.Value;
                var fontSizeAttr = fontElem.Attribute("size");
                if (fontSizeAttr != null && double.TryParse(fontSizeAttr.Value, out var size))
                    props["font.size"] = size;
                var fontBoldAttr = fontElem.Attribute("bold");
                if (fontBoldAttr != null && bool.TryParse(fontBoldAttr.Value, out var bold))
                    props["font.bold"] = bold;
                var fontItalicAttr = fontElem.Attribute("italic");
                if (fontItalicAttr != null && bool.TryParse(fontItalicAttr.Value, out var italic))
                    props["font.italic"] = italic;
                var fontUnderlineAttr = fontElem.Attribute("underline");
                if (fontUnderlineAttr != null && bool.TryParse(fontUnderlineAttr.Value, out var underline))
                    props["font.underline"] = underline;
            }

            var fillElem = styleElem.Element(ns + "fill");
            if (fillElem != null)
            {
                var fillColorAttr = fillElem.Attribute("color");
                if (fillColorAttr != null)
                    props["fill.color"] = fillColorAttr.Value;
            }

            var numberFormatElem = styleElem.Element(ns + "numberFormat");
            if (numberFormatElem != null)
            {
                var codeAttr = numberFormatElem.Attribute("code");
                if (codeAttr != null)
                    props["numberFormat.code"] = codeAttr.Value;
            }

            var borderElems = styleElem.Elements(ns + "border").ToList();
            if (borderElems.Count > 0)
            {
                var borders = new List<BorderInfo>();
                foreach (var bElem in borderElems)
                {
                    var borderInfo = new BorderInfo
                    {
                        Mode = (string?)bElem.Attribute("mode"),
                        Top = (string?)bElem.Attribute("top"),
                        Bottom = (string?)bElem.Attribute("bottom"),
                        Left = (string?)bElem.Attribute("left"),
                        Right = (string?)bElem.Attribute("right"),
                        Color = (string?)bElem.Attribute("color"),
                    };
                    borders.Add(borderInfo);
                }
                props["border"] = borders;
            }

            Name = name;
            Scope = scope;
            _props = props;
            Span = SourceSpan.CreateSpanAttributes(styleElem);
        }

        // Font 系アクセサ
        /// <summary>
        /// Gets the font name.
        /// </summary>
        public string? FontName => Get<string>("font.name");
        /// <summary>
        /// Gets the font size.
        /// </summary>
        public double? FontSize => Get<double>("font.size");
        /// <summary>
        /// Gets a value indicating whether font bold.
        /// </summary>
        public bool? FontBold => Get<bool>("font.bold");
        /// <summary>
        /// Gets a value indicating whether font italic.
        /// </summary>
        public bool? FontItalic => Get<bool>("font.italic");
        /// <summary>
        /// Gets a value indicating whether font underline.
        /// </summary>
        public bool? FontUnderline => Get<bool>("font.underline");

        // Fill
        /// <summary>
        /// Gets the fill color.
        /// </summary>
        public string? FillColor => Get<string>("fill.color");

        // NumberFormat
        /// <summary>
        /// Gets the number format code.
        /// </summary>
        public string? NumberFormatCode => Get<string>("numberFormat.code");

        // Border 一覧
        /// <summary>
        /// Represents borders.
        /// </summary>
        public IReadOnlyList<BorderInfo> Borders
            => Get<IReadOnlyList<BorderInfo>>("border") ?? Array.Empty<BorderInfo>();

        // デバッグ用途
        /// <summary>
        /// Gets the raw properties.
        /// </summary>
        public IReadOnlyDictionary<string, object?> RawProperties => _props;

        private T? Get<T>(string key)
        {
            if (_props.TryGetValue(key, out var v) && v is T t)
                return t;
            return default;
        }
    }

    /// <summary>
    /// Represents border info.
    /// </summary>
    public sealed class BorderInfo
    {
        /// <summary>
        /// Gets or sets the mode.
        /// </summary>
        public string? Mode { get; init; }   // "cell" / "outer" / "all"
        /// <summary>
        /// Gets or sets the top.
        /// </summary>
        public string? Top { get; init; }
        /// <summary>
        /// Gets or sets the bottom.
        /// </summary>
        public string? Bottom { get; init; }
        /// <summary>
        /// Gets or sets the left.
        /// </summary>
        public string? Left { get; init; }
        /// <summary>
        /// Gets or sets the right.
        /// </summary>
        public string? Right { get; init; }
        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        public string? Color { get; init; }
    }
}
