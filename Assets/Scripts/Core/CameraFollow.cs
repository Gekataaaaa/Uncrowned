using UnityEngine;

namespace Uncrowned.Core
{
    /// <summary>Side-view follow camera: tracks X, stays inside the walkable limits, light Y follow on jumps.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smooth = 8f;
        public float baseY = 4f;

        Camera cam;
        float shakeStrength, shakeTime, shakeDuration;
        Vector3 basePos;

        void Awake()
        {
            cam = GetComponent<Camera>();
            basePos = transform.position;
        }

        public void Shake(float strength, float duration)
        {
            if (strength < shakeStrength && shakeTime > 0) return;
            shakeStrength = strength;
            shakeDuration = duration;
            shakeTime = duration;
        }

        public void SnapTo(Vector2 p)
        {
            basePos = new Vector3(p.x, baseY, basePos.z);
        }

        void LateUpdate()
        {
            if (target != null)
            {
                float halfW = cam.orthographicSize * cam.aspect;
                float x = target.position.x + 2f;
                float min = Arena.MinX + halfW, max = Arena.MaxX - halfW;
                x = min <= max ? Mathf.Clamp(x, min, max) : (Arena.MinX + Arena.MaxX) * 0.5f;
                float y = baseY + Mathf.Max(0, target.position.y - 2f) * 0.5f;
                var goal = new Vector3(x, y, basePos.z);
                basePos = Vector3.Lerp(basePos, goal, 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime));
            }

            Vector3 offset = Vector3.zero;
            if (shakeTime > 0)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float k = shakeDuration > 0 ? shakeTime / shakeDuration : 0;
                offset = (Vector3)(Random.insideUnitCircle * shakeStrength * k);
            }
            transform.position = basePos + offset;
        }
    }
}
