using System;
using System.Collections.Generic;
using CommandFramework.API;
using UnityEngine;

namespace CommandFramework.UI
{
    /// <summary>
    /// Contract for rendering tactical unit context menus.
    /// Mod developers can implement this interface to provide their own custom IMGUI, uGUI, or HUD renderer.
    /// </summary>
    public interface IMenuRenderer
    {
        /// <summary>
        /// Unique identifier for this menu renderer.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Initializes graphic styles, fonts, or textures needed by the renderer.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Computes the exact width and height of the menu before rendering.
        /// </summary>
        Vector2 CalculateMenuDimensions(Unit unit, IReadOnlyList<IUnitCommandAction> actions);

        /// <summary>
        /// Renders the context menu UI at the specified screen rectangle.
        /// </summary>
        void RenderMenu(Rect screenRect, Unit unit, IReadOnlyList<IUnitCommandAction> visibleActions, Action onClose);
    }

    /// <summary>
    /// Visual theme properties for customizing tactical UI components.
    /// </summary>
    public class UITheme
    {
        public string Name { get; set; } = "Tactical Dark Green";
        public Color BackgroundColor { get; set; } = new Color(0.02f, 0.04f, 0.06f, 0.96f);
        public Color BorderColor { get; set; } = new Color(0.06f, 0.88f, 0.50f, 0.45f);
        public int BorderWidth { get; set; } = 1;
        public float MenuWidth { get; set; } = 210f;
        public float HeaderHeight { get; set; } = 20f;
        public float ButtonHeight { get; set; } = 24f;
        public float ButtonSpacing { get; set; } = 3f;
        public int HeaderFontSize { get; set; } = 11;
        public int BadgeFontSize { get; set; } = 9;
        public int ButtonFontSize { get; set; } = 10;
        public Color HeaderTextColor { get; set; } = new Color(0.06f, 0.88f, 0.50f, 1.0f);
        public Color NormalTextColor { get; set; } = new Color(0.85f, 0.95f, 1.0f, 1.0f);
        public Color ButtonDefaultBg { get; set; } = new Color(0.06f, 0.88f, 0.50f, 0.08f);
        public Color ButtonDefaultBorder { get; set; } = new Color(0.06f, 0.88f, 0.50f, 0.35f);
        public Color ButtonHoverBg { get; set; } = new Color(0.06f, 0.88f, 0.50f, 0.25f);
        public Color ButtonHoverBorder { get; set; } = new Color(0.06f, 0.88f, 0.50f, 1.0f);
    }
}
