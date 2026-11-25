using System.Collections.Generic;
using Uncrowned.Core;
using Uncrowned.Player;
using Uncrowned.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Uncrowned.Scenes
{
    /// <summary>
    /// World map: the climb from the bottom of the Abyss up to the Kingdom of Etheraion, drawn from
    /// Resources/Art/WorldMap (background, fog over locked regions, frame, node icons per state).
    /// Click a node to see it, then Enter to play it.
    /// </summary>
    public class WorldMapBootstrap : MonoBehaviour
    {
        // The map art is 320x180 px drawn 6x on the 1920x1080 canvas; node positions follow the art's frame.
        const float Unit = 6f;
        static readonly Vector2 ArtSize = new(320, 180);

        Text infoTitle, infoBody, infoStatus;
        Button enterButton;
        MapNode selected;
        readonly Dictionary<string, Image> nodeImages = new();

        void Start()
        {
            MenuCamera.Setup(new Color(0.04f, 0.03f, 0.06f));
            var root = UiKit.Canvas("World Map");

            var map = UiKit.Image(root, "Map", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, ArtSize * Unit, ArtKit.Sprite("WorldMap/0_bg")).transform;
            var nodes = WorldMapData.Nodes;
            for (int i = 0; i + 1 < nodes.Length; i++) Road(map, nodes[i], nodes[i + 1]);
            if (!WorldMapData.IsUnlocked(WorldMapData.Get("2-1")))
                Layer(map, "WorldMap/1_fog");
            Layer(map, "WorldMap/2_frame");
            foreach (var n in nodes) Node(map, n);
            var here = WorldMapData.Get(GameState.CurrentStageId) ?? nodes[0];
            var marker = UiKit.Image(map, "Hero Marker", Color.white, new Vector2(0.5f, 0.5f), ToMap(here.pos) + new Vector2(13, -2) * Unit,
                new Vector2(11, 13) * Unit, ArtKit.Sprite("WorldMap/Icons/hero_marker"));
            marker.rectTransform.pivot = new Vector2(0.5f, 0f);
            marker.raycastTarget = false;

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
                () => SceneTransition.Load(GameState.MainMenuScene), 22);

            Select(WorldMapData.Get(GameState.CurrentStageId) ?? nodes[0]);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (SceneTransition.Busy) return;
            if (kb.escapeKey.wasPressedThisFrame) SceneTransition.Load(GameState.MainMenuScene);
            if (kb.enterKey.wasPressedThisFrame && enterButton.interactable) Enter();
        }

        /// <summary>Normalized node position to map coordinates (same frame as Tools/ArtGen/world.py node_pos).</summary>
        static Vector2 ToMap(Vector2 normalized)
        {
            var px = new Vector2(26f + normalized.x * 268f, 160f - normalized.y * 140f);
            return new Vector2(px.x - ArtSize.x / 2f, ArtSize.y / 2f - px.y) * Unit;
        }

        static void Layer(Transform map, string path)
        {
            var img = UiKit.Image(map, path, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, ArtSize * Unit, ArtKit.Sprite(path));
            img.raycastTarget = false;
        }

        /// <summary>Dotted road, one dot every 4 art pixels: gold once the far end is unlocked.</summary>
        static void Road(Transform map, MapNode from, MapNode to)
        {
            Vector2 a = ToMap(from.pos), b = ToMap(to.pos);
            int steps = Mathf.FloorToInt((b - a).magnitude / (4 * Unit));
            var dot = ArtKit.Sprite(WorldMapData.IsUnlocked(to) ? "WorldMap/Icons/path_dot_open" : "WorldMap/Icons/path_dot_locked");
            for (int i = 1; i < steps; i++)
                UiKit.Image(map, "Road", Color.white, new Vector2(0.5f, 0.5f), Vector2.Lerp(a, b, i / (float)steps), Vector2.one * Unit, dot).raycastTarget = false;
        }

        static string IconFor(MapNode n)
        {
            if (n.kind == NodeKind.Town) return "node_town";
            if (n.kind == NodeKind.Kingdom) return "node_castle";
            if (n.id == "1-5") return "node_boss";
            string state = GameState.IsCleared(n.id) ? "cleared" : WorldMapData.IsUnlocked(n) ? "open" : "locked";
            return (n.id == "1-4" ? "node_skull_" : "node_stage_") + state;
        }

        void Node(Transform map, MapNode n)
        {
            var sprite = ArtKit.Sprite("WorldMap/Icons/" + IconFor(n));
            var size = (sprite != null ? sprite.rect.size : new Vector2(9, 9)) * Unit;
            var img = UiKit.Image(map, "Node " + n.id, Color.white, new Vector2(0.5f, 0.5f), ToMap(n.pos), size, sprite);
            // Where the node point sits inside the icons that are not centred on it.
            if (n.kind == NodeKind.Kingdom) img.rectTransform.pivot = new Vector2(0.5f, 4.5f / 29f);
            else if (n.id == "1-5") img.rectTransform.pivot = new Vector2(0.5f, 6.5f / 15f);
            var btn = img.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => Select(n));
            nodeImages[n.id] = img;

            string label = n.kind == NodeKind.Stage ? n.id : n.kind == NodeKind.Town ? "LASTLIGHT" : "ETHERAION";
            var color = WorldMapData.IsUnlocked(n) ? new Color(0.89f, 0.84f, 0.72f) : new Color(0.37f, 0.35f, 0.49f);
            PixelText.Create(map, label, ToMap(n.pos).x, ToMap(n.pos).y - 7 * Unit, 1, Unit, color);
        }

        void Paint(MapNode n)
        {
            nodeImages[n.id].rectTransform.localScale = n == selected ? Vector3.one * 1.25f : Vector3.one;
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
            SceneTransition.Load(selected);
        }
    }
}
