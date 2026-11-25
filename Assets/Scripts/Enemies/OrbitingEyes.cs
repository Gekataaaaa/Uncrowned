using Uncrowned.Core;
using UnityEngine;

namespace Uncrowned.Enemies
{
    /// <summary>The Eyeless Sovereign's hoard: stolen eyes circling him. Purely visual.</summary>
    public class OrbitingEyes : MonoBehaviour
    {
        public int count = 6;
        public Vector2 radius = new(3.25f, 2.1f);
        public Vector2 center;
        public float speed = 0.6f;

        Transform[] eyes;

        void Start()
        {
            eyes = new Transform[count];
            for (int i = 0; i < count; i++)
                eyes[i] = ArtKit.Place(transform, "Mobs/sovereign_floating_eye", Vector2.zero, 6).transform;
        }

        void Update()
        {
            for (int i = 0; i < count; i++)
            {
                float a = Time.time * speed + i * Mathf.PI * 2f / count;
                eyes[i].localPosition = center + new Vector2(Mathf.Cos(a) * radius.x, Mathf.Sin(a) * radius.y);
            }
        }
    }
}
