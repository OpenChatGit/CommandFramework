using System;
using System.Collections.Generic;
using UnityEngine;

namespace CommandFramework.UI
{
    /// <summary>
    /// Manages visual themes and color palettes for tactical UI elements.
    /// </summary>
    public static class UIThemeManager
    {
        private static readonly Dictionary<string, UITheme> _themes = new Dictionary<string, UITheme>(StringComparer.OrdinalIgnoreCase);

        public static UITheme ActiveTheme { get; private set; } = new UITheme();

        static UIThemeManager()
        {
            RegisterTheme("TacticalGreen", new UITheme
            {
                Name = "Tactical Green",
                BorderColor = new Color(0.06f, 0.88f, 0.50f, 0.45f),
                HeaderTextColor = new Color(0.06f, 0.88f, 0.50f, 1.0f)
            });

            RegisterTheme("CyberCyan", new UITheme
            {
                Name = "Cyber Cyan",
                BorderColor = new Color(0.0f, 0.82f, 1.0f, 0.45f),
                HeaderTextColor = new Color(0.0f, 0.82f, 1.0f, 1.0f),
                ButtonDefaultBorder = new Color(0.0f, 0.82f, 1.0f, 0.35f)
            });

            RegisterTheme("AmberAlert", new UITheme
            {
                Name = "Amber Alert",
                BorderColor = new Color(1.0f, 0.70f, 0.20f, 0.45f),
                HeaderTextColor = new Color(1.0f, 0.70f, 0.20f, 1.0f),
                ButtonDefaultBorder = new Color(1.0f, 0.70f, 0.20f, 0.35f)
            });
        }

        public static void RegisterTheme(string key, UITheme theme)
        {
            if (string.IsNullOrEmpty(key) || theme == null) return;
            _themes[key] = theme;
        }

        public static void SetTheme(string key)
        {
            if (_themes.TryGetValue(key, out var theme))
            {
                ActiveTheme = theme;
            }
        }

        public static void SetCustomTheme(UITheme theme)
        {
            if (theme != null)
            {
                ActiveTheme = theme;
            }
        }
    }

    /// <summary>
    /// Fluent UI Builder making it effortless to construct tactical IMGUI windows, buttons, and HUD controls.
    /// </summary>
    public static class UIBuilder
    {
        private static readonly Dictionary<string, Texture2D> _colorTextures = new Dictionary<string, Texture2D>();

        public static Texture2D GetSolidTexture(Color color)
        {
            string key = $"{color.r:F3}_{color.g:F3}_{color.b:F3}_{color.a:F3}";
            if (_colorTextures.TryGetValue(key, out var tex) && tex != null) return tex;

            tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixel(0, 0, color);
            tex.Apply();
            _colorTextures[key] = tex;
            return tex;
        }

        public static Texture2D CreateBorderedTexture(int w, int h, Color fill, Color border, int borderWidth)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pix = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = (x < borderWidth || x >= w - borderWidth || y < borderWidth || y >= h - borderWidth);
                    pix[y * w + x] = isBorder ? border : fill;
                }
            }
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        public static Texture2D GetRoundedBoxTexture(int w, int h, float radius, Color fill, Color? border, float borderWidth)
        {
            w = Mathf.Clamp(w, 8, 512);
            h = Mathf.Clamp(h, 8, 512);
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(w, h) * 0.5f);

            Color borderCol = border ?? fill;
            string key = $"rnd_{w}_{h}_{radius:F1}_{fill.r:F2}_{fill.g:F2}_{fill.b:F2}_{fill.a:F2}_{borderCol.r:F2}_{borderCol.g:F2}_{borderCol.b:F2}_{borderCol.a:F2}_{borderWidth:F1}";

            if (_colorTextures.TryGetValue(key, out var tex) && tex != null) return tex;

            tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Signed distance to rounded box
                    Vector2 p = new Vector2(x + 0.5f - w * 0.5f, y + 0.5f - h * 0.5f);
                    Vector2 b = new Vector2(w * 0.5f - radius, h * 0.5f - radius);
                    Vector2 q = new Vector2(Mathf.Abs(p.x) - b.x, Mathf.Abs(p.y) - b.y);
                    float dist = Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0.0f) - radius;

                    if (dist > 0.5f)
                    {
                        pixels[y * w + x] = new Color(0, 0, 0, 0);
                    }
                    else
                    {
                        float alpha = Mathf.Clamp01(0.5f - dist);
                        Color pixelCol = fill;

                        if (borderWidth > 0f && border.HasValue)
                        {
                            float borderDist = dist + borderWidth;
                            if (borderDist >= 0f)
                            {
                                pixelCol = border.Value;
                            }
                            else if (borderDist > -1f)
                            {
                                float t = -borderDist;
                                pixelCol = Color.Lerp(border.Value, fill, t);
                            }
                        }

                        pixelCol.a *= alpha;
                        pixels[y * w + x] = pixelCol;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _colorTextures[key] = tex;
            return tex;
        }

        public static void DrawHeader(string title, Color? color = null, int fontSize = 11)
        {
            var theme = UIThemeManager.ActiveTheme;
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = UIScaleManager.ScaleFontSize(fontSize),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = color ?? theme.HeaderTextColor }
            };
            GUILayout.Label(title.ToUpper(), style);
        }

        public static void DrawBadge(string badgeText, Color color, int fontSize = 9)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = UIScaleManager.ScaleFontSize(fontSize),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = color }
            };
            GUILayout.Label($"[ {badgeText} ]", style);
        }

        public static bool DrawButton(GUIContent content, Color? bgOverride = null, Color? borderOverride = null, bool isEnabled = true, float height = 24f)
        {
            var theme = UIThemeManager.ActiveTheme;
            Color bg = bgOverride ?? theme.ButtonDefaultBg;
            Color border = borderOverride ?? theme.ButtonDefaultBorder;

            var normalTex = CreateBorderedTexture(32, 32, bg, border, 1);
            var hoverTex = CreateBorderedTexture(32, 32, theme.ButtonHoverBg, theme.ButtonHoverBorder, 1);

            int scaledPad = Mathf.RoundToInt(UIScaleManager.ScaleAndSnap(6f));
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fontSize = UIScaleManager.ScaleFontSize(theme.ButtonFontSize),
                fontStyle = FontStyle.Bold,
                imagePosition = ImagePosition.ImageLeft,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(scaledPad, scaledPad, 2, 2),
                contentOffset = Vector2.zero,
                normal = { background = normalTex, textColor = theme.NormalTextColor },
                hover = { background = hoverTex, textColor = Color.white }
            };

            GUI.enabled = isEnabled;
            float scaledHeight = UIScaleManager.ScaleAndSnap(height);
            bool clicked = GUILayout.Button(content, style, GUILayout.Height(scaledHeight));
            GUI.enabled = true;
            return clicked;
        }

        public static void DrawDivider(Color? color = null, float height = 1f)
        {
            Color c = color ?? new Color(1f, 1f, 1f, 0.15f);
            float scaledH = Mathf.Max(1f, UIScaleManager.ScaleAndSnap(height));
            GUILayout.Space(UIScaleManager.ScaleAndSnap(3f));
            GUILayout.Label("", GUI.skin.box, GUILayout.Height(scaledH), GUILayout.ExpandWidth(true));
            GUILayout.Space(UIScaleManager.ScaleAndSnap(3f));
        }
    }
}
