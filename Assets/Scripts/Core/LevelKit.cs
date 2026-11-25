using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Uncrowned.Core
{
    /// <summary>Shared side-view level pieces: camera, light, ground collider.</summary>
    public static class LevelKit
    {
        /// <summary>Camera clear colour behind the art (only visible past the edges of a backdrop).</summary>
        public struct Theme
        {
            public Color sky;
        }

        public static readonly Theme Abyss = new() { sky = new Color(0.05f, 0.04f, 0.08f) };
        public static readonly Theme Town = new() { sky = new Color(0.05f, 0.04f, 0.06f) };

        public static CameraFollow SetupCamera(Theme theme, Transform target)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = ArtKit.CameraSize;   // exactly the art's 320x180 screen
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = theme.sky;
            cam.transform.position = new Vector3(target != null ? target.position.x : 0, ArtKit.CameraY, -10);
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.baseY = ArtKit.CameraY;
            follow.target = target;
            if (target != null) follow.SnapTo(target.position);
            return follow;
        }

        public static void EnsureGlobalLight()
        {
            foreach (var l in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                if (l.lightType == Light2D.LightType.Global) return;
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
        }

        /// <summary>Invisible ground collider for the whole level; the visible ground is art (see ArtKit.GroundStrip).</summary>
        public static Transform BuildGround(float length)
        {
            var root = new GameObject("Level").transform;
            var ground = new GameObject("Ground Collider");
            ground.transform.SetParent(root, false);
            ground.transform.position = new Vector2(length / 2f - 10f, Arena.GroundY - 2f);
            ground.AddComponent<BoxCollider2D>().size = new Vector2(length + 40f, 4f);
            return root;
        }

        public static Transform Block(Transform parent, Vector2 pos, Vector2 size, Color color, int order)
        {
            var go = new GameObject("Block");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            SpriteFactory.AddRenderer(go, SpriteFactory.Square, color, order);
            return go.transform;
        }
    }
}
