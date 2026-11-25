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
    /// Builds a stage at runtime: Abyss backdrop, hero, enemies along the road, boss arena and HUD.
    /// Lives on one GameObject in the Stage scene. (Class name kept so the scene reference stays valid.)
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
            Arena.MaxX = 1000;

            var player = PlayerFactory.Create(new Vector2(3, 1.5f), withCombat: true);
            playerHealth = player.GetComponent<Health>();

            LevelKit.SetupCamera(LevelKit.Abyss, player.transform);
            LevelKit.EnsureGlobalLight();
            new GameObject("GameFeel").AddComponent<GameFeel>();

            stage = new GameObject("StageDirector").AddComponent<StageDirector>();
            stage.player = playerHealth;
            stage.stageId = GameState.CurrentStageId;
            stage.Init();

            var level = LevelKit.BuildGround(LevelKit.Abyss, stage.BossArenaEnd + 10f);
            LevelKit.BuildSpires(level, LevelKit.Abyss, stage.BossArenaEnd + 10f, 7);
            // Boss gate
            LevelKit.Block(level, new Vector2(stage.BossArenaStart, 3.5f), new Vector2(0.8f, 7f), new Color(0.3f, 0.22f, 0.35f), -3);
            LevelKit.Block(level, new Vector2(stage.BossArenaStart, 7.2f), new Vector2(2.4f, 0.6f), new Color(0.55f, 0.35f, 0.75f), -3);

            var hud = new GameObject("HUD").AddComponent<Hud>();
            hud.player = playerHealth;
            hud.combat = player.GetComponent<WarriorCombat>();
            hud.stage = stage;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame) { SceneManager.LoadScene(GameState.WorldMapScene); return; }

            if (!kb.enterKey.wasPressedThisFrame) return;
            if (stage.Current == StageDirector.State.Cleared)
            {
                var town = WorldMapData.TownAfter(stage.stageId);
                if (town != null) GameState.CurrentStageId = town.id;
                SceneManager.LoadScene(town != null ? GameState.TownScene : GameState.WorldMapScene);
            }
            else if (playerHealth.IsDead) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
