using System.Collections;
using UnityEngine;

namespace Uncrowned.Core
{
    /// <summary>Hit-stop (short freeze on impact) and camera shake entry points.</summary>
    public class GameFeel : MonoBehaviour
    {
        public static GameFeel Instance { get; private set; }

        CameraFollow cam;
        Coroutine hitStop;

        void Awake()
        {
            Instance = this;
            cam = FindAnyObjectByType<CameraFollow>();
        }

        public static void Shake(float strength, float duration)
        {
            if (Instance != null && Instance.cam != null) Instance.cam.Shake(strength, duration);
        }

        public static void HitStop(float seconds)
        {
            if (Instance == null) return;
            if (Instance.hitStop != null) Instance.StopCoroutine(Instance.hitStop);
            Instance.hitStop = Instance.StartCoroutine(Instance.HitStopRoutine(seconds));
        }

        IEnumerator HitStopRoutine(float seconds)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(seconds);
            Time.timeScale = 1f;
            hitStop = null;
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (Instance == this) Instance = null;
        }
    }
}
