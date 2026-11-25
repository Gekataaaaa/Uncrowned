using UnityEngine;

namespace Uncrowned.Core
{
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smooth = 10f;

        float shakeStrength;
        float shakeTime;
        float shakeDuration;
        Vector3 basePos;

        void Start()
        {
            basePos = transform.position;
        }

        public void Shake(float strength, float duration)
        {
            if (strength < shakeStrength && shakeTime > 0) return;
            shakeStrength = strength;
            shakeDuration = duration;
            shakeTime = duration;
        }

        void LateUpdate()
        {
            if (target != null)
            {
                var goal = new Vector3(target.position.x, target.position.y, basePos.z);
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
