using System;
using UnityEngine;

namespace CommandFramework.API
{
    /// <summary>
    /// Global configuration settings for Command Framework SDK.
    /// Allows mod developers to configure UI behavior, HiDPI scaling, and color tokens.
    /// </summary>
    public static class CommandFrameworkSettings
    {
        /// <summary>
        /// Controls whether the built-in tactical context menu opens when right-clicking units.
        /// Mod developers building their own UI can set this to false.
        /// </summary>
        public static bool EnableDefaultContextMenu { get; set; } = true;

        /// <summary>
        /// Controls whether the game startup init banner is displayed.
        /// </summary>
        public static bool EnableStartupInitBanner { get; set; } = true;

        /// <summary>
        /// Tactical green color token used throughout the framework.
        /// </summary>
        public static Color ColorTacticalGreen { get; set; } = new Color(0.06f, 0.88f, 0.50f, 1.0f);

        /// <summary>
        /// Amber warning color token.
        /// </summary>
        public static Color ColorAmber { get; set; } = new Color(1.0f, 0.70f, 0.20f, 1.0f);

        /// <summary>
        /// Cyan information color token.
        /// </summary>
        public static Color ColorCyan { get; set; } = new Color(0.0f, 0.82f, 1.0f, 1.0f);

        /// <summary>
        /// Hostile / Alert red color token.
        /// </summary>
        public static Color ColorHostileRed { get; set; } = new Color(1.0f, 0.30f, 0.35f, 1.0f);

        /// <summary>
        /// UI Scale multiplier (e.g. 1.0 = normal, 1.25 = 125%, 1.5 = 150%, 2.0 = 200%).
        /// </summary>
        public static float UIScaleMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// Controls whether UI elements automatically scale proportionally to resolution (1080p, 1440p, 4K).
        /// </summary>
        public static bool EnableAutoHiDPI { get; set; } = true;

        /// <summary>
        /// Controls whether UI positions, bounds, borders, and text are snapped to integer pixels to prevent blur.
        /// </summary>
        public static bool EnablePixelSnapping { get; set; } = true;
    }
}
