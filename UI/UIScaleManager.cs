using System;
using CommandFramework.API;
using UnityEngine;

namespace CommandFramework.UI
{
    /// <summary>
    /// Manages HiDPI resolution scaling, dynamic UI scaling factors, and pixel-perfect grid snapping.
    /// Ensures all custom UI rendered by Command Framework is razor-sharp across 1080p, 1440p, 4K, and Ultrawide.
    /// </summary>
    public static class UIScaleManager
    {
        public const float ReferenceHeight = 1080f;

        /// <summary>
        /// Current computed UI scale factor based on screen resolution and user settings.
        /// </summary>
        public static float CurrentScale
        {
            get
            {
                float baseScale = 1.0f;
                if (CommandFrameworkSettings.EnableAutoHiDPI && Screen.height > 0)
                {
                    baseScale = Mathf.Max(0.75f, Screen.height / ReferenceHeight);
                }
                return baseScale * CommandFrameworkSettings.UIScaleMultiplier;
            }
        }

        /// <summary>
        /// Snaps a float value to the nearest integer screen pixel to prevent subpixel blur.
        /// </summary>
        public static float Snap(float value)
        {
            return CommandFrameworkSettings.EnablePixelSnapping ? Mathf.Round(value) : value;
        }

        /// <summary>
        /// Scales a design dimension (at 1080p reference) by CurrentScale and snaps to integer pixels.
        /// </summary>
        public static float ScaleAndSnap(float value)
        {
            return Snap(value * CurrentScale);
        }

        /// <summary>
        /// Snaps a 2D Vector to integer screen pixel coordinates.
        /// </summary>
        public static Vector2 Snap(Vector2 vec)
        {
            if (!CommandFrameworkSettings.EnablePixelSnapping) return vec;
            return new Vector2(Mathf.Round(vec.x), Mathf.Round(vec.y));
        }

        /// <summary>
        /// Scales and snaps a 2D Vector to integer screen pixels.
        /// </summary>
        public static Vector2 ScaleAndSnap(Vector2 vec)
        {
            float s = CurrentScale;
            return Snap(new Vector2(vec.x * s, vec.y * s));
        }

        /// <summary>
        /// Snaps a Rect to exact integer pixel boundaries to eliminate fuzzy borders and distorted textures.
        /// </summary>
        public static Rect Snap(Rect rect)
        {
            if (!CommandFrameworkSettings.EnablePixelSnapping) return rect;
            return new Rect(
                Mathf.Round(rect.x),
                Mathf.Round(rect.y),
                Mathf.Round(rect.width),
                Mathf.Round(rect.height)
            );
        }

        /// <summary>
        /// Scales a Rect by CurrentScale and snaps to integer pixels.
        /// </summary>
        public static Rect ScaleAndSnap(Rect rect)
        {
            float s = CurrentScale;
            return Snap(new Rect(
                rect.x * s,
                rect.y * s,
                rect.width * s,
                rect.height * s
            ));
        }

        /// <summary>
        /// Computes scaled font size snapped to integer point size.
        /// </summary>
        public static int ScaleFontSize(int baseFontSize)
        {
            return Mathf.Max(9, Mathf.RoundToInt(baseFontSize * CurrentScale));
        }
    }
}
