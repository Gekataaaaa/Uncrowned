using Uncrowned.Combat;
using Uncrowned.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Uncrowned.Player
{
    /// <summary>Side-view movement: A/D to walk, Space to jump. Facing follows the last walk direction.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        public float speed = 5.5f;
        public float jumpSpeed = 13f;

        public int Facing { get; private set; } = 1;
        public Vector2 Aim => new Vector2(Facing, 0);
        public bool Grounded { get; private set; }

        /// <summary>Multiplier set by combat (slower while swinging, 0 while rooted).</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        /// <summary>When set, overrides input movement (used by dash).</summary>
        public Vector2? ForcedVelocity { get; set; }
        /// <summary>Combat locks facing mid-swing so attacks don't flip direction.</summary>
        public bool FacingLocked { get; set; }

        Rigidbody2D rb;
        Health health;
        Transform visual;
        float moveX;
        bool jumpQueued;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
        }

        void Start()
        {
            visual = transform.Find("Visual");
        }

        void Update()
        {
            moveX = 0;
            if (health.IsDead) return;

            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveX -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveX += 1;
            if ((kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) && Grounded && SpeedMultiplier > 0) jumpQueued = true;

            if (!FacingLocked && moveX != 0) Facing = moveX > 0 ? 1 : -1;
            if (visual != null) visual.localScale = new Vector3(Facing, 1, 1);
        }

        void FixedUpdate()
        {
            var hit = Physics2D.Raycast(rb.position, Vector2.down, 0.85f, 1 << 0);
            Grounded = hit.collider != null && rb.linearVelocity.y <= 0.1f;

            if (ForcedVelocity.HasValue)
            {
                rb.linearVelocity = ForcedVelocity.Value;
            }
            else
            {
                float vy = rb.linearVelocity.y;
                if (jumpQueued) { vy = jumpSpeed; jumpQueued = false; }
                rb.linearVelocity = new Vector2(moveX * speed * SpeedMultiplier, vy);
            }

            var p = rb.position;
            float cx = Arena.ClampX(p.x);
            if (cx != p.x) rb.position = new Vector2(cx, p.y);
        }
    }
}
