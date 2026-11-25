using System.Collections.Generic;
using Uncrowned.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Uncrowned.UI
{
    /// <summary>
    /// The 3x5 pixel font from Resources/Art/Transitions/font_3x5 (glyph order in font_3x5.txt),
    /// laid out as one UI Image per letter. Sizes are in art pixels; `unit` is UI units per art pixel.
    /// </summary>
    public static class PixelText
    {
        static Dictionary<char, Sprite> glyphs;

        static void Load()
        {
            glyphs = new Dictionary<char, Sprite>();
            var sheet = ArtKit.Sprite("Transitions/font_3x5");
            var order = Resources.Load<TextAsset>("Art/Transitions/font_3x5");
            if (sheet == null || order == null) return;
            var lines = order.text.Split('\n');
            string chars = lines.Length > 1 ? lines[1].TrimEnd('\r') : "";
            for (int i = 0; i < chars.Length; i++)
            {
                var r = new Rect(sheet.rect.x + i * 4, sheet.rect.y, 3, 5);
                if (r.xMax > sheet.texture.width) break;
                var g = Sprite.Create(sheet.texture, r, new Vector2(0.5f, 0.5f), ArtKit.PPU);
                g.name = "glyph " + chars[i];
                glyphs[chars[i]] = g;
            }
        }

        /// <summary>Width of `s` in art pixels at `scale`.</summary>
        public static int Width(string s, int scale = 1) => (s.Length * 4 - 1) * scale;

        /// <summary>Text centred on `centerX`, top edge at `top` (UI units, parent's centre is 0,0).</summary>
        public static List<Image> Create(Transform parent, string s, float centerX, float top, int scale, float unit, Color color)
        {
            if (glyphs == null) Load();
            var list = new List<Image>();
            s = s.ToUpperInvariant();
            float x = centerX - Width(s, scale) * unit / 2f;
            foreach (char ch in s)
            {
                if (glyphs.TryGetValue(ch, out var g) && ch != ' ')
                {
                    var img = UiKit.Image(parent, "Glyph", color, new Vector2(0.5f, 0.5f),
                        new Vector2(x + 1.5f * scale * unit, top - 2.5f * scale * unit), new Vector2(3, 5) * scale * unit, g);
                    img.raycastTarget = false;
                    list.Add(img);
                }
                x += 4 * scale * unit;
            }
            return list;
        }

        /// <summary>Drops characters the font has no glyph for.</summary>
        public static string Clean(string s)
        {
            if (glyphs == null) Load();
            var sb = new System.Text.StringBuilder();
            foreach (char ch in s.ToUpperInvariant()) sb.Append(glyphs.ContainsKey(ch) || ch == ' ' ? ch : ' ');
            return sb.ToString().Trim();
        }
    }
}
