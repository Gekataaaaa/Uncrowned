using UnityEngine;

namespace Uncrowned.Core
{
    /// <summary>Rectangular play area. Everything that moves is clamped inside it.</summary>
    public static class Arena
    {
        public static Rect Bounds = new Rect(-15f, -10f, 30f, 20f);

        public static Vector2 Clamp(Vector2 p, float margin = 0.5f)
        {
            return new Vector2(
                Mathf.Clamp(p.x, Bounds.xMin + margin, Bounds.xMax - margin),
                Mathf.Clamp(p.y, Bounds.yMin + margin, Bounds.yMax - margin));
        }
    }
}
