using System.Collections.Generic;
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
    /// World map: the climb from the bottom of the Abyss up to the Kingdom of Etheraion.
    /// Click a node to see it, then Enter to play it.
    /// </summary>
    public class WorldMapBootstrap : MonoBehaviour
    {
        static readonly Vector2 MapSize = new(1180, 980);
        static readonly Vector2 MapCenter = new(-300, 0);

        Text infoTitle, infoBody, infoStatus;
        Button enterButton;
        MapNode selected;
        readonly Dictionary<string, Image> nodeImages = new();

        void Start()
        {
            MenuCamera.Setup(new Color(0.04f, 0.03f, 0.06f));
            var root = UiKit.Canvas("World Map");

            // Region bands, bottom to top.
            Band(root, 0f, 0.47f, new Color(0.2f, 0.1f, 0.3f), "ABYSS OF DESPAIR");
            Band(root, 0.47f, 0.82f, new Color(0.22f, 0.18f, 0.15f), "THE ASHEN WILDS");
            Band(root, 0.82f, 1f, new Color(0.3f, 0.25f, 0.1f), "ETHERAION");

            // Road between nodes, in order.
            var nodes = WorldMapData.Nodes;
            for (int i = 0; i + 1 < nodes.Length; i++) Road(root, ToMap(nodes[i].pos), ToMap(nodes[i + 1].pos));
            foreach (var n in nodes) Node(root, n);

            // Info panel
            var panel = UiKit.Image(root, "Info", UiKit.Panel, new Vector2(1, 0.5f), new Vector2(-330, 0), new Vector2(560, 980));
            var p = panel.transform;
            UiKit.Text(p, "THE ROAD BACK", 22, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(500, 40), color: UiKit.Muted);
            infoTitle = UiKit.Text(p, "", 36, new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(500, 90), color: UiKit.Gold);
            infoTitle.fontStyle = FontStyle.Bold;
            infoBody = UiKit.Text(p, "", 24, new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(480, 300), TextAnchor.UpperLeft);
            infoStatus = UiKit.Text(p, "", 22, new Vector2(0.5f, 0), new Vector2(0, 200), new Vector2(480, 40), color: UiKit.Muted);
            enterButton = UiKit.Button(p, "Enter", new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(360, 72), Enter);
            UiKit.Button(root, "Main Menu", new Vector2(0, 1), new Vector2(140, -50), new Vector2(220, 56),
                () => SceneManager.LoadScene(GameState.MainMenuScene), 22);

            Select(WorldMapData.Get(GameState.CurrentStageId) ?? nodes[0]);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame) SceneManager.LoadScene(GameState.MainMenuScene);
            if (kb.enterKey.wasPressedThisFrame && enterButton.interactable) Enter();
        }

        static Vector2 ToMap(Vector2 normalized) => MapCenter + (normalized - new Vector2(0.5f, 0.5f)) * MapSize;

        static void Band(Transform root, float y0, float y1, Color color, string label)
        {
            float h = (y1 - y0) * MapSize.y;
            float cy = MapCenter.y + ((y0 + y1) * 0.5f - 0.5f) * MapSize.y;
            var band = UiKit.Image(root, "Band " + label, color, new Vector2(0.5f, 0.5f), new Vector2(MapCenter.x, cy), new Vector2(MapSize.x, h));
            UiKit.Text(band.transform, label, 20, new Vector2(0, 0), new Vector2(170, 24), new Vector2(320, 30),
                TextAnchor.MiddleLeft, new Color(1, 1, 1, 0.35f));
        }

        static void Road(Transform root, Vector2 a, Vector2 b)
        {
            var mid = (a + b) * 0.5f;
            var road = UiKit.Image(root, "Road", new Color(0.75f, 0.65f, 0.5f, 0.45f), new Vector2(0.5f, 0.5f), mid, new Vector2((b - a).magnitude, 6));
            road.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
        }

        void Node(Transform root, MapNode n)
        {
            float size = n.kind == NodeKind.Stage ? 54 : 86;
            var img = UiKit.Image(root, "Node " + n.id, Color.white, new Vector2(0.5f, 0.5f), ToMap(n.pos), new Vector2(size, size), IconFactory.Disc);
            var btn = img.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => Select(n));
            nodeImages[n.id] = img;

            // Progress can't change while the map is open, so the lock is decided once here.
            if (!WorldMapData.IsUnlocked(n))
                UiKit.Image(img.transform, "Lock", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.7f, size * 0.7f), IconFactory.Lock);
            else if (n.kind == NodeKind.Kingdom)
                UiKit.Image(img.transform, "Crown", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64), IconFactory.Icon(Ability.Ultimate));
            else if (n.kind == NodeKind.Town)
                UiKit.Text(img.transform, "T", 40, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size), color: new Color(0.1f, 0.08f, 0.05f)).fontStyle = FontStyle.Bold;

            UiKit.Text(img.transform, n.title, 20, new Vector2(0.5f, 0), new Vector2(0, -22), new Vector2(360, 30));
            Paint(n);
        }

        void Paint(MapNode n)
        {
            var img = nodeImages[n.id];
            bool unlocked = WorldMapData.IsUnlocked(n);
            Color c = GameState.IsCleared(n.id) ? UiKit.Gold
                : n.kind == NodeKind.Town ? new Color(0.45f, 0.85f, 0.75f)
                : n.kind == NodeKind.Kingdom ? new Color(0.95f, 0.85f, 0.5f)
                : unlocked ? new Color(0.7f, 0.45f, 1f)
                : new Color(0.35f, 0.33f, 0.4f);
            if (!unlocked) c *= 0.6f;
            c.a = 1;
            img.color = c;
            img.rectTransform.localScale = n == selected ? Vector3.one * 1.25f : Vector3.one;
        }

        void Select(MapNode n)
        {
            selected = n;
            foreach (var node in WorldMapData.Nodes) Paint(node);

            bool unlocked = WorldMapData.IsUnlocked(n);
            infoTitle.text = n.title;
            infoBody.text = n.description;

            if (!unlocked) infoStatus.text = "Locked - clear " + n.requires + " first";
            else if (!n.playable) infoStatus.text = "Not built yet - coming soon";
            else if (GameState.IsCleared(n.id)) infoStatus.text = "Cleared";
            else infoStatus.text = n.kind == NodeKind.Town ? "A safe place to rest" : "Not cleared";

            enterButton.interactable = unlocked && n.playable;
        }

        void Enter()
        {
            if (selected == null || !selected.playable) return;
            GameState.CurrentStageId = selected.id;
            SceneManager.LoadScene(selected.kind == NodeKind.Town ? GameState.TownScene : GameState.StageScene);
        }
    }
}
