using System.Collections.Generic;
using Uncrowned.Combat;
using Uncrowned.Core;
using UnityEngine;

namespace Uncrowned.Enemies
{
    /// <summary>
    /// Linear stage, left to right. Enemy groups stand along the road and the camera scrolls freely.
    /// Only the boss arena (two screens wide) locks: once inside, the way back closes until the boss dies.
    /// </summary>
    public class StageDirector : MonoBehaviour
    {
        public enum State { Road, BossFight, Cleared }

        public Health player;
        public string stageName = "1-1";

        /// <summary>Groups placed along the road: x position and composition.</summary>
        public readonly List<(float x, int grunts, int brutes)> groups = new()
        {
            (14f, 2, 0),
            (26f, 3, 0),
            (40f, 2, 1),
            (54f, 4, 0),
            (68f, 3, 1),
            (82f, 2, 2),
        };

        public float bossArenaStart = 96f;
        public float BossArenaEnd { get; private set; }
        public State Current { get; private set; } = State.Road;
        public Enemy Boss { get; private set; }

        readonly List<Enemy> roadEnemies = new();

        public int RoadEnemiesAlive
        {
            get { roadEnemies.RemoveAll(e => e == null); return roadEnemies.Count; }
        }

        public float Progress => player == null ? 0 : Mathf.Clamp01(player.transform.position.x / bossArenaStart);

        void Start()
        {
            var cam = Camera.main;
            float screenW = cam.orthographicSize * 2f * cam.aspect;
            BossArenaEnd = bossArenaStart + screenW * 2f;

            Arena.MinX = 0f;
            Arena.MaxX = BossArenaEnd;

            foreach (var g in groups)
            {
                for (int i = 0; i < g.grunts; i++) roadEnemies.Add(Enemy.Create(EnemyKind.Grunt, new Vector2(g.x + i * 1.3f, 1.5f), player));
                for (int i = 0; i < g.brutes; i++) roadEnemies.Add(Enemy.Create(EnemyKind.Brute, new Vector2(g.x + g.grunts * 1.3f + 1f + i * 1.8f, 2f), player));
            }
        }

        void Update()
        {
            if (player == null || player.IsDead) return;

            switch (Current)
            {
                case State.Road:
                    if (player.transform.position.x > bossArenaStart + 2f)
                    {
                        Current = State.BossFight;
                        Arena.MinX = bossArenaStart; // the gate closes behind the player
                        Boss = Enemy.Create(EnemyKind.Boss, new Vector2(BossArenaEnd - 4f, 3f), player);
                        Boss.aggroRange = 999f;
                        GameFeel.Shake(0.25f, 0.4f);
                    }
                    break;

                case State.BossFight:
                    if (Boss == null)
                    {
                        Current = State.Cleared;
                        Arena.MinX = 0f;
                    }
                    break;
            }
        }
    }
}
