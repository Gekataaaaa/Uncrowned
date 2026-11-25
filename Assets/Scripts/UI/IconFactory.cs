using System;
using System.Collections.Generic;
using Uncrowned.Player;
using UnityEngine;

namespace Uncrowned.UI
{
    /// <summary>
    /// Procedural 32x32 pixel-art ability icons and round button sprites.
    /// Placeholders until real icon art is generated or bought.
    /// </summary>
    public static class IconFactory
    {
        const int N = 32;
        static readonly Dictionary<string, Sprite> Cache = new();

        static readonly Color Steel = new(0.85f, 0.87f, 0.92f);
        static readonly Color Gold = new(1f, 0.8f, 0.25f);
        static readonly Color Leather = new(0.55f, 0.35f, 0.2f);
        static readonly Color Sky = new(0.55f, 0.85f, 1f);
        static readonly Color Ember = new(1f, 0.5f, 0.2f);
        static readonly Color Ruby = new(0.9f, 0.2f, 0.3f);
        static readonly Color Outline = new(0.06f, 0.05f, 0.08f);

        public static Sprite Icon(Ability a) => a switch
        {
            Ability.Combo => Make("combo", DrawCombo),
            Ability.Dash => Make("dash", DrawDash),
            Ability.WideSwing => Make("wide", DrawWide),
            Ability.WarCry => Make("cry", DrawCry),
            _ => Make("ult", DrawUlt),
        };

        /// <summary>Filled circle used for the button body and the radial cooldown overlay.</summary>
        public static Sprite Disc => Get("disc", 64, (x, y, s) =>
        {
            float c = (s - 1) / 2f, d = Dist(x, y, c);
            return d <= c ? Color.white : Color.clear;
        });

        /// <summary>Circle outline for the button border.</summary>
        public static Sprite RingSprite => Get("ring", 64, (x, y, s) =>
        {
            float c = (s - 1) / 2f, d = Dist(x, y, c);
            return d <= c && d >= c - 4f ? Color.white : Color.clear;
        });

        /// <summary>Padlock drawn over locked world-map nodes.</summary>
        public static Sprite Lock => Make("lock", DrawLock);

        // ---------- Icons ----------

        static void DrawLock(Color[] p)
        {
            Arc(p, 16, 17, 6, 2, 0, 180, Steel);    // shackle
            for (int y = 4; y <= 17; y++)
                for (int x = 8; x <= 23; x++) Set(p, x, y, Gold);
            Dot(p, 16, 12, 2f, Outline);            // keyhole
            Line(p, 16, 11, 16, 7, 0, Outline);
        }

        static void DrawCombo(Color[] p)
        {
            Line(p, 11, 11, 27, 27, 2, Steel);      // blade
            Line(p, 13, 13, 27, 27, 0, Color.white); // edge highlight
            Line(p, 7, 14, 14, 7, 1, Gold);          // crossguard
            Line(p, 4, 4, 9, 9, 1, Leather);         // grip
            Dot(p, 3, 3, 1.5f, Gold);               // pommel
            // three slash marks
            Line(p, 4, 22, 11, 27, 0, Sky);
            Line(p, 6, 19, 13, 24, 0, Sky);
            Line(p, 20, 4, 25, 11, 0, Sky);
        }

        static void DrawDash(Color[] p)
        {
            Line(p, 10, 16, 25, 16, 2, Steel);
            Line(p, 27, 16, 19, 24, 2, Steel);
            Line(p, 27, 16, 19, 8, 2, Steel);
            Line(p, 2, 10, 8, 10, 0, Sky);
            Line(p, 1, 16, 6, 16, 0, Sky);
            Line(p, 2, 22, 8, 22, 0, Sky);
        }

        static void DrawWide(Color[] p)
        {
            Arc(p, 16, 16, 11, 2, 40, 320, Sky);
            // arrowheads at both ends of the arc
            Line(p, 25, 23, 25, 16, 1, Sky);
            Line(p, 25, 23, 18, 25, 1, Sky);
            Line(p, 25, 9, 25, 16, 1, Sky);
            Line(p, 25, 9, 18, 7, 1, Sky);
            Line(p, 12, 12, 20, 20, 1, Steel);       // sword in the middle
            Line(p, 10, 16, 16, 10, 0, Gold);
        }

