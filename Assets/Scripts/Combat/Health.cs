using System;
using System.Collections;
using Uncrowned.Core;
using UnityEngine;

namespace Uncrowned.Combat
{
    public class Health : MonoBehaviour
    {
        public float max = 100f;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;
        public bool Invulnerable { get; set; }

        /// <summary>Visual body to flash on hit (defaults to the first SpriteRenderer found).</summary>
        public SpriteRenderer body;
        public Color numberColor = Color.white;

        public event Action<float> Damaged;
        public event Action Died;

        Color baseColor;
        Coroutine flash;

        void Awake()
        {
            Current = max;
        }

        void Start()
        {
            // Read in Start: builders assign `body` right after AddComponent, after Awake ran.
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
            if (body != null) baseColor = body.color;
        }

        /// <summary>Use when configuring right after AddComponent (Awake has already run).</summary>
        public void SetMax(float value)
        {
            max = value;
            Current = value;
        }

        public void TakeDamage(float amount, bool big = false)
        {
            if (IsDead || Invulnerable || amount <= 0) return;
            Current = Mathf.Max(0, Current - amount);
            DamageNumber.Spawn(transform.position, amount, numberColor, big);
            if (body != null)
            {
                if (flash != null) StopCoroutine(flash);
                flash = StartCoroutine(Flash());
            }
            Damaged?.Invoke(amount);
            if (IsDead) Died?.Invoke();
        }

        IEnumerator Flash()
        {
            body.color = new Color(1f, 0.45f, 0.45f); // red tint works on sprites and flat shapes
            yield return new WaitForSeconds(0.07f);
            body.color = baseColor;
            flash = null;
        }
    }
}
