using Uncrowned.Core;
using Uncrowned.Player;
using Uncrowned.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Uncrowned.Scenes
{
    /// <summary>
    /// Lastlight, the peaceful town in a cave: houses, a forge, the campfire square, townsfolk and the
    /// road back to the world map. Services (forge, merchant...) will be decided later.
    /// </summary>
    public class TownBootstrap : MonoBehaviour
    {
        const float Length = 60f;
        const float ExitX = Length - 3f;
        const float CampfireX = 28f;

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

            var level = LevelKit.BuildGround(Length);
            ArtKit.Backdrop("Town", Camera.main);
            ArtKit.GroundStrip(level, "Town/ground_tile", -20f, Length + 20f);

            // Buildings: pivots are the bottom of the left wall (the sprites leave room for window glow).
            ArtKit.Place(level, "Town/Objects/house_small", new Vector2(4f, 0), -30, new Vector2(12, 52));
            ArtKit.Place(level, "Town/Objects/house_large", new Vector2(15f, 0), -30, new Vector2(12, 60));
            ArtKit.Place(level, "Town/Objects/forge", new Vector2(38f, 0), -30, new Vector2(10, 91));
            ArtKit.Place(level, "Town/Objects/house_small", new Vector2(47f, 0), -30, new Vector2(12, 52));
            ArtKit.Place(level, "Town/Objects/lantern_string", new Vector2(6.4f, 2.5f), -25, new Vector2(5, 10));
            ArtKit.Place(level, "Town/Objects/lantern_string", new Vector2(28.5f, 2.6f), -25, new Vector2(5, 10));

            // Campfire in the square, with its light pool (drawn at x = 150 px of the 320 px layer).
            var fire = ArtKit.Place(level, "Town/Objects/campfire_0", new Vector2(CampfireX, 0), -5);
            SpriteFrames.Play(fire, "Town/Objects/campfire_", 3, 8f);
            ArtKit.WorldLayer(level, "Town/5_lights", CampfireX + (160 - 150) / ArtKit.PPU, 30);

            // Townsfolk (roles to be decided).
            ArtKit.Place(level, "Town/Objects/npc_merchant", new Vector2(13f, 0), 4);
            ArtKit.Place(level, "Town/Objects/npc_elder", new Vector2(25f, 0), 4);
            ArtKit.Place(level, "Town/Objects/npc_blacksmith", new Vector2(42.5f, 0), 4).flipX = true;

            // Road out.
            LevelKit.Block(level, new Vector2(ExitX, 2f), new Vector2(0.5f, 4f), new Color(0.4f, 0.3f, 0.2f), -3);
            LevelKit.Block(level, new Vector2(ExitX + 0.6f, 3.4f), new Vector2(1.8f, 0.7f), new Color(0.55f, 0.42f, 0.25f), -3);
            ArtKit.Place(level, "Town/Objects/lantern", new Vector2(ExitX + 1.3f, 3.05f), -2, new Vector2(7, 0));

            var ui = UiKit.Canvas("Town UI");
            UiKit.Text(ui, "LASTLIGHT", 44, new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(800, 60), color: UiKit.Gold).fontStyle = FontStyle.Bold;
            UiKit.Text(ui, "A refuge on the rim of the Abyss", 22, new Vector2(0.5f, 1), new Vector2(0, -105), new Vector2(800, 30), color: UiKit.Muted);
            UiKit.Text(ui, "A / D - walk    Space - jump    Esc - world map", 18, new Vector2(0, 1), new Vector2(260, -40), new Vector2(480, 30),
                TextAnchor.MiddleLeft, new Color(1, 1, 1, 0.5f));
            prompt = UiKit.Text(ui, "", 28, new Vector2(0.5f, 0), new Vector2(0, 160), new Vector2(800, 40), color: UiKit.Gold);
        }

        void Update()
        {
            if (SceneTransition.Busy) return;
            bool atExit = hero.position.x > ExitX - 2.5f;
            prompt.text = atExit ? "E - Leave for the world map" : "";

            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame || (atExit && kb.eKey.wasPressedThisFrame))
                SceneTransition.Load(GameState.WorldMapScene);
        }
    }
}
