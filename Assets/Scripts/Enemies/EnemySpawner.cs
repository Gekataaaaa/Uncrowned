using System.Collections.Generic;
using Uncrowned.Combat;
using Uncrowned.Core;
using UnityEngine;

namespace Uncrowned.Enemies
{
    /// <summary>Endless waves around the arena edge. Every third wave adds brutes.</summary>
    public class EnemySpawner : MonoBehaviour
    {
        public Health player;
        public float pauseBetweenWaves = 2.5f;

        public int Wave { get; private set; }
        public int Alive => alive.Count;

        readonly List<Enemy> alive = new();
        float pause = 1f;

        void Update()
        {
            alive.RemoveAll(e => e == null);
            if (player == null || player.IsDead || alive.Count > 0) return;

            pause -= Time.deltaTime;
            if (pause > 0) return;

            Wave++;
            int grunts = 3 + Wave * 2;
            int brutes = Wave % 3 == 0 ? Wave / 3 : 0;
            for (int i = 0; i < grunts; i++) alive.Add(Enemy.Create(EdgePoint(), player, false));
            for (int i = 0; i < brutes; i++) alive.Add(Enemy.Create(EdgePoint(), player, true));
            pause = pauseBetweenWaves;
        }

        static Vector2 EdgePoint()
        {
            var b = Arena.Bounds;
            float m = 1.5f;
            switch (Random.Range(0, 4))
            {
                case 0: return new Vector2(Random.Range(b.xMin + m, b.xMax - m), b.yMax - m);
                case 1: return new Vector2(Random.Range(b.xMin + m, b.xMax - m), b.yMin + m);
                case 2: return new Vector2(b.xMin + m, Random.Range(b.yMin + m, b.yMax - m));
                default: return new Vector2(b.xMax - m, Random.Range(b.yMin + m, b.yMax - m));
            }
        }
    }
}
