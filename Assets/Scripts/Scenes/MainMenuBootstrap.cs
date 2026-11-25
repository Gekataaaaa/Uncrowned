using Uncrowned.Core;
using Uncrowned.UI;
using UnityEngine;

namespace Uncrowned.Scenes
{
    /// <summary>Title screen: New Game (with prologue), Continue, Quit.</summary>
    public class MainMenuBootstrap : MonoBehaviour
    {
        const string Prologue =
            "In the Kingdom of Etheraion, an old law says a child born with mismatched eyes\n" +
            "carries a curse upon the crown.\n\n" +
            "You were born a prince - with one eye violet, and one eye crimson.\n\n" +
            "They took your name, your title, your throne. Then they threw you into\n" +
            "the Abyss of Despair, where the kingdom buries what it fears.\n\n" +
            "No one has ever climbed out.\n\n" +
            "You will be the first. And when you reach the light,\n" +
            "every one of them will kneel - or fall.";

        GameObject menu;
        GameObject prologue;

        void Start()
        {
            var cam = MenuCamera.Setup(new Color(0.05f, 0.03f, 0.08f));
            // The pit the prince wakes up in, behind the title.
            cam.orthographicSize = ArtKit.CameraSize;
            cam.transform.position = new Vector3(0, ArtKit.CameraY, -10);
            ArtKit.Backdrop("Backgrounds/1-1_PitOfExiles", cam);
            ArtKit.GroundStrip(null, "Backgrounds/1-1_PitOfExiles/ground_tile", -15f, 15f);
            var root = UiKit.Canvas("Main Menu");

            // Faint violet/crimson glow behind the title, a nod to the two eyes.
            UiKit.Image(root, "Glow Violet", new Color(0.55f, 0.3f, 1f, 0.08f), new Vector2(0.5f, 0.5f), new Vector2(-260, 160), new Vector2(700, 700), IconFactory.Disc);
            UiKit.Image(root, "Glow Crimson", new Color(0.9f, 0.15f, 0.25f, 0.08f), new Vector2(0.5f, 0.5f), new Vector2(260, 160), new Vector2(700, 700), IconFactory.Disc);

            menu = new GameObject("Menu", typeof(RectTransform));
            menu.transform.SetParent(root, false);
            Stretch(menu);
            var m = menu.transform;
            UiKit.Text(m, "UNCROWNED", 120, new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(1400, 160), color: UiKit.Gold).fontStyle = FontStyle.Bold;
            UiKit.Text(m, "Cast into the Abyss for the color of his eyes. He will climb back - and take the crown.",
                26, new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1400, 50), color: UiKit.Muted).fontStyle = FontStyle.Italic;

            UiKit.Button(m, "New Game", new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(380, 70), ShowPrologue);
            var cont = UiKit.Button(m, "Continue", new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(380, 70),
                () => SceneTransition.Load(GameState.WorldMapScene));
            cont.interactable = GameState.HasSave;
            UiKit.Button(m, "Quit", new Vector2(0.5f, 0.5f), new Vector2(0, -160), new Vector2(380, 70), Quit);
            UiKit.Text(m, "prototype build", 18, new Vector2(1, 0), new Vector2(-120, 30), new Vector2(220, 30), color: new Color(1, 1, 1, 0.3f));

            prologue = new GameObject("Prologue", typeof(RectTransform));
            prologue.transform.SetParent(root, false);
            Stretch(prologue);
            UiKit.Fill(prologue.transform, "Dim", new Color(0, 0, 0, 0.92f));
            UiKit.Text(prologue.transform, Prologue, 30, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1500, 640), color: new Color(0.9f, 0.86f, 0.95f));
            UiKit.Button(prologue.transform, "Begin the climb", new Vector2(0.5f, 0.5f), new Vector2(0, -360), new Vector2(420, 70), Begin);
            prologue.SetActive(false);
        }

        void ShowPrologue()
        {
            menu.SetActive(false);
            prologue.SetActive(true);
        }

        void Begin()
        {
            GameState.NewGame();
            SceneTransition.Load(GameState.WorldMapScene);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void Stretch(GameObject go)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>Camera for UI-only scenes.</summary>
    public static class MenuCamera
    {
        public static Camera Setup(Color background)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            return cam;
        }
    }
}