        static void DrawCry(Color[] p)
        {
            Dot(p, 9, 16, 4.5f, Ember);
            Dot(p, 10, 16, 2f, Ruby);
            Arc(p, 9, 16, 9, 1, -50, 50, Ember);
            Arc(p, 9, 16, 14, 1, -45, 45, Gold);
            Arc(p, 9, 16, 19, 1, -40, 40, Ember);
        }

        static void DrawUlt(Color[] p)
        {
            // Crown: zigzag top with three peaks over a band.
            for (int x = 5; x <= 26; x++)
            {
                float t = (x - 5) / 21f;           // 0..1 across the crown
                float zig = Mathf.Abs(Mathf.Repeat(t * 4f, 2f) - 1f); // 1 at peaks, 0 at valleys
                int top = Mathf.RoundToInt(Mathf.Lerp(17, 26, zig));
                for (int y = 9; y <= top; y++) Set(p, x, y, Gold);
            }
            for (int x = 5; x <= 26; x++) { Set(p, x, 9, Leather); Set(p, x, 10, Leather); }
            Dot(p, 5, 26, 1.6f, Ruby);
            Dot(p, 15.5f, 26, 1.6f, Color.white);
            Dot(p, 26, 26, 1.6f, Ruby);
            Dot(p, 15.5f, 14, 2f, Ruby);
            // impact cracks below
            Line(p, 15, 6, 10, 2, 0, Sky);
            Line(p, 17, 6, 22, 2, 0, Sky);
            Line(p, 16, 6, 16, 1, 0, Sky);
        }

        // ---------- Raster helpers ----------

        static Sprite Make(string key, Action<Color[]> draw)
        {
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var p = new Color[N * N];
            draw(p);
            AddOutline(p);
            return Cache[key] = ToSprite(key, p, N);
        }

        static Sprite Get(string key, int size, Func<int, int, int, Color> fill)
        {
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            var p = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    p[y * size + x] = fill(x, y, size);
            return Cache[key] = ToSprite(key, p, size);
        }

        static Sprite ToSprite(string key, Color[] p, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Icon_" + key
            };
            tex.SetPixels(p);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = key;
            return sprite;
        }

        static float Dist(float x, float y, float c) => Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));

        static void Set(Color[] p, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= N || y >= N) return;
            p[y * N + x] = c;
        }

        static void Dot(Color[] p, float cx, float cy, float r, Color c)
        {
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) Set(p, x, y, c);
        }

        /// <summary>Thick line; thickness 0 = single pixel.</summary>
        static void Line(Color[] p, int x0, int y0, int x1, int y1, int thickness, Color c)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0 : i / (float)steps;
                float x = Mathf.Lerp(x0, x1, t), y = Mathf.Lerp(y0, y1, t);
                if (thickness == 0) Set(p, Mathf.RoundToInt(x), Mathf.RoundToInt(y), c);
                else Dot(p, x, y, thickness * 0.75f, c);
            }
        }

        /// <summary>Arc from a0 to a1 degrees (0 = right, counter-clockwise).</summary>
        static void Arc(Color[] p, float cx, float cy, float r, int thickness, float a0, float a1, Color c)
        {
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (Mathf.Abs(d - r) > thickness * 0.5f + 0.5f) continue;
                    float a = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (a0 < 0 ? a >= a0 && a <= a1 : (a < 0 ? a + 360 : a) >= a0 && (a < 0 ? a + 360 : a) <= a1)
                        Set(p, x, y, c);
                }
        }

        /// <summary>Dark 1px outline around every drawn pixel so icons read on any background.</summary>
        static void AddOutline(Color[] p)
        {
            var copy = (Color[])p.Clone();
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    if (copy[y * N + x].a > 0) continue;
                    bool near = false;
                    for (int dy = -1; dy <= 1 && !near; dy++)
                        for (int dx = -1; dx <= 1 && !near; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && ny >= 0 && nx < N && ny < N && copy[ny * N + nx].a > 0) near = true;
                        }
                    if (near) p[y * N + x] = Outline;
                }
        }
    }
}
