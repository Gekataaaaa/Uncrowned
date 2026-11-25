using Uncrowned.Core;
using Uncrowned.Player;
using Uncrowned.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Uncrowned.Scenes
{
    /// <summary>
    /// Lastlight, the peaceful town. Placeholder: walkable street, building silhouettes, a campfire
    /// and the road back to the world map. Services (forge, merchant...) will be decided later.
    /// </summary>
    public class TownBootstrap : MonoBehaviour
    {
        const float Length = 60f;
        const float ExitX = Length - 3f;

        Transform hero;
        Text prompt;

        void Awake()
        {
            Physics2D.IgnoreLayerCollision(Arena.CharacterLayer, Arena.CharacterLayer, true);
            Arena.MinX = 0;
            Arena.MaxX = Length;

            hero = PlayerFactory.Create(new Vector2(4, 1.5f), withCombat: false).transform;
            LevelKit.SetupCamera(LevelKit.Town, hero);
            LevelKit.EnsureGlobalLight();
            new GameObject("GameFeel").AddComponent<GameFeel>();

            var level = LevelKit.BuildGround(LevelKit.Town, Length);

            // Building silhouettes (roles to be decided).
            float[] xs = { 12f, 22f, 34f, 46f };
            foreach (var x in xs)
            {
                LevelKit.Block(level, new Vector2(x, 2.5f), new Vector2(6f, 5f), new Color(0.28f, 0.22f, 0.2f), -8);
                var roof = LevelKit.Block(level, new Vector2(x, 5.6f), new Vector2(4.6f, 4.6f), new Color(0.35f, 0.18f, 0.16f), -9);
                roof.localRotation = Quaternion.Euler(0, 0, 45);
                LevelKit.Block(level, new Vector2(x, 1.2f), new Vector2(1.2f, 2.4f), new Color(0.12f, 0.08f, 0.06f), -7);       // door
                LevelKit.Block(level, new Vector2(x + 1.8f, 3f), new Vector2(1f, 1f), new Color(1f, 0.75f, 0.35f), -7);         // lit window
            }

            // Campfire in the square.
            LevelKit.Block(level, new Vector2(28f, 0.25f), new Vector2(1.6f, 0.3f), new Color(0.3f, 0.2f, 0.12f), -2);
            LevelKit.Block(level, new Vector2(28f, 0.7f), new Vector2(0.8f, 0.8f), new Color(1f, 0.55f, 0.15f), -1).localRotation = Quaternion.Euler(0, 0, 45);
            LevelKit.Block(level, new Vector2(28f, 0.75f), new Vector2(0.4f, 0.4f), new Color(1f, 0.9f, 0.5f), 0).localRotation = Quaternion.Euler(0, 0, 45);

            // Road out.
            LevelKit.Block(level, new Vector2(ExitX, 2f), new Vector2(0.5f, 4f), new Color(0.4f, 0.3f, 0.2f), -3);
            LevelKit.Block(level, new Vector2(ExitX + 0.6f, 3.4f), new Vector2(1.8f, 0.7f), new Color(0.55f, 0.42f, 0.25f), -3);

            var ui = UiKit.Canvas("Town UI");
            UiKit.Text(ui, "LASTLIGHT", 44, new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(800, 60), color: UiKit.Gold).fontStyle = FontStyle.Bold;
            UiKit.Text(ui, "A refuge on the rim of the Abyss", 22, new Vector2(0.5f, 1), new Vector2(0, -105), new Vector2(800, 30), color: UiKit.Muted);
            UiKit.Text(ui, "A / D - walk    Space - jump    Esc - world map", 18, new Vector2(0, 1), new Vector2(260, -40), new Vector2(480, 30),
                TextAnchor.MiddleLeft, new Color(1, 1, 1, 0.5f));
            prompt = UiKit.Text(ui, "", 28, new Vector2(0.5f, 0), new Vector2(0, 160), new Vector2(800, 40), color: UiKit.Gold);
        }

        void Update()
        {
            bool atExit = hero.position.x > ExitX - 2.5f;
            prompt.text = atExit ? "E - Leave for the world map" : "";

            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame || (atExit && kb.eKey.wasPressedThisFrame))
                SceneManager.LoadScene(GameState.WorldMapScene);
        }
    }
}
