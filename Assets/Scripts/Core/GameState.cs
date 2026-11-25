using UnityEngine;

namespace Uncrowned.Core
{
    /// <summary>Scene names and save progress (PlayerPrefs for now).</summary>
    public static class GameState
    {
        public const string MainMenuScene = "MainMenu";
        public const string WorldMapScene = "WorldMap";
        public const string TownScene = "Town";
        public const string StageScene = "Stage";

        const string SaveKey = "uncrowned.save";
        const string ClearedPrefix = "uncrowned.cleared.";

        /// <summary>Stage the player picked on the world map.</summary>
        public static string CurrentStageId = "1-1";

        public static bool HasSave => PlayerPrefs.GetInt(SaveKey, 0) == 1;

        public static void NewGame()
        {
            foreach (var node in WorldMapData.Nodes) PlayerPrefs.DeleteKey(ClearedPrefix + node.id);
            PlayerPrefs.SetInt(SaveKey, 1);
            PlayerPrefs.Save();
        }

        public static bool IsCleared(string id) => PlayerPrefs.GetInt(ClearedPrefix + id, 0) == 1;

        public static void MarkCleared(string id)
        {
            PlayerPrefs.SetInt(ClearedPrefix + id, 1);
            PlayerPrefs.Save();
        }
    }
}
