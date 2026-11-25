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
    /// Builds the side-view stage prototype at runtime: ground, background, warrior,
    /// enemies along the road, boss arena and HUD. Put this on one GameObject in an empty scene.
    /// </summary>
    public class PrototypeBootstrap : MonoBehaviour
    {
        Health playerHealth;
        StageDirector stage;

        void Awake()
        {
            // Characters pass through each other; they only collide with the ground.
            Physics2D.IgnoreLayerCollision(Arena.CharacterLayer, Arena.CharacterLayer, true);
            Arena.MinX = 0;
            Arena.MaxX = 200;

            SetupCamera();
            EnsureGlobalLight();

            var player = BuildPlayer();
            playerHealth = player.GetComponent<Health>();

            var follow = Camera.main.GetComponent<CameraFollow>();
            follow.target = player.transform;
            new GameObject("GameFeel").AddComponent<GameFeel>();

            stage = new GameObject("StageDirector").AddComponent<StageDirector>();
            stage.player = playerHealth;

            BuildLevel(stage.bossArenaStart, 200f);

            var hud = new GameObject("HUD").AddComponent<Hud>();
            hud.player = playerHealth;
            hud.combat = player.GetComponent<WarriorCombat>();
            hud.stage = stage;
        }

        void Update()
        {
            var kb = Keyboard.current;
            bool over = playerHealth != null && (playerHealth.IsDead || stage.Current == StageDirector.State.Cleared);
            if (over && kb != null && kb.enterKey.wasPressedThisFrame)
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
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.13f, 0.14f, 0.22f);
            cam.transform.position = new Vector3(10, 4, -10);
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

        static void BuildLevel(float bossStart, float length)
        {
            var root = new GameObject("Level").transform;

            // Ground with collider. Top surface sits at Arena.GroundY.
            var ground = new GameObject("Ground");
            ground.transform.SetParent(root, false);
            ground.transform.position = new Vector3(length / 2f - 10f, Arena.GroundY - 2f, 0);
            ground.transform.localScale = new Vector3(length + 20f, 4f, 1);
            SpriteFactory.AddRenderer(ground, SpriteFactory.Square, new Color(0.22f, 0.17f, 0.13f), -5);
            var col = ground.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            var grass = new GameObject("Grass");
            grass.transform.SetParent(root, false);
            grass.transform.position = new Vector3(length / 2f - 10f, Arena.GroundY - 0.15f, 0);
            grass.transform.localScale = new Vector3(length + 20f, 0.3f, 1);
            SpriteFactory.AddRenderer(grass, SpriteFactory.Square, new Color(0.25f, 0.38f, 0.2f), -4);

            // Parallax background: distant hills and nearer trees.
            var far = new GameObject("Far Hills").transform;
            far.SetParent(root, false);
            far.gameObject.AddComponent<ParallaxLayer>().factor = 0.8f;
            for (int i = 0; i < 30; i++)
            {
                var h = new GameObject("Hill");
                h.transform.SetParent(far, false);
                float s = Random.Range(5f, 9f);
                h.transform.localPosition = new Vector3(-20 + i * 6f, Arena.GroundY + 1f, 0);
                h.transform.localRotation = Quaternion.Euler(0, 0, 45);
                h.transform.localScale = new Vector3(s, s, 1);
                SpriteFactory.AddRenderer(h, SpriteFactory.Square, new Color(0.18f, 0.19f, 0.28f), -30);
            }

            var near = new GameObject("Trees").transform;
            near.SetParent(root, false);
            near.gameObject.AddComponent<ParallaxLayer>().factor = 0.4f;
            for (int i = 0; i < 60; i++)
            {
                var t = new GameObject("Tree");
                t.transform.SetParent(near, false);
                float hgt = Random.Range(3f, 6f);
                t.transform.localPosition = new Vector3(-10 + i * 4f + Random.Range(-1f, 1f), Arena.GroundY + hgt / 2f, 0);
                t.transform.localScale = new Vector3(Random.Range(0.6f, 1.2f), hgt, 1);
                SpriteFactory.AddRenderer(t, SpriteFactory.Square, new Color(0.15f, 0.22f, 0.18f), -20);
            }

            // Start wall and boss gate pillars.
            MakeBlock(root, new Vector2(-0.5f, 4f), new Vector2(1f, 8f), new Color(0.3f, 0.27f, 0.24f), -3);
            MakeBlock(root, new Vector2(bossStart, 3.5f), new Vector2(0.8f, 7f), new Color(0.45f, 0.35f, 0.2f), -3);
            MakeBlock(root, new Vector2(bossStart, 7.2f), new Vector2(2.4f, 0.6f), new Color(0.6f, 0.5f, 0.25f), -3);
        }

        static void MakeBlock(Transform parent, Vector2 pos, Vector2 size, Color color, int order)
        {
            var w = new GameObject("Block");
            w.transform.SetParent(parent, false);
            w.transform.position = pos;
            w.transform.localScale = new Vector3(size.x, size.y, 1);
            SpriteFactory.AddRenderer(w, SpriteFactory.Square, color, order);
        }

        static GameObject BuildPlayer()
        {
            var go = new GameObject("Warrior");
            go.layer = Arena.CharacterLayer;
            go.transform.position = new Vector3(3, 1.5f, 0);

            // Visual root is flipped by PlayerController to face left/right.
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);

            var body = new GameObject("Body");
            body.transform.SetParent(visual.transform, false);
            body.transform.localScale = new Vector3(0.9f, 1.5f, 1);
            var bodySr = SpriteFactory.AddRenderer(body, SpriteFactory.Square, new Color(0.35f, 0.55f, 0.85f), 10);

            var head = new GameObject("Visor");
            head.transform.SetParent(visual.transform, false);
            head.transform.localPosition = new Vector3(0.25f, 0.45f, 0);
            head.transform.localScale = new Vector3(0.35f, 0.15f, 1);
            SpriteFactory.AddRenderer(head, SpriteFactory.Square, new Color(0.9f, 0.85f, 0.4f), 11);

            // Greatsword held forward and up.
            var blade = new GameObject("Blade");
            blade.transform.SetParent(visual.transform, false);
            blade.transform.localPosition = new Vector3(0.75f, 0.35f, 0);
            blade.transform.localRotation = Quaternion.Euler(0, 0, 50);
            blade.transform.localScale = new Vector3(1.5f, 0.2f, 1);
            SpriteFactory.AddRenderer(blade, SpriteFactory.Square, new Color(0.85f, 0.85f, 0.9f), 12);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            go.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 1.5f);

            var hp = go.AddComponent<Health>();
            hp.body = bodySr;
            hp.SetMax(150f);
            hp.numberColor = new Color(1f, 0.4f, 0.4f);
            hp.Damaged += _ => GameFeel.Shake(0.12f, 0.12f);

            go.AddComponent<Valor>();
            go.AddComponent<PlayerController>();
            go.AddComponent<WarriorCombat>();
            return go;
        }
    }
}
