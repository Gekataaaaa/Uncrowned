using UnityEngine;

namespace Uncrowned.Core
{
    /// <summary>A decoration placed along a stage: sprite path and its pivot in pixels from the top-left (null = feet).</summary>
    public readonly struct PropArt
    {
        public readonly string path;
        public readonly Vector2? pivot;
        public PropArt(string path, Vector2? pivot = null) { this.path = path; this.pivot = pivot; }
    }

    /// <summary>
    /// How a stage looks and who lives there: backdrop folder in Resources/Art/Backgrounds, props and the
    /// enemy sprites (Resources/Art/Mobs). Every stage ends in the boss arena (Backgrounds/1-5_BossArena).
    /// </summary>
    public class StageTheme
    {
        public const string BossArena = "Backgrounds/1-5_BossArena";
        public const string Flyer = "despair_wisp";

        public string folder;
        public PropArt[] groundProps;
        public PropArt ceilingProp;
        public PropArt wallProp;
        public string[] grunts;
        public string brute;
        public string boss;
        public string bossName;

        static readonly PropArt VioletSmall = new("Backgrounds/Props/crystal_violet_small", new Vector2(13, 16));
        static readonly PropArt VioletLarge = new("Backgrounds/Props/crystal_violet_large", new Vector2(17, 22));
        static readonly PropArt TealSmall = new("Backgrounds/Props/crystal_teal_small", new Vector2(14, 18));
        static readonly PropArt TealLarge = new("Backgrounds/Props/crystal_teal_large", new Vector2(17, 22));

        public static StageTheme For(string stageId) => stageId switch
        {
            "1-2" => new StageTheme
            {
                folder = "Backgrounds/1-2_WeepingHollows",
                groundProps = new[] { TealSmall, TealLarge, new PropArt("Backgrounds/Props/puddle", new Vector2(22, 0)) },
                wallProp = new PropArt("Backgrounds/Props/weeping_face"),
                grunts = new[] { "weeping_sentinel", Flyer, "chainbound_ghoul" },
                brute = "abyssal_hulk",
                // Placeholder until 1-5 is built: lets the Sovereign be fought now.
                boss = "eyeless_sovereign",
                bossName = "The Eyeless Sovereign",
            },
            _ => new StageTheme
            {
                folder = "Backgrounds/1-1_PitOfExiles",
                groundProps = new[] { VioletSmall, VioletLarge, new PropArt("Backgrounds/Props/skull_pile") },
                ceilingProp = new PropArt("Backgrounds/Props/hanging_cage", new Vector2(12, 0)),
                grunts = new[] { "hollow_crawler", "gloomling", "chainbound_ghoul", Flyer },
                brute = "abyssal_hulk",
                boss = "the_jailer",
                bossName = "The Jailer",
            },
        };
    }
}
