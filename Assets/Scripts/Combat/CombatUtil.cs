using System.Collections.Generic;
using Uncrowned.Enemies;
using UnityEngine;

namespace Uncrowned.Combat
{
    public static class CombatUtil
    {
        static readonly List<Collider2D> Hits = new();
        static readonly List<Enemy> Result = new();

        /// <summary>Living enemies inside a circle, optionally limited to an arc around a direction.</summary>
        public static List<Enemy> EnemiesInArc(Vector2 origin, Vector2 dir, float radius, float arcDegrees)
        {
            Result.Clear();
            Hits.Clear();
            var filter = new ContactFilter2D().NoFilter();
            Physics2D.OverlapCircle(origin, radius, filter, Hits);
            foreach (var col in Hits)
            {
                var e = col.GetComponentInParent<Enemy>();
                if (e == null || e.Health.IsDead || Result.Contains(e)) continue;
                if (arcDegrees < 360f)
                {
                    Vector2 to = (Vector2)e.transform.position - origin;
                    if (to.sqrMagnitude > 0.0001f && Vector2.Angle(dir, to) > arcDegrees * 0.5f) continue;
                }
                Result.Add(e);
            }
            return Result;
        }
    }
}
