using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CommandFramework.UI.Declarative
{
    /// <summary>
    /// Represents a parsed Declarative HTML/CSS UI Document.
    /// Can be rendered directly in OnGUI() with full CSS styling and live data-bindings.
    /// </summary>
    public class UIDocument
    {
        public UINode RootNode { get; private set; }
        public UIStyleSheet StyleSheet { get; private set; } = new UIStyleSheet();
        public UIBindingContext BindingContext { get; set; }

        public static UIDocument Parse(string htmlText, string cssText = null)
        {
            var doc = new UIDocument();

            if (!string.IsNullOrEmpty(cssText))
            {
                doc.StyleSheet = UIStyleSheet.Parse(cssText);
            }

            doc.RootNode = ParseHtml(htmlText, doc.StyleSheet);
            return doc;
        }

        public static UIDocument LoadFromFile(string htmlPath, string cssPath = null)
        {
            string html = File.Exists(htmlPath) ? File.ReadAllText(htmlPath) : string.Empty;
            string css = (!string.IsNullOrEmpty(cssPath) && File.Exists(cssPath)) ? File.ReadAllText(cssPath) : null;
            return Parse(html, css);
        }

        public void Render(Rect screenRect, Vector2 mousePos)
        {
            if (RootNode == null) return;

            UpdateHoverState(RootNode, mousePos);
            RootNode.ComputeLayout(screenRect, StyleSheet);
            RootNode.Render(BindingContext, mousePos);
        }

        private void UpdateHoverState(UINode node, Vector2 mousePos)
        {
            if (node == null) return;
            node.IsHovered = node.ComputedRect.Contains(mousePos);
            for (int i = 0; i < node.Children.Count; i++)
            {
                UpdateHoverState(node.Children[i], mousePos);
            }
        }

        public Vector2 MeasureDimensions(Vector2 defaultSize)
        {
            if (RootNode == null) return defaultSize;

            RootNode.ComputeLayout(new Rect(0, 0, defaultSize.x, defaultSize.y), StyleSheet);
            return new Vector2(RootNode.ComputedRect.width, RootNode.ComputedRect.height);
        }

        // --- Lightweight HTML Parser ---

        private static UINode ParseHtml(string html, UIStyleSheet styleSheet)
        {
            if (string.IsNullOrEmpty(html)) return new UINode();

            // Extract inline <style> if present
            var styleMatch = Regex.Match(html, @"<style>(.*?)</style>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (styleMatch.Success)
            {
                var inlineSheet = UIStyleSheet.Parse(styleMatch.Groups[1].Value);
                // Merge rules
                html = html.Remove(styleMatch.Index, styleMatch.Length);
            }

            var root = new UINode { TagName = "root" };
            var stack = new Stack<UINode>();
            stack.Push(root);

            // Match tags and text
            var tagRegex = new Regex(@"(<[a-zA-Z0-9_\-]+(\s+[^>]*)?>)|(</[a-zA-Z0-9_\-]+>)|([^<]+)", RegexOptions.Compiled);
            var matches = tagRegex.Matches(html);

            foreach (Match m in matches)
            {
                string token = m.Value.Trim();
                if (string.IsNullOrEmpty(token)) continue;

                if (token.StartsWith("</"))
                {
                    // Closing Tag
                    if (stack.Count > 1) stack.Pop();
                }
                else if (token.StartsWith("<") && !token.StartsWith("<!--"))
                {
                    // Opening Tag
                    var node = ParseOpeningTag(token);
                    stack.Peek().AddChild(node);

                    bool isSelfClosing = token.EndsWith("/>") || node.TagName == "divider" || node.TagName == "img" || node.TagName == "hr";
                    if (!isSelfClosing)
                    {
                        stack.Push(node);
                    }
                }
                else
                {
                    // Text content
                    if (stack.Count > 0)
                    {
                        var current = stack.Peek();
                        current.TextContent = token;
                    }
                }
            }

            return root.Children.Count > 0 ? root.Children[0] : root;
        }

        private static UINode ParseOpeningTag(string tagStr)
        {
            var node = new UINode();
            tagStr = tagStr.Trim('<', '>', '/').Trim();

            int spaceIdx = tagStr.IndexOf(' ');
            if (spaceIdx < 0)
            {
                node.TagName = tagStr.ToLowerInvariant();
                return node;
            }

            node.TagName = tagStr.Substring(0, spaceIdx).ToLowerInvariant();
            string attrStr = tagStr.Substring(spaceIdx + 1);

            // Parse Attributes
            var attrRegex = new Regex(@"([a-zA-Z0-9_\-:@]+)\s*=\s*[""']([^""']*)[""']", RegexOptions.Compiled);
            var matches = attrRegex.Matches(attrStr);

            foreach (Match attrMatch in matches)
            {
                string key = attrMatch.Groups[1].Value.ToLowerInvariant();
                string val = attrMatch.Groups[2].Value;

                node.Attributes[key] = val;

                if (key == "id") node.Id = val;
                else if (key == "class") node.ClassName = val;
                else if (key == "icon") node.IconName = val;
                else if (key == "@click" || key == "onclick") node.OnClickAction = val;
            }

            return node;
        }
    }
}
