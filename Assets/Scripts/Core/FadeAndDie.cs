using UnityEngine;

namespace Uncrowned.Core
{
    /// <summary>Short-lived visual: fades a sprite out, optionally growing, then destroys itself.</summary>
    public class FadeAndDie : MonoBehaviour
    {
        public float life = 0.2f;
        public float growTo = 1f;

        SpriteRenderer sr;
        float t;
        Color start;
        Vector3 startScale;

        void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            start = sr != null ? sr.color : Color.white;
            startScale = transform.localScale;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / life);
            if (sr != null) sr.color = new Color(start.r, start.g, start.b, start.a * (1f - k));
            transform.localScale = Vector3.Lerp(startScale, startScale * growTo, k);
            if (t >= life) Destroy(gameObject);
        }

        public static void Spawn(Sprite sprite, Color color, Vector2 pos, float angleDeg, float scale, float life, float growTo = 1f, int order = 20)
        {
            var go = new GameObject("Fx_" + sprite.name);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, 0, angleDeg));
            go.transform.localScale = Vector3.one * scale;
            SpriteFactory.AddRenderer(go, sprite, color, order);
            var f = go.AddComponent<FadeAndDie>();
            f.life = life;
            f.growTo = growTo;
        }
    }
}
