using Uncrowned.Combat;
using Uncrowned.Enemies;
using Uncrowned.Player;
using Uncrowned.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Uncrowned.Core
{
    /// <summary>
    /// Builds a stage at runtime: parallax backdrop and props of the stage's theme, hero, enemies along
    /// the road, the boss arena (colonnade, bone throne) with an exit gate that opens once the boss falls.
    /// Lives on one GameObject in the Stage scene. (Class name kept so the scene reference stays valid.)
    /// </summary>
    public class PrototypeBootstrap : MonoBehaviour
    {
        Health playerHealth;
        StageDirector stage;
        GameObject exitGate;
        float exitX;

        void Awake()
        {
            // Characters pass through each other; they only collide with the ground.
            Physics2D.IgnoreLayerCollision(Arena.CharacterLayer, Arena.CharacterLayer, true);
            Arena.MinX = 0;
            Arena.MaxX = 1000;

            var player = PlayerFactory.Create(new Vector2(3, 1.5f), withCombat: true);
            playerHealth = player.GetComponent<Health>();

            LevelKit.SetupCamera(LevelKit.Abyss, player.transform);
            LevelKit.EnsureGlobalLight();
            new GameObject("GameFeel").AddComponent<GameFeel>();

            var theme = StageTheme.For(GameState.CurrentStageId);
            stage = new GameObject("StageDirector").AddComponent<StageDirector>();
            stage.player = playerHealth;
            stage.stageId = GameState.CurrentStageId;
            stage.theme = theme;
            stage.Init();

            float end = stage.BossArenaEnd;
            var level = LevelKit.BuildGround(end + 10f);
            ArtKit.Backdrop(theme.folder, Camera.main);
            ArtKit.GroundStrip(level, theme.folder + "/ground_tile", -20f, stage.BossArenaStart);
            ArtKit.GroundStrip(level, StageTheme.BossArena + "/ground_tile", stage.BossArenaStart, end + 30f);
            BuildRoadProps(level, theme, stage.BossArenaStart);
            BuildBossArena(level, stage.BossArenaStart, end);

            var hud = new GameObject("HUD").AddComponent<Hud>();
            hud.player = playerHealth;
            hud.combat = player.GetComponent<WarriorCombat>();
            hud.stage = stage;
        }

        static void BuildRoadProps(Transform level, StageTheme theme, float roadEnd)
        {
            var rng = new System.Random(5);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            for (float x = R(2f, 5f); x < roadEnd - 3f; x += R(2.5f, 7f))
            {
                var p = theme.groundProps[rng.Next(theme.groundProps.Length)];
                ArtKit.Place(level, p.path, new Vector2(x, Arena.GroundY), -12, p.pivot);
            }
            if (theme.ceilingProp.path != null)
                for (float x = R(8f, 14f); x < roadEnd - 5f; x += R(18f, 30f))
                    ArtKit.Place(level, theme.ceilingProp.path, new Vector2(x, ArtKit.ScreenTop), -15, theme.ceilingProp.pivot);
            if (theme.wallProp.path != null)
                for (float x = R(6f, 12f); x < roadEnd - 5f; x += R(14f, 24f))
                    ArtKit.Place(level, theme.wallProp.path, new Vector2(x, Arena.GroundY), -75, theme.wallProp.pivot);
        }

        void BuildBossArena(Transform level, float start, float end)
        {
            for (float x = start; x < end + 10f; x += ArtKit.ScreenW)
                ArtKit.WorldLayer(level, StageTheme.BossArena + "/1_back", x + ArtKit.ScreenW / 2f, -70);
            float throneX = end - 6f;
            ArtKit.Place(level, "Backgrounds/Props/bone_throne", new Vector2(throneX, Arena.GroundY), -60);
            // The light pools are drawn around the throne at x = 230 px of the 320 px layer.
            ArtKit.WorldLayer(level, StageTheme.BossArena + "/5_lights", throneX - (230 - 160) / ArtKit.PPU, -55);

            exitX = end - 1.5f;
            exitGate = ArtKit.Place(level, "Transitions/stage_gate", new Vector2(exitX, Arena.GroundY), -50, new Vector2(42, 66)).gameObject;
            exitGate.SetActive(false);
        }

        void Update()
        {
            bool cleared = stage.Current == StageDirector.State.Cleared;
            if (cleared && !exitGate.activeSelf) exitGate.SetActive(true);
            if (SceneTransition.Busy) return;

            if (cleared && playerHealth.transform.position.x > exitX - 1f) { Leave(); return; }

            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame) { SceneTransition.Load(GameState.WorldMapScene); return; }

            if (!kb.enterKey.wasPressedThisFrame) return;
            if (cleared) Leave();
            else if (playerHealth.IsDead) SceneTransition.Load(SceneManager.GetActiveScene().name);
        }

        void Leave()
        {
            var town = WorldMapData.TownAfter(stage.stageId);
            if (town == null) { SceneTransition.Load(GameState.WorldMapScene); return; }
            GameState.CurrentStageId = town.id;
            SceneTransition.Load(town);
        }
    }
}
