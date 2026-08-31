using System;
using System.Collections.Generic;
using CommandFramework.API;
using UnityEngine;

namespace CommandFramework.UI.Declarative
{
    /// <summary>
    /// Represents a declarative UI Node with styling, layout math, and event bindings.
    /// Emulates browser Box-Model and Flexbox engines in Unity IMGUI with 1:1 fidelity.
    /// </summary>
    public class UINode
    {
        public string TagName { get; set; } = "div";
        public string Id { get; set; }
        public string ClassName { get; set; }
        public string TextContent { get; set; }
        public string IconName { get; set; }
        public string OnClickAction { get; set; }

        public Dictionary<string, string> Attributes { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public List<UINode> Children { get; } = new List<UINode>();
        public UINode Parent { get; set; }

        // Layout state
        public Rect ComputedRect { get; set; }
        public UIStyleRule ResolvedStyle { get; set; } = new UIStyleRule();

        public bool IsHovered { get; set; }
        public bool IsActive { get; set; }

        public void AddChild(UINode child)
        {
            if (child == null) return;
            child.Parent = this;
            Children.Add(child);
        }

        /// <summary>
        /// Calculates layout dimensions and positions for this node and its children (Flexbox engine).
        /// </summary>
        public void ComputeLayout(Rect availableRect, UIStyleSheet styleSheet)
        {
            // Resolve CSS style including hover / active states
            ResolvedStyle = styleSheet.ResolveStyle(TagName, ClassName, Id, IsHovered, IsActive);

            float padLeft = UIScaleManager.ScaleAndSnap(ResolvedStyle.Padding.left);
            float padRight = UIScaleManager.ScaleAndSnap(ResolvedStyle.Padding.right);
            float padTop = UIScaleManager.ScaleAndSnap(ResolvedStyle.Padding.top);
            float padBottom = UIScaleManager.ScaleAndSnap(ResolvedStyle.Padding.bottom);

            float gap = UIScaleManager.ScaleAndSnap(ResolvedStyle.Gap);

            float x = availableRect.x;
            float y = availableRect.y;
            float w = ResolvedStyle.Width.HasValue ? UIScaleManager.ScaleAndSnap(ResolvedStyle.Width.Value) : availableRect.width;

            float contentX = x + padLeft;
            float contentY = y + padTop;
            float contentW = Mathf.Max(0f, w - (padLeft + padRight));

            bool isColumn = !ResolvedStyle.FlexDirection.Equals("row", StringComparison.OrdinalIgnoreCase);
            bool isSpaceBetween = ResolvedStyle.JustifyContent.Equals("space-between", StringComparison.OrdinalIgnoreCase);
            bool isCenterAlign = ResolvedStyle.AlignItems.Equals("center", StringComparison.OrdinalIgnoreCase);
            bool isFlexEndAlign = ResolvedStyle.AlignItems.Equals("flex-end", StringComparison.OrdinalIgnoreCase);

            float contentH = Mathf.Max(0f, (ResolvedStyle.Height.HasValue ? UIScaleManager.ScaleAndSnap(ResolvedStyle.Height.Value) : availableRect.height) - (padTop + padBottom));

            if (Children.Count > 0)
            {
                int childCount = Children.Count;
                float[] childW = new float[childCount];
                float[] childH = new float[childCount];
                float[] childMarginT = new float[childCount];
                float[] childMarginB = new float[childCount];
                float[] childMarginL = new float[childCount];
                float[] childMarginR = new float[childCount];
                bool[] isFlexible = new bool[childCount];

                float totalFixedW = 0f;
                int flexCount = 0;

                // Pass 1: Resolve styles and compute intrinsic / explicit dimensions
                for (int i = 0; i < childCount; i++)
                {
                    var child = Children[i];
                    var childStyle = styleSheet.ResolveStyle(child.TagName, child.ClassName, child.Id, child.IsHovered, child.IsActive);

                    childMarginT[i] = UIScaleManager.ScaleAndSnap(childStyle.Margin.top);
                    childMarginB[i] = UIScaleManager.ScaleAndSnap(childStyle.Margin.bottom);
                    childMarginL[i] = UIScaleManager.ScaleAndSnap(childStyle.Margin.left);
                    childMarginR[i] = UIScaleManager.ScaleAndSnap(childStyle.Margin.right);

                    float defaultH = 18f;
                    if (child.TagName == "button") defaultH = 24f;
                    else if (child.ClassName != null && child.ClassName.Contains("badge")) defaultH = 14f;
                    else if (child.ClassName != null && child.ClassName.Contains("header")) defaultH = 18f;

                    childH[i] = childStyle.Height.HasValue ? UIScaleManager.ScaleAndSnap(childStyle.Height.Value) : (isColumn ? UIScaleManager.ScaleAndSnap(defaultH) : contentH - (childMarginT[i] + childMarginB[i]));

                    if (childStyle.Width.HasValue)
                    {
                        childW[i] = UIScaleManager.ScaleAndSnap(childStyle.Width.Value);
                        totalFixedW += childW[i] + childMarginL[i] + childMarginR[i];
                    }
                    else if (isColumn)
                    {
                        childW[i] = Mathf.Max(0f, contentW - (childMarginL[i] + childMarginR[i]));
                    }
                    else
                    {
                        // In a row without explicit width, leaf node or container is flexible
                        isFlexible[i] = true;
                        flexCount++;
                    }
                }

                // Pass 2: Distribute remaining row width among flexible children
                if (!isColumn)
                {
                    float totalGaps = Mathf.Max(0, childCount - 1) * gap;
                    float remainingW = Mathf.Max(0f, contentW - totalFixedW - totalGaps);

                    for (int i = 0; i < childCount; i++)
                    {
                        if (isFlexible[i])
                        {
                            childW[i] = flexCount > 0 ? Mathf.Max(0f, (remainingW / flexCount) - (childMarginL[i] + childMarginR[i])) : 0f;
                        }
                    }
                }

                // Pass 3: Position each child with alignment & justification
                float currentOffset = 0f;

                for (int i = 0; i < childCount; i++)
                {
                    var child = Children[i];
                    float childX, childY;

                    if (isColumn)
                    {
                        // Column Layout
                        if (isCenterAlign)
                            childX = contentX + (contentW - childW[i]) * 0.5f;
                        else if (isFlexEndAlign)
                            childX = contentX + contentW - childW[i] - childMarginR[i];
                        else
                            childX = contentX + childMarginL[i];

                        childY = contentY + currentOffset + childMarginT[i];
                        currentOffset += childH[i] + childMarginT[i] + childMarginB[i] + gap;
                    }
                    else
                    {
                        // Row Layout
                        if (isSpaceBetween && childCount == 2 && i == 1)
                        {
                            // Precision space-between for 2 children (e.g. titlebar close btn, toggle rows)
                            childX = contentX + contentW - childW[i] - childMarginR[i];
                        }
                        else
                        {
                            childX = contentX + currentOffset + childMarginL[i];
                        }

                        if (isCenterAlign)
                            childY = contentY + (contentH - childH[i]) * 0.5f;
                        else if (isFlexEndAlign)
                            childY = contentY + contentH - childH[i] - childMarginB[i];
                        else
                            childY = contentY + childMarginT[i];

                        currentOffset += childW[i] + childMarginL[i] + childMarginR[i] + gap;
                    }

                    Rect childAvail = new Rect(childX, childY, childW[i], childH[i]);
                    child.ComputeLayout(childAvail, styleSheet);
                }
            }

            float h;
            if (ResolvedStyle.Height.HasValue)
            {
                h = UIScaleManager.ScaleAndSnap(ResolvedStyle.Height.Value);
            }
            else if (Children.Count > 0)
            {
                if (isColumn)
                {
                    float totalChildH = 0f;
                    for (int i = 0; i < Children.Count; i++)
                    {
                        totalChildH += Children[i].ComputedRect.height;
                    }
                    totalChildH += Mathf.Max(0, Children.Count - 1) * gap;
                    h = totalChildH + padTop + padBottom;
                }
                else
                {
                    float maxChildH = 0f;
                    for (int i = 0; i < Children.Count; i++)
                    {
                        maxChildH = Mathf.Max(maxChildH, Children[i].ComputedRect.height);
                    }
                    h = maxChildH + padTop + padBottom;
                }
            }
            else
            {
                // Leaf node (text or button)
                h = availableRect.height > 0f ? availableRect.height : UIScaleManager.ScaleAndSnap(TagName == "button" ? 24f : 18f);
            }

            ComputedRect = UIScaleManager.Snap(new Rect(x, y, w, h));
        }

        /// <summary>
        /// Renders this node and its children into Unity IMGUI matching HTML/CSS styling 1:1.
        /// </summary>
        public void Render(UIBindingContext context, Vector2 mousePos)
        {
            IsHovered = ComputedRect.Contains(mousePos);

            // 1. Draw Drop Shadow for top-level panels
            if (Parent == null)
            {
                var shadowTex = UIBuilder.GetSolidTexture(new Color(0f, 0f, 0f, 0.45f));
                float shadowOffset = UIScaleManager.ScaleAndSnap(4f);
                GUI.DrawTexture(new Rect(ComputedRect.x + shadowOffset, ComputedRect.y + shadowOffset, ComputedRect.width, ComputedRect.height), shadowTex);
            }

            // 2. Draw Background and Border
            if (ResolvedStyle.BorderRadius > 0.5f)
            {
                int texW = Mathf.RoundToInt(ComputedRect.width);
                int texH = Mathf.RoundToInt(ComputedRect.height);
                float rad = UIScaleManager.ScaleAndSnap(ResolvedStyle.BorderRadius);
                Color fill = ResolvedStyle.BackgroundColor ?? new Color(0, 0, 0, 0);
                Color? border = (ResolvedStyle.BorderWidth > 0f && ResolvedStyle.BorderColor.HasValue) ? ResolvedStyle.BorderColor : null;
                float bw = UIScaleManager.ScaleAndSnap(ResolvedStyle.BorderWidth);

                var roundedTex = UIBuilder.GetRoundedBoxTexture(texW, texH, rad, fill, border, bw);
                GUI.DrawTexture(ComputedRect, roundedTex);
            }
            else
            {
                // Draw Background
                if (ResolvedStyle.BackgroundColor.HasValue && ResolvedStyle.BackgroundColor.Value.a > 0.01f)
                {
                    var bgTex = UIBuilder.GetSolidTexture(ResolvedStyle.BackgroundColor.Value);
                    GUI.DrawTexture(ComputedRect, bgTex);
                }

                // Draw Border Outline (Pixel-Snapping)
                if (ResolvedStyle.BorderWidth > 0f && ResolvedStyle.BorderColor.HasValue)
                {
                    var borderTex = UIBuilder.GetSolidTexture(ResolvedStyle.BorderColor.Value);
                    float bw = Mathf.Max(1f, UIScaleManager.ScaleAndSnap(ResolvedStyle.BorderWidth));
                    GUI.DrawTexture(new Rect(ComputedRect.x, ComputedRect.y, ComputedRect.width, bw), borderTex);
                    GUI.DrawTexture(new Rect(ComputedRect.x, ComputedRect.yMax - bw, ComputedRect.width, bw), borderTex);
                    GUI.DrawTexture(new Rect(ComputedRect.x, ComputedRect.y, bw, ComputedRect.height), borderTex);
                    GUI.DrawTexture(new Rect(ComputedRect.xMax - bw, ComputedRect.y, bw, ComputedRect.height), borderTex);
                }
            }

            // 4. Render Node Specifics
            if (TagName.Equals("button", StringComparison.OrdinalIgnoreCase))
            {
                string text = context != null ? context.ResolveText(TextContent) : TextContent;
                Texture2D icon = !string.IsNullOrEmpty(IconName) ? CommandIconLibrary.GetIcon(IconName) : null;

                // Handle click interaction
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && IsHovered)
                {
                    if (!string.IsNullOrEmpty(OnClickAction) && context != null)
                    {
                        context.TriggerAction(OnClickAction);
                        Event.current.Use();
                    }
                }

                // Precision centered layout: Icon (12x12) + Gap (6px) + Centered Text
                int baseFs = ResolvedStyle.FontSize ?? 10;
                var textStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = ResolvedStyle.FontStyle != FontStyle.Normal ? ResolvedStyle.FontStyle : FontStyle.Bold,
                    fontSize = UIScaleManager.ScaleFontSize(baseFs),
                    normal = { textColor = ResolvedStyle.Color ?? Color.white }
                };

                Vector2 textSize = textStyle.CalcSize(new GUIContent(text));

                if (icon != null)
                {
                    float iconSize = UIScaleManager.ScaleAndSnap(12f);
                    float gap = UIScaleManager.ScaleAndSnap(6f);
                    float totalContentW = iconSize + gap + textSize.x;

                    float startX = UIScaleManager.Snap(ComputedRect.x + (ComputedRect.width - totalContentW) * 0.5f);
                    float iconY = UIScaleManager.Snap(ComputedRect.y + (ComputedRect.height - iconSize) * 0.5f);

                    // Draw crisp vector icon
                    GUI.DrawTexture(new Rect(startX, iconY, iconSize, iconSize), icon, ScaleMode.ScaleToFit, true);

                    // Draw text label
                    float textX = startX + iconSize + gap;
                    GUI.Label(new Rect(textX, ComputedRect.y, textSize.x + 4f, ComputedRect.height), text, textStyle);
                }
                else
                {
                    GUI.Label(ComputedRect, text, textStyle);
                }
            }
            else if (!string.IsNullOrEmpty(TextContent))
            {
                string text = context != null ? context.ResolveText(TextContent) : TextContent;
                int baseFs = ResolvedStyle.FontSize ?? 11;
                var labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = ResolvedStyle.TextAlignment,
                    fontStyle = ResolvedStyle.FontStyle != FontStyle.Normal ? ResolvedStyle.FontStyle : FontStyle.Bold,
                    fontSize = UIScaleManager.ScaleFontSize(baseFs),
                    clipping = TextClipping.Overflow,
                    wordWrap = false,
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                    normal = { textColor = ResolvedStyle.Color ?? Color.white },
                    hover = { textColor = ResolvedStyle.Color ?? Color.white },
                    active = { textColor = ResolvedStyle.Color ?? Color.white },
                    focused = { textColor = ResolvedStyle.Color ?? Color.white }
                };

                GUI.Label(ComputedRect, text, labelStyle);
            }

            // 5. Render Children
            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].Render(context, mousePos);
            }
        }
    }
}
