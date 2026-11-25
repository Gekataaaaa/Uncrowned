using UnityEngine;

namespace Uncrowned.Core
{
    /// <summary>Floating damage number in world space.</summary>
    public class DamageNumber : MonoBehaviour
    {
        static Font font;

        TextMesh text;
        float t;
        const float Life = 0.7f;
        Color color;

        public static void Spawn(Vector2 pos, float amount, Color color, bool big)
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var go = new GameObject("DamageNumber");
            go.transform.position = pos + new Vector2(Random.Range(-0.2f, 0.2f), 0.6f);
            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.text = Mathf.RoundToInt(amount).ToString();
            tm.fontSize = big ? 64 : 40;
            tm.characterSize = 0.05f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;
            mr.sortingOrder = 100;

            var dn = go.AddComponent<DamageNumber>();
            dn.text = tm;
            dn.color = color;
        }

        void Update()
        {
            t += Time.deltaTime;
            transform.position += Vector3.up * (1.5f * Time.deltaTime);
            text.color = new Color(color.r, color.g, color.b, 1f - t / Life);
            if (t >= Life) Destroy(gameObject);
        }
    }
}
