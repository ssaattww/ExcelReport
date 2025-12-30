using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ExcelReportLib.DSL.AST
{
    public enum StyleScope
    {
        Cell,
        Grid,
        Both
    }

    /// <summary>
    /// セルの書式設定を表すASTノード
    /// </summary>
    public sealed class StyleAst : IAst<StyleAst>
    {
        public static string TagName => "style";
        public string Name { get; init; } = string.Empty;
        public StyleScope Scope { get; init; }
        public SourceSpan? Span { get; init; }

        private IReadOnlyDictionary<string, object?> _props { get; init; }

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

            var borderElem = styleElem.Element(ns + "border");
            if (borderElem != null)
            {
                var borders = new List<BorderInfo>();
                foreach (var bElem in borderElem.Elements(ns + "borders"))
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
        public string? FontName => Get<string>("font.name");
        public double? FontSize => Get<double>("font.size");
        public bool? FontBold => Get<bool>("font.bold");
        public bool? FontItalic => Get<bool>("font.italic");
        public bool? FontUnderline => Get<bool>("font.underline");

        // Fill
        public string? FillColor => Get<string>("fill.color");

        // NumberFormat
        public string? NumberFormatCode => Get<string>("numberFormat.code");

        // Border 一覧
        public IReadOnlyList<BorderInfo> Borders
            => Get<IReadOnlyList<BorderInfo>>("border") ?? Array.Empty<BorderInfo>();

        // デバッグ用途
        public IReadOnlyDictionary<string, object?> RawProperties => _props;

        private T? Get<T>(string key)
        {
            if (_props.TryGetValue(key, out var v) && v is T t)
                return t;
            return default;
        }
    }

    public sealed class BorderInfo
    {
        public string? Mode { get; init; }   // "cell" / "outer" / "all"
        public string? Top { get; init; }
        public string? Bottom { get; init; }
        public string? Left { get; init; }
        public string? Right { get; init; }
        public string? Color { get; init; }
    }
}
