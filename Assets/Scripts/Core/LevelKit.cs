using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Uncrowned.Core
{
    /// <summary>Shared side-view level pieces: camera, light, ground, parallax backdrops.</summary>
    public static class LevelKit
    {
        public struct Theme
        {
            public Color sky, ground, groundTop, far, near;
        }

        public static readonly Theme Abyss = new()
        {
            sky = new Color(0.07f, 0.05f, 0.11f),
            ground = new Color(0.13f, 0.1f, 0.16f),
            groundTop = new Color(0.45f, 0.25f, 0.65f),   // faint violet moss
            far = new Color(0.12f, 0.09f, 0.18f),
            near = new Color(0.17f, 0.12f, 0.23f),
        };

        public static readonly Theme Town = new()
        {
            sky = new Color(0.2f, 0.16f, 0.22f),
            ground = new Color(0.22f, 0.17f, 0.13f),
            groundTop = new Color(0.4f, 0.32f, 0.22f),
            far = new Color(0.18f, 0.14f, 0.2f),
            near = new Color(0.25f, 0.2f, 0.18f),
        };

        public static CameraFollow SetupCamera(Theme theme, Transform target)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = theme.sky;
            cam.transform.position = new Vector3(target != null ? target.position.x : 0, 4, -10);
            var follow = cam.GetComponent<CameraFollow>() ?? cam.gameObject.AddComponent<CameraFollow>();
            follow.target = target;
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

        public static Transform BuildGround(Theme theme, float length)
        {
            var root = new GameObject("Level").transform;
            Block(root, new Vector2(length / 2f - 10f, Arena.GroundY - 2f), new Vector2(length + 40f, 4f), theme.ground, -5)
                .gameObject.AddComponent<BoxCollider2D>().size = Vector2.one;
            Block(root, new Vector2(length / 2f - 10f, Arena.GroundY - 0.12f), new Vector2(length + 40f, 0.25f), theme.groundTop, -4);
            Block(root, new Vector2(-0.5f, 4f), new Vector2(1f, 8f), theme.near, -3); // start wall
            return root;
        }

        /// <summary>Jagged rock spires in two parallax layers (Abyss) — reads as cave walls.</summary>
        public static void BuildSpires(Transform root, Theme theme, float length, int seed)
        {
            var rng = new System.Random(seed);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            var far = new GameObject("Far Spires").transform;
            far.SetParent(root, false);
            far.gameObject.AddComponent<ParallaxLayer>().factor = 0.75f;
            for (float x = -20; x < length * 0.4f + 40; x += R(3f, 6f))
            {
                float h = R(6f, 14f);
                Block(far, new Vector2(x, Arena.GroundY + h / 2f - 1f), new Vector2(R(1.5f, 3.5f), h), theme.far, -30);
            }

            var near = new GameObject("Near Spires").transform;
            near.SetParent(root, false);
            near.gameObject.AddComponent<ParallaxLayer>().factor = 0.4f;
            for (float x = -10; x < length * 0.7f + 30; x += R(4f, 9f))
            {
                float h = R(2f, 7f);
                var s = Block(near, new Vector2(x, Arena.GroundY + h / 2f), new Vector2(R(0.6f, 1.6f), h), theme.near, -20);
                s.localRotation = Quaternion.Euler(0, 0, R(-6f, 6f));
                if (rng.NextDouble() < 0.35)   // glowing crystal at the tip
                    Block(near, new Vector2(x, Arena.GroundY + h + 0.2f), new Vector2(0.4f, 0.4f), new Color(0.6f, 0.35f, 0.95f), -19)
                        .localRotation = Quaternion.Euler(0, 0, 45);
            }
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
