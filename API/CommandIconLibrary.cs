using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CommandFramework.API
{
    /// <summary>
    /// Central High-Definition Icon Library for Command Framework.
    /// Provides ultra-crisp, anti-aliased 64x64 procedural vector textures and supports loading external PNG/JPG icons.
    /// </summary>
    public static class CommandIconLibrary
    {
        private static readonly Dictionary<string, Texture2D> _icons = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        // Pre-cached High-Definition Icons
        public static Texture2D IconStop { get; private set; }
        public static Texture2D IconResume { get; private set; }
        public static Texture2D IconLoop { get; private set; }
        public static Texture2D IconFormation { get; private set; }
        public static Texture2D IconHoldFire { get; private set; }
        public static Texture2D IconEngage { get; private set; }
        public static Texture2D IconShield { get; private set; }
        public static Texture2D IconRadar { get; private set; }
        public static Texture2D IconBolt { get; private set; }
        public static Texture2D IconPen { get; private set; }

        static CommandIconLibrary()
        {
            InitializeBuiltInIcons();
        }

        public static void InitializeBuiltInIcons()
        {
            if (IconStop != null) return;

            const int S = 64; // High-Definition 64x64 texture size for razor-sharp rendering

            Color green = new Color(0.06f, 0.88f, 0.50f, 1.0f);
            Color amber = new Color(1.0f, 0.70f, 0.20f, 1.0f);
            Color red = new Color(1.0f, 0.30f, 0.35f, 1.0f);
            Color cyan = new Color(0.0f, 0.82f, 1.0f, 1.0f);
            Color white = Color.white;

            IconStop = CreateStopIcon(S, white);
            IconResume = CreatePlayIcon(S, green);
            IconLoop = CreateLoopIcon(S, cyan);
            IconFormation = CreateFormationIcon(S, cyan);
            IconHoldFire = CreateCrosshairIcon(S, red);
            IconEngage = CreateCrosshairIcon(S, green);
            IconShield = CreateShieldIcon(S, white);
            IconRadar = CreateRadarIcon(S, white);
            IconBolt = CreateLightningBoltIcon(S, white);
            IconPen = CreatePenIcon(S, white);

            RegisterIcon("core.stop", IconStop);
            RegisterIcon("core.resume", IconResume);
            RegisterIcon("core.loop", IconLoop);
            RegisterIcon("core.formation", IconFormation);
            RegisterIcon("core.hold_fire", IconHoldFire);
            RegisterIcon("core.engage", IconEngage);
            RegisterIcon("core.shield", IconShield);
            RegisterIcon("core.radar", IconRadar);
            RegisterIcon("core.bolt", IconBolt);
            RegisterIcon("core.lightning", IconBolt);
            RegisterIcon("core.overcharge", IconBolt);
            RegisterIcon("core.pen", IconPen);
            RegisterIcon("core.draw", IconPen);
            RegisterIcon("core.edit", IconPen);
        }

        public static void RegisterIcon(string name, Texture2D texture)
        {
            if (string.IsNullOrEmpty(name) || texture == null) return;
            _icons[name] = texture;
        }

        public static Texture2D GetIcon(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_icons.TryGetValue(name, out var tex)) return tex;
            return null;
        }

        public static Texture2D LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (ImageConversion.LoadImage(tex, data))
                {
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    return tex;
                }
            }
            catch (Exception ex)
            {
                CommandFrameworkPlugin.Log?.LogError($"Failed to load icon from '{filePath}': {ex.Message}");
            }
            return null;
        }

        // --- High-Definition Anti-Aliased Vector Texture Generators (64x64) ---

        private static Texture2D CreateBlankTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color transparent = new Color(0, 0, 0, 0);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = transparent;
            tex.SetPixels(pixels);
            return tex;
        }

        private static Texture2D CreateStopIcon(int S, Color color)
        {
            var tex = CreateBlankTexture(S);
            float radius = S * 0.08f;

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - S * 0.5f) - (S * 0.28f - radius));
                    float dy = Mathf.Max(0, Mathf.Abs(y - S * 0.5f) - (S * 0.28f - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;

                    float alpha = Mathf.Clamp01(0.5f - dist);
                    if (alpha > 0)
                    {
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreatePlayIcon(int S, Color color)
        {
            var tex = CreateBlankTexture(S);
            Vector2 p0 = new Vector2(S * 0.26f, S * 0.80f);
            Vector2 p1 = new Vector2(S * 0.26f, S * 0.20f);
            Vector2 p2 = new Vector2(S * 0.82f, S * 0.50f);

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float d = DistanceToTriangle(p, p0, p1, p2);
                    float alpha = Mathf.Clamp01(0.5f - d);
                    if (alpha > 0)
                    {
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateLoopIcon(int S, Color color)
        {
            var tex = CreateBlankTexture(S);
            Vector2 center = new Vector2(S * 0.5f, S * 0.5f);
            float radius = S * 0.32f;
            float thickness = S * 0.07f;

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float dist = Vector2.Distance(p, center);
                    float ringDist = Mathf.Abs(dist - radius) - (thickness * 0.5f);
                    float alpha = Mathf.Clamp01(0.5f - ringDist);

                    float angle = Mathf.Atan2(p.y - center.y, p.x - center.x) * Mathf.Rad2Deg;
                    if ((angle > 40 && angle < 85) || (angle < -95 && angle > -140))
                    {
                        alpha = 0f;
                    }

                    if (alpha > 0)
                    {
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
                    }
                }
            }

            DrawAntiAliasedTriangle(tex, 
                new Vector2(S * 0.52f, S * 0.88f),
                new Vector2(S * 0.82f, S * 0.82f),
                new Vector2(S * 0.68f, S * 0.60f),
                color);

            DrawAntiAliasedTriangle(tex, 
                new Vector2(S * 0.48f, S * 0.12f),
                new Vector2(S * 0.18f, S * 0.18f),
                new Vector2(S * 0.32f, S * 0.40f),
                color);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateFormationIcon(int S, Color color)
        {
            var tex = CreateBlankTexture(S);
            Vector2 pLead = new Vector2(S * 0.5f, S * 0.80f);
            Vector2 pLeft = new Vector2(S * 0.22f, S * 0.24f);
            Vector2 pRight = new Vector2(S * 0.78f, S * 0.24f);

            DrawAntiAliasedLine(tex, pLead, pLeft, S * 0.04f, new Color(color.r, color.g, color.b, 0.45f));
            DrawAntiAliasedLine(tex, pLead, pRight, S * 0.04f, new Color(color.r, color.g, color.b, 0.45f));

            DrawAntiAliasedCircle(tex, pLead, S * 0.11f, color);
            DrawAntiAliasedCircle(tex, pLeft, S * 0.09f, color);
            DrawAntiAliasedCircle(tex, pRight, S * 0.09f, color);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCrosshairIcon(int S, Color color)
        {
            var tex = CreateBlankTexture(S);
            Vector2 center = new Vector2(S * 0.5f, S * 0.5f);
            float radius = S * 0.32f;
            float thickness = S * 0.045f;

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float ringDist = Mathf.Abs(dist - radius) - (thickness * 0.5f);
                    float alpha = Mathf.Clamp01(0.5f - ringDist);
                    if (alpha > 0)
                    {
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
                    }
                }
            }

            DrawAntiAliasedCircle(tex, center, S * 0.065f, color);

            float lineW = S * 0.045f;
            DrawAntiAliasedLine(tex, new Vector2(center.x, S * 0.08f), new Vector2(center.x, S * 0.22f), lineW, color);
            DrawAntiAliasedLine(tex, new Vector2(center.x, S * 0.78f), new Vector2(center.x, S * 0.92f), lineW, color);
            DrawAntiAliasedLine(tex, new Vector2(S * 0.08f, center.y), new Vector2(S * 0.22f, center.y), lineW, color);
            DrawAntiAliasedLine(tex, new Vector2(S * 0.78f, center.y), new Vector2(S * 0.92f, center.y), lineW, color);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateShieldIcon(int S, Color color)
        {
            var tex = CreateBlankTexture(S);
            Vector2 pTopL = new Vector2(S * 0.22f, S * 0.82f);
            Vector2 pTopR = new Vector2(S * 0.78f, S * 0.82f);
            Vector2 pMidL = new Vector2(S * 0.22f, S * 0.45f);
            Vector2 pMidR = new Vector2(S * 0.78f, S * 0.45f);
            Vector2 pBot = new Vector2(S * 0.50f, S * 0.14f);

            float lineW = S * 0.05f;
            DrawAntiAliasedLine(tex, pTopL, pTopR, lineW, color);
            DrawAntiAliasedLine(tex, pTopL, pMidL, lineW, color);
            DrawAntiAliasedLine(tex, pTopR, pMidR, lineW, color);
            DrawAntiAliasedLine(tex, pMidL, pBot, lineW, color);
            DrawAntiAliasedLine(tex, pMidR, pBot, lineW, color);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateRadarIcon(int S, Color color)
        {
            var tex = CreateBlankTexture(S);
            Vector2 center = new Vector2(S * 0.5f, S * 0.5f);
            float thick = S * 0.045f;

            DrawAntiAliasedCircleRing(tex, center, S * 0.38f, thick, color);
            DrawAntiAliasedCircleRing(tex, center, S * 0.22f, thick, color);
            DrawAntiAliasedCircle(tex, center, S * 0.05f, color);
            DrawAntiAliasedLine(tex, center, new Vector2(S * 0.78f, S * 0.78f), thick, color);

            tex.Apply();
            return tex;
        }

        private static void DrawAntiAliasedCircle(Texture2D tex, Vector2 center, float radius, Color color)
        {
            int S = tex.width;
            int minX = Mathf.Max(0, (int)(center.x - radius - 2));
            int maxX = Mathf.Min(S - 1, (int)(center.x + radius + 2));
            int minY = Mathf.Max(0, (int)(center.y - radius - 2));
            int maxY = Mathf.Min(S - 1, (int)(center.y + radius + 2));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) - radius;
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    if (alpha > 0)
                    {
                        Color existing = tex.GetPixel(x, y);
                        Color blended = Color.Lerp(existing, color, alpha);
                        blended.a = Mathf.Max(existing.a, color.a * alpha);
                        tex.SetPixel(x, y, blended);
                    }
                }
            }
        }

        private static void DrawAntiAliasedCircleRing(Texture2D tex, Vector2 center, float radius, float thickness, Color color)
        {
            int S = tex.width;
            int minX = Mathf.Max(0, (int)(center.x - radius - thickness - 2));
            int maxX = Mathf.Min(S - 1, (int)(center.x + radius + thickness + 2));
            int minY = Mathf.Max(0, (int)(center.y - radius - thickness - 2));
            int maxY = Mathf.Min(S - 1, (int)(center.y + radius + thickness + 2));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dist = Mathf.Abs(Vector2.Distance(new Vector2(x, y), center) - radius) - (thickness * 0.5f);
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    if (alpha > 0)
                    {
                        Color existing = tex.GetPixel(x, y);
                        Color blended = Color.Lerp(existing, color, alpha);
                        blended.a = Mathf.Max(existing.a, color.a * alpha);
                        tex.SetPixel(x, y, blended);
                    }
                }
            }
        }

        private static void DrawAntiAliasedLine(Texture2D tex, Vector2 a, Vector2 b, float thickness, Color color)
        {
            int S = tex.width;
            int minX = Mathf.Max(0, (int)(Mathf.Min(a.x, b.x) - thickness - 2));
            int maxX = Mathf.Min(S - 1, (int)(Mathf.Max(a.x, b.x) + thickness + 2));
            int minY = Mathf.Max(0, (int)(Mathf.Min(a.y, b.y) - thickness - 2));
            int maxY = Mathf.Min(S - 1, (int)(Mathf.Max(a.y, b.y) + thickness + 2));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float dist = DistanceToSegment(p, a, b) - (thickness * 0.5f);
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    if (alpha > 0)
                    {
                        Color existing = tex.GetPixel(x, y);
                        Color blended = Color.Lerp(existing, color, alpha);
                        blended.a = Mathf.Max(existing.a, color.a * alpha);
                        tex.SetPixel(x, y, blended);
                    }
                }
            }
        }

        private static void DrawAntiAliasedTriangle(Texture2D tex, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int S = tex.width;
            int minX = Mathf.Max(0, (int)(Mathf.Min(a.x, Mathf.Min(b.x, c.x)) - 2));
            int maxX = Mathf.Min(S - 1, (int)(Mathf.Max(a.x, Mathf.Max(b.x, c.x)) + 2));
            int minY = Mathf.Max(0, (int)(Mathf.Min(a.y, Mathf.Min(b.y, c.y)) - 2));
            int maxY = Mathf.Min(S - 1, (int)(Mathf.Max(a.y, Mathf.Max(b.y, c.y)) + 2));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float dist = DistanceToTriangle(p, a, b, c);
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    if (alpha > 0)
                    {
                        Color existing = tex.GetPixel(x, y);
                        Color blended = Color.Lerp(existing, color, alpha);
                        blended.a = Mathf.Max(existing.a, color.a * alpha);
                        tex.SetPixel(x, y, blended);
                    }
                }
            }
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        private static float DistanceToTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            Vector2 e0 = p1 - p0, e1 = p2 - p1, e2 = p0 - p2;
            Vector2 v0 = p - p0, v1 = p - p1, v2 = p - p2;

            Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
            Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
            Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));

            float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            Vector2 d = new Vector2(
                Mathf.Min(Mathf.Min(Vector2.Dot(pq0, pq0), Vector2.Dot(pq1, pq1)), Vector2.Dot(pq2, pq2)),
                Mathf.Min(Mathf.Min(s * (v0.x * e0.y - v0.y * e0.x), s * (v1.x * e1.y - v1.y * e1.x)), s * (v2.x * e2.y - v2.y * e2.x))
            );

            return -Mathf.Sqrt(d.x) * Mathf.Sign(d.y);
        }

        private static Texture2D CreateLightningBoltIcon(int S, Color color)
        {
            var tex = CreateBlankTexture(S);
            Vector2 p1 = new Vector2(S * 0.54f, S * 0.92f);
            Vector2 p2 = new Vector2(S * 0.22f, S * 0.44f);
            Vector2 p3 = new Vector2(S * 0.50f, S * 0.44f);
            DrawAntiAliasedTriangle(tex, p1, p2, p3, color);

            Vector2 p4 = new Vector2(S * 0.46f, S * 0.56f);
            Vector2 p5 = new Vector2(S * 0.78f, S * 0.56f);
            Vector2 p6 = new Vector2(S * 0.46f, S * 0.08f);
            DrawAntiAliasedTriangle(tex, p4, p5, p6, color);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreatePenIcon(int S, Color color)
        {
            var tex = CreateBlankTexture(S);
            // Pen tip
            Vector2 tip = new Vector2(S * 0.16f, S * 0.16f);
            Vector2 p1 = new Vector2(S * 0.28f, S * 0.14f);
            Vector2 p2 = new Vector2(S * 0.14f, S * 0.28f);
            DrawAntiAliasedTriangle(tex, tip, p1, p2, color);

            // Pen shaft / body
            DrawAntiAliasedLine(tex, new Vector2(S * 0.22f, S * 0.22f), new Vector2(S * 0.76f, S * 0.76f), S * 0.14f, color);
            // Pen top cap
            DrawAntiAliasedLine(tex, new Vector2(S * 0.74f, S * 0.74f), new Vector2(S * 0.84f, S * 0.84f), S * 0.18f, color);

            tex.Apply();
            return tex;
        }
    }
}
