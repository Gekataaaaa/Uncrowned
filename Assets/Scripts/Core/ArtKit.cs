using System.Collections.Generic;
using UnityEngine;

namespace Uncrowned.Core
{
    /// <summary>
    /// Loads the pixel art from Resources/Art (made by Tools/ArtGen/world.py) and places it in the world.
    /// 16 px = 1 unit. The art is drawn for a 320x180 screen with the ground line 30 px above the bottom,
    /// so the camera shows exactly that screen with the art's ground row on <see cref="Arena.GroundY"/>.
    /// Sprites import with a bottom-centre pivot (PixelArtImporter); other pivots are given in pixels here.
    /// </summary>
    public static class ArtKit
    {
        public const float PPU = 16f;
        public const float ScreenW = 320 / PPU;
        public const float ScreenH = 180 / PPU;
        public const float GroundBelow = 30 / PPU;
        public const float CameraSize = ScreenH / 2f;
        public const float CameraY = Arena.GroundY + CameraSize - GroundBelow;
        public const float ScreenTop = CameraY + CameraSize;

        static readonly Dictionary<string, Sprite> Cache = new();

        /// <summary>Sprite at Resources/Art/<paramref name="path"/> (no extension), cached.</summary>
        public static Sprite Sprite(string path, bool warnIfMissing = true)
        {
            if (!Cache.TryGetValue(path, out var s) || s == null)
            {
                s = Resources.Load<Sprite>("Art/" + path);
                if (s == null && warnIfMissing) Debug.LogWarning("Missing sprite: Resources/Art/" + path);
                Cache[path] = s;
            }
            return s;
        }

        /// <summary>
        /// New sprite object with pixel <paramref name="pivotPx"/> (counted from the sprite's top-left)
        /// sitting on <paramref name="pos"/>. Without a pivot the bottom centre (the feet) sits there.
        /// </summary>
        public static SpriteRenderer Place(Transform parent, string path, Vector2 pos, int order, Vector2? pivotPx = null)
        {
            var sprite = Sprite(path);
            var go = new GameObject(System.IO.Path.GetFileName(path));
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (sprite != null && pivotPx.HasValue)
            {
                var r = sprite.rect;
                pos -= new Vector2(pivotPx.Value.x - r.width / 2f, r.height - pivotPx.Value.y) / PPU;
            }
            go.transform.position = pos;
            return sr;
        }

        /// <summary>Ground made of a seamless tile, top edge on the ground line, from x0 to x1.</summary>
        public static SpriteRenderer GroundStrip(Transform parent, string tilePath, float x0, float x1, int order = -10)
        {
            var sr = Place(parent, tilePath, new Vector2((x0 + x1) / 2f, Arena.GroundY - GroundBelow), order);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(x1 - x0, GroundBelow);
            return sr;
        }

        /// <summary>A full 320x180 layer fixed in the world, centred on x (boss colonnade, light pools).</summary>
        public static SpriteRenderer WorldLayer(Transform parent, string path, float x, int order) =>
            Place(parent, path, new Vector2(x, Arena.GroundY - GroundBelow), order);

        /// <summary>
        /// Parallax backdrop from a folder of layers that follow the camera: 0_sky (fixed to the screen),
        /// 1_far or 1_back and 2_mid (slower than the world, tiled sideways) and 4_fg (screen frame on top).
        /// </summary>
        public static Transform Backdrop(string folder, Camera cam)
        {
            var root = new GameObject("Backdrop").transform;
            var c = cam.transform.position;
            float bottom = c.y - cam.orthographicSize;
            float widen = Mathf.Max(1f, cam.orthographicSize * 2f * cam.aspect / ScreenW); // wider than 16:9
            Layer(root, folder + "/0_sky", c.x, bottom, -100, 1f, 1f, false, widen);
            Layer(root, folder + "/1_far", c.x, bottom, -90, 0.8f, 0.9f, true);
            Layer(root, folder + "/1_back", c.x, bottom, -90, 0.8f, 0.9f, true);
            Layer(root, folder + "/2_mid", c.x, bottom, -80, 0.55f, 0.8f, true);
            Layer(root, folder + "/4_fg", c.x, bottom, 50, 1f, 1f, false, widen);
            return root;
        }

        static void Layer(Transform root, string path, float x, float y, int order, float fx, float fy, bool tile, float widen = 1f)
        {
            var sprite = Sprite(path, warnIfMissing: false);
            if (sprite == null) return;
            var layer = new GameObject(System.IO.Path.GetFileName(path)).transform;
            layer.SetParent(root, false);
            layer.position = new Vector3(x, y, 0);
            var p = layer.gameObject.AddComponent<ParallaxLayer>();
            p.factor = fx;
            p.factorY = fy;
            p.tileWidth = tile ? ScreenW : 0;
            int copies = tile ? 1 : 0;
            for (int i = -copies; i <= copies; i++)
            {
                var go = new GameObject("Tile " + i);
                go.transform.SetParent(layer, false);
                go.transform.localPosition = new Vector3(i * ScreenW, 0, 0);
                go.transform.localScale = new Vector3(widen, 1, 1);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = order;
            }
        }
    }
}
