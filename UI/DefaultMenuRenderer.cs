using System;
using System.Collections.Generic;
using CommandFramework.API;
using UnityEngine;

namespace CommandFramework.UI
{
    /// <summary>
    /// Default tactical IMGUI context menu renderer implementing IMenuRenderer.
    /// Fully respects UIThemeManager customizations and renders high-definition action buttons and dynamic badges.
    /// </summary>
    public class DefaultMenuRenderer : IMenuRenderer
    {
        public string Id => "core.default_imgui_renderer";

        private Texture2D _windowBgTexture;
        private GUIStyle _windowBoxStyle;

        public void Initialize()
        {
            var theme = UIThemeManager.ActiveTheme;
            _windowBgTexture = UIBuilder.CreateBorderedTexture(64, 64, theme.BackgroundColor, theme.BorderColor, theme.BorderWidth);
            _windowBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _windowBgTexture },
                padding = new RectOffset(8, 8, 6, 8)
            };
        }

        public Vector2 CalculateMenuDimensions(Unit unit, IReadOnlyList<IUnitCommandAction> actions)
        {
            var theme = UIThemeManager.ActiveTheme;
            int count = actions?.Count ?? 0;
            float height = 64f + (count * (theme.ButtonHeight + theme.ButtonSpacing)) + 8f;
            return UIScaleManager.ScaleAndSnap(new Vector2(theme.MenuWidth, height));
        }

        public void RenderMenu(Rect screenRect, Unit unit, IReadOnlyList<IUnitCommandAction> visibleActions, Action onClose)
        {
            if (_windowBoxStyle == null || _windowBgTexture == null)
            {
                Initialize();
            }

            Rect snappedRect = UIScaleManager.Snap(screenRect);
            GUI.Window(98234, snappedRect, id => DrawContent(unit, visibleActions), "", _windowBoxStyle);
        }

        private void DrawContent(Unit unit, IReadOnlyList<IUnitCommandAction> visibleActions)
        {
            if (unit == null || unit.disabled) return;

            GUILayout.BeginVertical();

            // 1. Unit Header Title
            string unitName = !string.IsNullOrEmpty(unit.NetworkunitName) ? unit.NetworkunitName : unit.name;
            UIBuilder.DrawHeader(unitName);

            // 2. Status Badge Calculation
            var state = CommandFrameworkAPI.GetOrderState(unit);
            string statusBadge = !string.IsNullOrEmpty(state.DisplayBadge) ? state.DisplayBadge : "ACTIVE";
            Color statusColor = state.BadgeColor ?? CommandFrameworkSettings.ColorCyan;

            UIBuilder.DrawBadge(statusBadge, statusColor);
            GUILayout.Space(4);

            // 3. Dynamic Action Buttons
            if (visibleActions != null)
            {
                foreach (var action in visibleActions)
                {
                    string label = action.GetDisplayName(unit);
                    Texture2D icon = action.GetIcon(unit);
                    GUIContent btnContent = (icon != null) ? new GUIContent($" {label}", icon) : new GUIContent(label);
                    Color? colorOverride = action.GetButtonColor(unit);

                    bool isEnabled = action.IsEnabled(unit);
                    if (UIBuilder.DrawButton(btnContent, colorOverride, null, isEnabled))
                    {
                        CommandFrameworkAPI.ExecuteCommand(action, unit);
                    }

                    GUILayout.Space(UIThemeManager.ActiveTheme.ButtonSpacing);
                }
            }

            GUILayout.Space(4);
            GUILayout.EndVertical();
        }
    }
}
