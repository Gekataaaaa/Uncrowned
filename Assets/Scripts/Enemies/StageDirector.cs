using System.Collections.Generic;
using Uncrowned.Combat;
using Uncrowned.Core;
using UnityEngine;

namespace Uncrowned.Enemies
{
    /// <summary>
    /// Linear stage, left to right. Enemy groups stand along the road and the camera scrolls freely.
    /// Only the boss arena (two screens wide) locks: once inside, the way back closes until the boss dies.
    ///
    /// Layout rules: at least `minEnemies` per stage, no more than `maxPerGroup` in one spot,
    /// groups spaced `groupSpacingScreens` screens apart.
    /// </summary>
    public class StageDirector : MonoBehaviour
    {
        public enum State { Road, BossFight, Cleared }

        public Health player;
        public string stageId = "1-1";
        /// <summary>Who lives on this stage; set before Start (defaults to the stage id's theme).</summary>
        [System.NonSerialized] public StageTheme theme;
        public string bossName => theme.bossName;

        [Header("Layout")]
        public int minEnemies = 50;
        public int maxPerGroup = 7;
        public float groupSpacingScreens = 1.5f;
        public int seed = 11;

        public float ScreenWidth { get; private set; }
        public float BossArenaStart { get; private set; }
        public float BossArenaEnd { get; private set; }
        public State Current { get; private set; } = State.Road;
        public Enemy Boss { get; private set; }
        public int TotalEnemies { get; private set; }

        readonly List<(float x, int grunts, int brutes)> groups = new();
        readonly List<Enemy> roadEnemies = new();

        public int RoadEnemiesAlive
        {
            get { roadEnemies.RemoveAll(e => e == null); return roadEnemies.Count; }
        }

        public float Progress => player == null || BossArenaStart <= 0 ? 0 : Mathf.Clamp01(player.transform.position.x / BossArenaStart);

        /// <summary>Plans the group layout. Call before building the level so its length is known.</summary>
        public void Init()
        {
            theme ??= StageTheme.For(stageId);
            var cam = Camera.main;
            ScreenWidth = cam.orthographicSize * 2f * cam.aspect;
            float spacing = ScreenWidth * groupSpacingScreens;

            var rng = new System.Random(seed);
            groups.Clear();
            int total = 0;
            float x = ScreenWidth;   // first group one screen in
            while (total < minEnemies)
            {
                int size = Mathf.Min(maxPerGroup, rng.Next(4, maxPerGroup + 1));
                // Heavier enemies appear more often deeper in the stage.
                int brutes = Mathf.Min(size - 1, rng.Next(0, 1 + groups.Count / 3));
                groups.Add((x, size - brutes, brutes));
                total += size;
                x += spacing;
            }
            TotalEnemies = total;
            BossArenaStart = x;
            BossArenaEnd = BossArenaStart + ScreenWidth * 2f;

            Arena.MinX = 0f;
            Arena.MaxX = BossArenaEnd;
        }

        void Start()
        {
            if (ScreenWidth <= 0) Init();
            var rng = new System.Random(seed + 1);
            foreach (var g in groups)
            {
                for (int i = 0; i < g.grunts; i++)
                {
                    string look = theme.grunts[rng.Next(theme.grunts.Length)];
                    bool flies = look == StageTheme.Flyer;
                    roadEnemies.Add(Enemy.Create(EnemyKind.Grunt, new Vector2(g.x + i * 1.3f, flies ? 2.4f : 1.5f), player, look, flies));
                }
                for (int i = 0; i < g.brutes; i++)
                    roadEnemies.Add(Enemy.Create(EnemyKind.Brute, new Vector2(g.x + g.grunts * 1.3f + 1f + i * 1.8f, 2f), player, theme.brute));
            }
        }

        void Update()
        {
            if (player == null || player.IsDead) return;

            switch (Current)
            {
                case State.Road:
                    if (player.transform.position.x > BossArenaStart + 2f)
                    {
                        Current = State.BossFight;
                        Arena.MinX = BossArenaStart; // the gate closes behind the player
                        Boss = Enemy.Create(EnemyKind.Boss, new Vector2(BossArenaEnd - 4f, 3f), player, theme.boss);
                        Boss.aggroRange = 999f;
                        GameFeel.Shake(0.25f, 0.4f);
                    }
                    break;

                case State.BossFight:
                    if (Boss == null)
                    {
                        Current = State.Cleared;
                        Arena.MinX = 0f;
                        GameState.MarkCleared(stageId);
                    }
                    break;
            }
        }
    }
}
