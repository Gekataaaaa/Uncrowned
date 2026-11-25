using Uncrowned.Combat;
using Uncrowned.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Uncrowned.Player
{
    /// <summary>WASD movement and mouse aim. Combat code drives speed multipliers and forced moves (dash).</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        public float speed = 5f;

        public Vector2 Aim { get; private set; } = Vector2.right;
        /// <summary>Multiplier set by combat (slower while swinging, 0 while rooted).</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        /// <summary>When set, overrides input movement (used by dash).</summary>
        public Vector2? ForcedVelocity { get; set; }

        Rigidbody2D rb;
        Health health;
        Transform sword;
        Vector2 input;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
        }

        void Start()
        {
            sword = transform.Find("Sword");
        }

        void Update()
        {
            if (health.IsDead) { input = Vector2.zero; return; }

            var kb = Keyboard.current;
            input = Vector2.zero;
            if (kb != null)
            {
                if (kb.wKey.isPressed) input.y += 1;
                if (kb.sKey.isPressed) input.y -= 1;
                if (kb.aKey.isPressed) input.x -= 1;
                if (kb.dKey.isPressed) input.x += 1;
            }
            input = Vector2.ClampMagnitude(input, 1f);

            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse != null && cam != null)
            {
                Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
                Vector2 dir = world - (Vector2)transform.position;
                if (dir.sqrMagnitude > 0.01f) Aim = dir.normalized;
            }

            if (sword != null)
            {
                float angle = Mathf.Atan2(Aim.y, Aim.x) * Mathf.Rad2Deg;
                sword.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        void FixedUpdate()
        {
            rb.linearVelocity = ForcedVelocity ?? input * (speed * SpeedMultiplier);
            rb.position = Arena.Clamp(rb.position);
        }
    }
}
