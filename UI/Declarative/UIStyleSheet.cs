using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace CommandFramework.UI.Declarative
{
    /// <summary>
    /// Parsed CSS Style Rule containing visual and layout properties.
    /// </summary>
    public class UIStyleRule
    {
        public string Selector { get; set; }

        // Layout properties
        public float? Width { get; set; }
        public float? Height { get; set; }
        public float? MinWidth { get; set; }
        public float? MinHeight { get; set; }
        public float? MaxWidth { get; set; }
        public float? MaxHeight { get; set; }

        public RectOffset Padding { get; set; } = new RectOffset(0, 0, 0, 0);
        public RectOffset Margin { get; set; } = new RectOffset(0, 0, 0, 0);
        public float Gap { get; set; } = 0f;

        public string Display { get; set; } = "flex"; // flex, block, inline
        public string FlexDirection { get; set; } = "column"; // row, column
        public string JustifyContent { get; set; } = "flex-start"; // flex-start, center, flex-end, space-between
        public string AlignItems { get; set; } = "stretch"; // flex-start, center, flex-end, stretch

        // Visual properties
        public Color? BackgroundColor { get; set; }
        public Color? BorderColor { get; set; }
        public float BorderWidth { get; set; } = 0f;
        public float BorderRadius { get; set; } = 0f;

        // Typography
        public Color? Color { get; set; }
        public int? FontSize { get; set; }
        public FontStyle FontStyle { get; set; } = FontStyle.Normal;
        public TextAnchor TextAlignment { get; set; } = TextAnchor.MiddleLeft;

        // Pseudo-classes
        public UIStyleRule HoverStyle { get; set; }
        public UIStyleRule ActiveStyle { get; set; }
        public UIStyleRule DisabledStyle { get; set; }

        public UIStyleRule Clone()
        {
            return new UIStyleRule
            {
                Selector = Selector,
                Width = Width,
                Height = Height,
                MinWidth = MinWidth,
                MinHeight = MinHeight,
                MaxWidth = MaxWidth,
                MaxHeight = MaxHeight,
                Padding = new RectOffset(Padding.left, Padding.right, Padding.top, Padding.bottom),
                Margin = new RectOffset(Margin.left, Margin.right, Margin.top, Margin.bottom),
                Gap = Gap,
                Display = Display,
                FlexDirection = FlexDirection,
                JustifyContent = JustifyContent,
                AlignItems = AlignItems,
                BackgroundColor = BackgroundColor,
                BorderColor = BorderColor,
                BorderWidth = BorderWidth,
                BorderRadius = BorderRadius,
                Color = Color,
                FontSize = FontSize,
                FontStyle = FontStyle,
                TextAlignment = TextAlignment
            };
        }
    }

    /// <summary>
    /// CSS Parser and style cascade resolver for Command Framework Declarative UI.
    /// </summary>
    public class UIStyleSheet
    {
        private readonly Dictionary<string, UIStyleRule> _rules = new Dictionary<string, UIStyleRule>(StringComparer.OrdinalIgnoreCase);

        public static UIStyleSheet Parse(string cssText)
        {
            var sheet = new UIStyleSheet();
            if (string.IsNullOrEmpty(cssText)) return sheet;

            // Strip comments
            cssText = System.Text.RegularExpressions.Regex.Replace(cssText, @"/\*.*?\*/", string.Empty, System.Text.RegularExpressions.RegexOptions.Singleline);

            // Split into rule blocks
            var blocks = cssText.Split(new[] { '}' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var block in blocks)
            {
                int braceIndex = block.IndexOf('{');
                if (braceIndex < 0) continue;

                string selectorList = block.Substring(0, braceIndex).Trim();
                string declarations = block.Substring(braceIndex + 1).Trim();

                var selectors = selectorList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var sel in selectors)
                {
                    string trimmedSel = sel.Trim();
                    if (string.IsNullOrEmpty(trimmedSel)) continue;

                    var rule = sheet.GetOrCreateRule(trimmedSel);
                    sheet.ParseDeclarations(rule, declarations);
                }
            }

            return sheet;
        }

        public UIStyleRule GetOrCreateRule(string selector)
        {
            if (selector.EndsWith(":hover", StringComparison.OrdinalIgnoreCase))
            {
                string baseSel = selector.Substring(0, selector.Length - 6).Trim();
                var baseRule = GetOrCreateRule(baseSel);
                if (baseRule.HoverStyle == null) baseRule.HoverStyle = new UIStyleRule { Selector = selector };
                return baseRule.HoverStyle;
            }

            if (selector.EndsWith(":active", StringComparison.OrdinalIgnoreCase))
            {
                string baseSel = selector.Substring(0, selector.Length - 7).Trim();
                var baseRule = GetOrCreateRule(baseSel);
                if (baseRule.ActiveStyle == null) baseRule.ActiveStyle = new UIStyleRule { Selector = selector };
                return baseRule.ActiveStyle;
            }

            if (!_rules.TryGetValue(selector, out var rule))
            {
                rule = new UIStyleRule { Selector = selector };
                _rules[selector] = rule;
            }
            return rule;
        }

        public UIStyleRule ResolveStyle(string tagName, string className, string id, bool isHovered = false, bool isActive = false)
        {
            var resolved = new UIStyleRule();

            // 1. Tag rule (e.g. "button", "div")
            if (!string.IsNullOrEmpty(tagName) && _rules.TryGetValue(tagName, out var tagRule))
            {
                MergeRule(resolved, tagRule);
                if (isHovered && tagRule.HoverStyle != null) MergeRule(resolved, tagRule.HoverStyle);
                if (isActive && tagRule.ActiveStyle != null) MergeRule(resolved, tagRule.ActiveStyle);
            }

            // 2. Class rules (e.g. ".context-btn", ".primary")
            if (!string.IsNullOrEmpty(className))
            {
                var classes = className.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var cls in classes)
                {
                    string clsSelector = "." + cls;
                    if (_rules.TryGetValue(clsSelector, out var classRule))
                    {
                        MergeRule(resolved, classRule);
                        if (isHovered && classRule.HoverStyle != null) MergeRule(resolved, classRule.HoverStyle);
                        if (isActive && classRule.ActiveStyle != null) MergeRule(resolved, classRule.ActiveStyle);
                    }
                }
            }

            // 3. ID rule (e.g. "#main-menu")
            if (!string.IsNullOrEmpty(id))
            {
                string idSelector = "#" + id;
                if (_rules.TryGetValue(idSelector, out var idRule))
                {
                    MergeRule(resolved, idRule);
                    if (isHovered && idRule.HoverStyle != null) MergeRule(resolved, idRule.HoverStyle);
                    if (isActive && idRule.ActiveStyle != null) MergeRule(resolved, idRule.ActiveStyle);
                }
            }

            return resolved;
        }

        private void MergeRule(UIStyleRule target, UIStyleRule source)
        {
            if (source == null) return;

            if (source.Width.HasValue) target.Width = source.Width;
            if (source.Height.HasValue) target.Height = source.Height;
            if (source.MinWidth.HasValue) target.MinWidth = source.MinWidth;
            if (source.MinHeight.HasValue) target.MinHeight = source.MinHeight;
            if (source.MaxWidth.HasValue) target.MaxWidth = source.MaxWidth;
            if (source.MaxHeight.HasValue) target.MaxHeight = source.MaxHeight;

            if (source.Padding != null) target.Padding = source.Padding;
            if (source.Margin != null) target.Margin = source.Margin;
            if (source.Gap > 0f) target.Gap = source.Gap;

            if (!string.IsNullOrEmpty(source.Display)) target.Display = source.Display;
            if (!string.IsNullOrEmpty(source.FlexDirection)) target.FlexDirection = source.FlexDirection;
            if (!string.IsNullOrEmpty(source.JustifyContent)) target.JustifyContent = source.JustifyContent;
            if (!string.IsNullOrEmpty(source.AlignItems)) target.AlignItems = source.AlignItems;

            if (source.BackgroundColor.HasValue) target.BackgroundColor = source.BackgroundColor;
            if (source.BorderColor.HasValue) target.BorderColor = source.BorderColor;
            if (source.BorderWidth > 0f) target.BorderWidth = source.BorderWidth;
            if (source.BorderRadius > 0f) target.BorderRadius = source.BorderRadius;

            if (source.Color.HasValue) target.Color = source.Color;
            if (source.FontSize.HasValue) target.FontSize = source.FontSize;
            if (source.FontStyle != FontStyle.Normal) target.FontStyle = source.FontStyle;
            if (source.TextAlignment != TextAnchor.MiddleLeft) target.TextAlignment = source.TextAlignment;
        }

        private void ParseDeclarations(UIStyleRule rule, string declarations)
        {
            var props = declarations.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var prop in props)
            {
                int colon = prop.IndexOf(':');
                if (colon < 0) continue;

                string key = prop.Substring(0, colon).Trim().ToLowerInvariant();
                string val = prop.Substring(colon + 1).Trim();

                switch (key)
                {
                    case "width":
                        if (TryParsePixel(val, out float w)) rule.Width = w;
                        break;
                    case "height":
                        if (TryParsePixel(val, out float h)) rule.Height = h;
                        break;
                    case "min-width":
                        if (TryParsePixel(val, out float minW)) rule.MinWidth = minW;
                        break;
                    case "min-height":
                        if (TryParsePixel(val, out float minH)) rule.MinHeight = minH;
                        break;
                    case "max-width":
                        if (TryParsePixel(val, out float maxW)) rule.MaxWidth = maxW;
                        break;
                    case "max-height":
                        if (TryParsePixel(val, out float maxH)) rule.MaxHeight = maxH;
                        break;

                    case "padding":
                        rule.Padding = ParseRectOffset(val);
                        break;
                    case "margin":
                        rule.Margin = ParseRectOffset(val);
                        break;
                    case "gap":
                        if (TryParsePixel(val, out float gap)) rule.Gap = gap;
                        break;

                    case "display":
                        rule.Display = val.ToLowerInvariant();
                        break;
                    case "flex-direction":
                        rule.FlexDirection = val.ToLowerInvariant();
                        break;
                    case "justify-content":
                        rule.JustifyContent = val.ToLowerInvariant();
                        break;
                    case "align-items":
                        rule.AlignItems = val.ToLowerInvariant();
                        break;

                    case "background":
                    case "background-color":
                        rule.BackgroundColor = ParseColor(val);
                        break;
                    case "border-color":
                        rule.BorderColor = ParseColor(val);
                        break;
                    case "border":
                        ParseBorderShorthand(rule, val);
                        break;
                    case "border-width":
                        if (TryParsePixel(val, out float bw)) rule.BorderWidth = bw;
                        break;
                    case "border-radius":
                        if (TryParsePixel(val, out float br)) rule.BorderRadius = br;
                        break;

                    case "color":
                        rule.Color = ParseColor(val);
                        break;
                    case "font-size":
                        if (TryParsePixel(val, out float fs)) rule.FontSize = (int)fs;
                        break;
                    case "font-weight":
                        if (val.IndexOf("bold", StringComparison.OrdinalIgnoreCase) >= 0 || val == "700" || val == "800" || val == "900")
                            rule.FontStyle = FontStyle.Bold;
                        break;
                    case "text-align":
                        rule.TextAlignment = ParseTextAlignment(val);
                        break;
                }
            }
        }

        private static bool TryParsePixel(string val, out float result)
        {
            val = val.Replace("px", "").Trim();
            return float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private static RectOffset ParseRectOffset(string val)
        {
            var parts = val.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1 && TryParsePixel(parts[0], out float all))
            {
                int v = (int)all;
                return new RectOffset(v, v, v, v);
            }
            if (parts.Length == 2 && TryParsePixel(parts[0], out float tb) && TryParsePixel(parts[1], out float lr))
            {
                return new RectOffset((int)lr, (int)lr, (int)tb, (int)tb);
            }
            if (parts.Length == 4 && TryParsePixel(parts[0], out float top) && TryParsePixel(parts[1], out float right) &&
                TryParsePixel(parts[2], out float bottom) && TryParsePixel(parts[3], out float left))
            {
                return new RectOffset((int)left, (int)right, (int)top, (int)bottom);
            }
            return new RectOffset(0, 0, 0, 0);
        }

        private static void ParseBorderShorthand(UIStyleRule rule, string val)
        {
            // e.g. "1px solid rgba(15, 224, 120, 0.35)" or "1px solid #0fe078"
            var parts = val.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 1 && TryParsePixel(parts[0], out float bw))
            {
                rule.BorderWidth = bw;
            }
            if (parts.Length >= 3)
            {
                string colorStr = string.Join(" ", parts, 2, parts.Length - 2);
                rule.BorderColor = ParseColor(colorStr);
            }
        }

        public static Color ParseColor(string val)
        {
            val = val.Trim();

            // Named tokens
            if (val.Equals("var(--tactical-green)", StringComparison.OrdinalIgnoreCase)) return new Color(0.06f, 0.88f, 0.50f, 1f);
            if (val.Equals("var(--friendly-blue)", StringComparison.OrdinalIgnoreCase)) return new Color(0.0f, 0.82f, 1.0f, 1f);
            if (val.Equals("var(--amber-warn)", StringComparison.OrdinalIgnoreCase)) return new Color(1.0f, 0.70f, 0.20f, 1f);
            if (val.Equals("var(--hostile-red)", StringComparison.OrdinalIgnoreCase)) return new Color(1.0f, 0.20f, 0.27f, 1f);

            // Hex (#RRGGBB or #RRGGBBAA)
            if (val.StartsWith("#"))
            {
                if (ColorUtility.TryParseHtmlString(val, out var hexCol)) return hexCol;
            }

            // rgba(r, g, b, a) or rgb(r, g, b)
            if (val.StartsWith("rgba", StringComparison.OrdinalIgnoreCase) || val.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            {
                int open = val.IndexOf('(');
                int close = val.IndexOf(')');
                if (open >= 0 && close > open)
                {
                    var comps = val.Substring(open + 1, close - open - 1).Split(',');
                    if (comps.Length >= 3)
                    {
                        float r = float.Parse(comps[0].Trim(), CultureInfo.InvariantCulture) / 255f;
                        float g = float.Parse(comps[1].Trim(), CultureInfo.InvariantCulture) / 255f;
                        float b = float.Parse(comps[2].Trim(), CultureInfo.InvariantCulture) / 255f;
                        float a = comps.Length >= 4 ? float.Parse(comps[3].Trim(), CultureInfo.InvariantCulture) : 1f;
                        return new Color(r, g, b, a);
                    }
                }
            }

            return Color.white;
        }

        private static TextAnchor ParseTextAlignment(string val)
        {
            val = val.ToLowerInvariant();
            if (val == "center") return TextAnchor.MiddleCenter;
            if (val == "right") return TextAnchor.MiddleRight;
            return TextAnchor.MiddleLeft;
        }
    }
}
