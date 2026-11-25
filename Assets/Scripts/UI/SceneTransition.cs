using System.Collections;
using System.Collections.Generic;
using Uncrowned.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Uncrowned.UI
{
    /// <summary>
    /// Scene change with the crown iris: the screen closes to a crown (Art/Transitions/CrownIris),
    /// shows an optional title card in the pixel font, loads the scene and opens again.
    /// Use instead of SceneManager.LoadScene.
    /// </summary>
    public class SceneTransition : MonoBehaviour
    {
        const int IrisFrames = 9;          // iris_00 = closed ... iris_08 = open
        const float FrameTime = 0.06f;
        const float Unit = 6f;             // UI units per art pixel (1080 / 180)

        static SceneTransition active;
        public static bool Busy => active != null;

        Image iris;
        Transform card;

        /// <summary>Changes scene; with a title, a card like "1-2 / WEEPING HOLLOWS / subtitle" shows in between.</summary>
        public static void Load(string scene, string number = null, string title = null, string subtitle = null)
        {
            if (active != null) return;
            var go = new GameObject("Scene Transition");
            DontDestroyOnLoad(go);
            active = go.AddComponent<SceneTransition>();
            active.StartCoroutine(active.Run(scene, number, title, subtitle));
        }

        /// <summary>Goes to a world-map node (stage or town) with its title card.</summary>
        public static void Load(MapNode node)
        {
            string scene = node.kind == NodeKind.Town ? GameState.TownScene : GameState.StageScene;
            string number = null, title = node.title;
            int split = node.title.IndexOf("  ", System.StringComparison.Ordinal);
            if (split > 0)
            {
                number = node.title.Substring(0, split);
                title = node.title.Substring(split).Trim();
            }
            string first = node.description.Split('.')[0];
            Load(scene, number, title, first.Length <= 40 ? first : null);
        }

        IEnumerator Run(string scene, string number, string title, string subtitle)
        {
            var root = UiKit.Canvas("Transition UI", 1000);
            root.SetParent(transform, false);
            iris = UiKit.Fill(root, "Iris", Color.white);
            iris.raycastTarget = true;   // swallow clicks while the screen is closed

            for (int i = IrisFrames - 1; i >= 0; i--) yield return Frame(i);

            if (title != null) yield return TitleCard(root, number, title, subtitle);

            var load = SceneManager.LoadSceneAsync(scene);
            while (!load.isDone) yield return null;
            if (card != null) Destroy(card.gameObject);

            for (int i = 0; i < IrisFrames; i++) yield return Frame(i);
            active = null;
            Destroy(gameObject);
        }

        IEnumerator Frame(int i)
        {
            iris.sprite = ArtKit.Sprite($"Transitions/CrownIris/iris_{i:00}");
            yield return new WaitForSecondsRealtime(FrameTime);
        }

        IEnumerator TitleCard(Transform root, string number, string title, string subtitle)
        {
            card = new GameObject("Title Card", typeof(RectTransform)).transform;
            card.SetParent(root, false);
            // Same layout as the art (title_card_example.png): rows counted in art pixels from the top.
            float Y(float px) => 540f - px * Unit;
            var numText = number != null ? PixelText.Create(card, PixelText.Clean(number), 0, Y(62), 1, Unit, Color.clear) : new List<Image>();
            var nameText = PixelText.Create(card, PixelText.Clean(title), 0, Y(74), 2, Unit, Color.clear);
            var rule = UiKit.Image(card, "Rule", new Color(0.87f, 0.31f, 0.31f), new Vector2(0.5f, 0.5f), new Vector2(0, Y(90.5f)), new Vector2(0, Unit));

            Color[] num = { Hex("2a0a10"), Hex("5f0a05"), Hex("a64545"), Hex("de5050") };
            Color[] name = { Hex("2a2238"), Hex("68538a"), Hex("acbbd0"), Hex("fff6e0") };
            for (int step = 0; step < 4; step++)
            {
                foreach (var g in numText) g.color = num[step];
                foreach (var g in nameText) g.color = name[step];
                rule.rectTransform.sizeDelta = new Vector2(140 * Unit * (step + 1) / 4f, Unit);
                yield return new WaitForSecondsRealtime(0.1f);
            }
            if (!string.IsNullOrEmpty(subtitle))
                PixelText.Create(card, PixelText.Clean(subtitle), 0, Y(96), 1, Unit, Hex("68538a"));
            yield return new WaitForSecondsRealtime(0.9f);
        }

        static Color Hex(string h) => ColorUtility.TryParseHtmlString("#" + h, out var c) ? c : Color.white;
    }
}
