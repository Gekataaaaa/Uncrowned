using UnityEngine;

namespace Uncrowned.Core
{
    public enum NodeKind { Stage, Town, Kingdom }

    public class MapNode
    {
        public string id;
        public string title;
        public string description;
        public NodeKind kind;
        /// <summary>Normalized map position, (0,0) bottom-left, (1,1) top-right.</summary>
        public Vector2 pos;
        /// <summary>Node that must be cleared first (null = open from the start).</summary>
        public string requires;
        /// <summary>False for content that is not built yet.</summary>
        public bool playable;
    }

    /// <summary>
    /// The road out of the Abyss, from the depths at the bottom of the map to Etheraion at the top.
    /// </summary>
    public static class WorldMapData
    {
        public static readonly MapNode[] Nodes =
        {
            new() { id = "1-1", title = "1-1  The Pit of Exiles", kind = NodeKind.Stage, pos = new(0.12f, 0.06f), playable = true,
                description = "Where the kingdom throws what it fears. You wake here, bruised and barefoot, with a rusted blade at your side." },
            new() { id = "town", title = "Lastlight", kind = NodeKind.Town, pos = new(0.3f, 0.08f), requires = "1-1", playable = true,
                description = "A hidden refuge in the depths of the Abyss. A fire, a forge, and people who don't ask about your eyes." },
            new() { id = "1-2", title = "1-2  Weeping Hollows", kind = NodeKind.Stage, pos = new(0.44f, 0.17f), requires = "1-1", playable = true,
                description = "Caverns that cry in borrowed voices. The Sentinels keep their silent watch." },
            new() { id = "1-3", title = "1-3  Chasm of Echoes", kind = NodeKind.Stage, pos = new(0.24f, 0.27f), requires = "1-2",
                description = "Every step answers back. Something large moves below." },
            new() { id = "1-4", title = "1-4  The Jailer's Descent", kind = NodeKind.Stage, pos = new(0.42f, 0.33f), requires = "1-3",
                description = "Chains, keys and the warden who never lets an exile go." },
            new() { id = "1-5", title = "1-5  Throne of the Eyeless", kind = NodeKind.Stage, pos = new(0.6f, 0.4f), requires = "1-4",
                description = "The Eyeless Sovereign rules the Abyss and hoards the eyes of the damned. He wants yours." },
            new() { id = "2-1", title = "2-1  The Ashen Wilds", kind = NodeKind.Stage, pos = new(0.58f, 0.64f), requires = "1-5",
                description = "Burnt forests between the Abyss and the old border." },
            new() { id = "2-2", title = "2-2  Shattered March", kind = NodeKind.Stage, pos = new(0.4f, 0.74f), requires = "2-1",
                description = "The frontier forts that once guarded your father's realm." },
            new() { id = "etheraion", title = "Kingdom of Etheraion", kind = NodeKind.Kingdom, pos = new(0.5f, 0.9f), requires = "2-2",
                description = "Your birthright. Those who cast you down still sit on the throne. Take back the crown." },
        };

        public static MapNode Get(string id)
        {
            foreach (var n in Nodes) if (n.id == id) return n;
            return null;
        }

        /// <summary>The town that clearing this stage opens, if any: the hero walks straight there.</summary>
        public static MapNode TownAfter(string stageId)
        {
            foreach (var n in Nodes) if (n.kind == NodeKind.Town && n.requires == stageId) return n;
            return null;
        }

        public static bool IsUnlocked(MapNode n) => n.requires == null || GameState.IsCleared(n.requires);
    }
}
