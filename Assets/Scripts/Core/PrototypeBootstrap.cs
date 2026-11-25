using Uncrowned.Combat;
using Uncrowned.Enemies;
using Uncrowned.Player;
using Uncrowned.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Uncrowned.Core
{
    /// <summary>
    /// Builds the combat prototype at runtime: arena, warrior, wave spawner, HUD.
    /// Put this on one GameObject in an otherwise empty scene.
    /// </summary>
    public class PrototypeBootstrap : MonoBehaviour
    {
        Health playerHealth;

        void Awake()
        {
            SetupCamera();
            EnsureGlobalLight();
            BuildArena();

            var player = BuildPlayer();
            playerHealth = player.GetComponent<Health>();

            Camera.main.GetComponent<CameraFollow>().target = player.transform;
            new GameObject("GameFeel").AddComponent<GameFeel>();

            var spawner = new GameObject("EnemySpawner").AddComponent<EnemySpawner>();
            spawner.player = playerHealth;

            var hud = new GameObject("HUD").AddComponent<Hud>();
            hud.player = playerHealth;
            hud.combat = player.GetComponent<WarriorCombat>();
            hud.spawner = spawner;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (playerHealth != null && playerHealth.IsDead && kb != null && kb.enterKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 7.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.05f, 0.08f);
            cam.transform.position = new Vector3(0, 0, -10);
            if (cam.GetComponent<CameraFollow>() == null) cam.gameObject.AddComponent<CameraFollow>();
        }

        static void EnsureGlobalLight()
        {
            foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                if (l.lightType == Light2D.LightType.Global) return;
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
        }

        static void BuildArena()
        {
            var root = new GameObject("Arena").transform;
            var b = Arena.Bounds;
            const float tile = 2f;
            for (float x = b.xMin; x < b.xMax; x += tile)
                for (float y = b.yMin; y < b.yMax; y += tile)
                {
                    var t = new GameObject("Tile");
                    t.transform.SetParent(root, false);
                    t.transform.position = new Vector2(x + tile / 2, y + tile / 2);
                    t.transform.localScale = Vector3.one * tile;
                    bool dark = ((int)((x - b.xMin) / tile) + (int)((y - b.yMin) / tile)) % 2 == 0;
                    SpriteFactory.AddRenderer(t, SpriteFactory.Square, dark ? new Color(0.16f, 0.17f, 0.15f) : new Color(0.19f, 0.2f, 0.17f), -10);
                }

            // Border walls (visual; movement is clamped by Arena.Clamp).
            var wall = new Color(0.32f, 0.28f, 0.24f);
            MakeWall(root, new Vector2(b.center.x, b.yMax + 0.5f), new Vector2(b.width + 2, 1), wall);
            MakeWall(root, new Vector2(b.center.x, b.yMin - 0.5f), new Vector2(b.width + 2, 1), wall);
            MakeWall(root, new Vector2(b.xMin - 0.5f, b.center.y), new Vector2(1, b.height), wall);
            MakeWall(root, new Vector2(b.xMax + 0.5f, b.center.y), new Vector2(1, b.height), wall);
        }

        static void MakeWall(Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var w = new GameObject("Wall");
            w.transform.SetParent(parent, false);
            w.transform.position = pos;
            w.transform.localScale = new Vector3(size.x, size.y, 1);
            SpriteFactory.AddRenderer(w, SpriteFactory.Square, color, -9);
        }

        static GameObject BuildPlayer()
        {
            var go = new GameObject("Warrior");

            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(go.transform, false);
            shadow.transform.localPosition = new Vector3(0, -0.5f, 0);
            shadow.transform.localScale = new Vector3(1f, 0.35f, 1);
            SpriteFactory.AddRenderer(shadow, SpriteFactory.Circle, new Color(0, 0, 0, 0.35f), 1);

            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            var bodySr = SpriteFactory.AddRenderer(body, SpriteFactory.Square, new Color(0.35f, 0.55f, 0.85f), 10);

            // Greatsword: pivot at the player's center, blade pointing along +X, rotated toward the aim.
            var sword = new GameObject("Sword");
            sword.transform.SetParent(go.transform, false);
            var blade = new GameObject("Blade");
            blade.transform.SetParent(sword.transform, false);
            blade.transform.localPosition = new Vector3(0.95f, 0, 0);
            blade.transform.localScale = new Vector3(1.2f, 0.18f, 1);
            SpriteFactory.AddRenderer(blade, SpriteFactory.Square, new Color(0.85f, 0.85f, 0.9f), 11);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            go.AddComponent<CircleCollider2D>().radius = 0.45f;

            var hp = go.AddComponent<Health>();
            hp.body = bodySr;
            hp.SetMax(100f);
            hp.numberColor = new Color(1f, 0.4f, 0.4f);
            hp.Damaged += _ => GameFeel.Shake(0.12f, 0.12f);

            go.AddComponent<Valor>();
            go.AddComponent<PlayerController>();
            go.AddComponent<WarriorCombat>();
            return go;
        }
    }
}
