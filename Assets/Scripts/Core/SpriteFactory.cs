using System.Collections.Generic;
using UnityEngine;

namespace Uncrowned.Core
{
    /// <summary>
    /// Placeholder pixel sprites generated at runtime, so the prototype needs no art assets.
    /// Swap these for real sprites once an asset pack is chosen.
    /// </summary>
    public static class SpriteFactory
    {
        public const int PixelsPerUnit = 16;
        static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite Square => Get("square", 16, (x, y, s) => true);

        public static Sprite Circle => Get("circle", 16, (x, y, s) =>
        {
            float c = (s - 1) / 2f;
            return (x - c) * (x - c) + (y - c) * (y - c) <= c * c + 1f;
        });

        /// <summary>Ring used for circular slashes (outer radius = 1 unit when scale is 1).</summary>
        public static Sprite Ring => Get("ring", 32, (x, y, s) =>
        {
            float c = (s - 1) / 2f;
            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
            return d <= c && d >= c - 3f;
        });

        /// <summary>Arc sector pointing right (+X), 180 degrees wide, outer radius 1 unit.</summary>
        public static Sprite Arc => Get("arc", 32, (x, y, s) =>
        {
            float c = (s - 1) / 2f;
            float dx = x - c, dy = y - c;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            return dx >= 0 && d <= c && d >= c - 4f;
        });

        static Sprite Get(string key, int size, System.Func<int, int, int, bool> fill)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Gen_" + key
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = fill(x, y, size) ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            tex.SetPixels32(pixels);
            tex.Apply();

            // Square/circle are 1 unit wide; ring/arc are 2 units wide so their radius is 1 unit.
            float ppu = size == 16 ? PixelsPerUnit : size / 2f;
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        public static SpriteRenderer AddRenderer(GameObject go, Sprite sprite, Color color, int order)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
