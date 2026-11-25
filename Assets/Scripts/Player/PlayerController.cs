using Uncrowned.Combat;
using Uncrowned.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Uncrowned.Player
{
    /// <summary>
    /// Side-view movement: A/D to walk, Space to jump. The warrior always faces the mouse
    /// and skills are aimed at the cursor; walking never turns him around.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        public float speed = 5.5f;
        public float jumpSpeed = 13f;

        /// <summary>+1 when the mouse is to the right, -1 when to the left.</summary>
        public int Facing { get; private set; } = 1;
        /// <summary>Normalized direction from the warrior to the mouse cursor.</summary>
        public Vector2 Aim { get; private set; } = Vector2.right;
        public bool Grounded { get; private set; }

        /// <summary>Multiplier set by combat (slower while swinging, 0 while rooted).</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        /// <summary>When set, overrides input movement (used by dash).</summary>
        public Vector2? ForcedVelocity { get; set; }

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
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveX -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveX += 1;
                if ((kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) && Grounded && SpeedMultiplier > 0) jumpQueued = true;
            }

            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse != null && cam != null)
            {
                Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
                Vector2 dir = world - (Vector2)transform.position;
                if (dir.sqrMagnitude > 0.01f) Aim = dir.normalized;
            }

            if (!FacingLocked) FaceAim();
        }

        /// <summary>While true the warrior keeps his current facing (set by combat for the length of an action animation).</summary>
        public bool FacingLocked { get; set; }

        /// <summary>Turns the warrior toward the cursor right now, ignoring the lock.</summary>
        public void FaceAim()
        {
            if (Mathf.Abs(Aim.x) > 0.02f) Facing = Aim.x > 0 ? 1 : -1;
            if (visual != null) visual.localScale = new Vector3(Facing, 1, 1);
        }

        void FixedUpdate()
        {
            var hit = Physics2D.Raycast(rb.position, Vector2.down, PlayerFactory.HalfHeight + 0.1f, 1 << 0);
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
