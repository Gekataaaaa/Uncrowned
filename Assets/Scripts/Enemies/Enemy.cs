using Uncrowned.Combat;
using Uncrowned.Core;
using UnityEngine;

namespace Uncrowned.Enemies
{
    /// <summary>Melee chaser: walks to the player, winds up, then hits if still in reach.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        public float speed = 2.2f;
        public float damage = 8f;
        public float reach = 1.0f;
        public float windup = 0.45f;
        public float attackCooldown = 1.2f;

        public Health Health { get; private set; }

        Transform body;
        SpriteRenderer bodySr;
        Color baseColor;
        Rigidbody2D rb;
        Health target;

        float stunTimer;
        float airTimer, airDuration;
        float windupTimer = -1f;
        float cooldownTimer;
        Vector2 knock;

        public static Enemy Create(Vector2 pos, Health target, bool brute)
        {
            var go = new GameObject(brute ? "Brute" : "Grunt");
            go.transform.position = pos;

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(go.transform, false);
            float size = brute ? 1.5f : 0.9f;
            bodyGo.transform.localScale = Vector3.one * size;
            var color = brute ? new Color(0.55f, 0.15f, 0.2f) : new Color(0.75f, 0.3f, 0.25f);
            var sr = SpriteFactory.AddRenderer(bodyGo, SpriteFactory.Square, color, 5);

            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(go.transform, false);
            shadow.transform.localPosition = new Vector3(0, -size * 0.5f, 0);
            shadow.transform.localScale = new Vector3(size, size * 0.35f, 1);
            SpriteFactory.AddRenderer(shadow, SpriteFactory.Circle, new Color(0, 0, 0, 0.35f), 1);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            rb.linearDamping = 6f;
            rb.mass = brute ? 4f : 1f;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = size * 0.45f;

            var hp = go.AddComponent<Health>();
            hp.SetMax(brute ? 160f : 40f);
            hp.body = sr;

            var e = go.AddComponent<Enemy>();
            e.target = target;
            if (brute)
            {
                e.speed = 1.5f;
                e.damage = 18f;
                e.reach = 1.4f;
                e.windup = 0.7f;
            }
            return e;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            Health = GetComponent<Health>();
        }

        void Start()
        {
            body = transform.Find("Body");
            bodySr = body.GetComponent<SpriteRenderer>();
            baseColor = bodySr.color;
            Health.Died += OnDied;
        }

        public void Knockback(Vector2 force, float stun = 0.15f)
        {
            knock += force / rb.mass;
            stunTimer = Mathf.Max(stunTimer, stun);
            CancelWindup();
        }

        /// <summary>Launch into the air: helpless for the duration.</summary>
        public void KnockUp(float duration)
        {
            airDuration = airTimer = duration;
            stunTimer = Mathf.Max(stunTimer, duration);
            CancelWindup();
        }

        public bool IsAirborne => airTimer > 0;

        void CancelWindup()
        {
            windupTimer = -1f;
            if (bodySr != null) bodySr.color = baseColor;
        }

        void Update()
        {
            if (Health.IsDead) return;

            // Visual hop while airborne.
            if (airTimer > 0)
            {
                airTimer -= Time.deltaTime;
                float k = 1f - Mathf.Clamp01(airTimer / airDuration);
                body.localPosition = new Vector3(0, Mathf.Sin(k * Mathf.PI) * 1.2f, 0);
            }
            else body.localPosition = Vector3.zero;

            cooldownTimer -= Time.deltaTime;
            if (stunTimer > 0) { stunTimer -= Time.deltaTime; return; }
            if (target == null || target.IsDead) return;

            float dist = Vector2.Distance(transform.position, target.transform.position);

            if (windupTimer >= 0)
            {
                windupTimer += Time.deltaTime;
                bodySr.color = Color.Lerp(baseColor, new Color(1f, 0.9f, 0.3f), windupTimer / windup);
                if (windupTimer >= windup)
                {
                    if (dist <= reach + 0.3f) target.TakeDamage(damage);
                    CancelWindup();
                    cooldownTimer = attackCooldown;
                }
            }
            else if (dist <= reach && cooldownTimer <= 0)
            {
                windupTimer = 0f;
            }
        }

        void FixedUpdate()
        {
            if (Health.IsDead) return;
            Vector2 move = Vector2.zero;
            bool canMove = stunTimer <= 0 && windupTimer < 0 && target != null && !target.IsDead;
            if (canMove)
            {
                Vector2 to = target.transform.position - transform.position;
                if (to.magnitude > reach * 0.8f) move = to.normalized * speed;
            }
            rb.linearVelocity = move + knock;
            knock = Vector2.Lerp(knock, Vector2.zero, 10f * Time.fixedDeltaTime);
            rb.position = Arena.Clamp(rb.position);
        }

        void OnDied()
        {
            FadeAndDie.Spawn(SpriteFactory.Circle, new Color(1f, 0.5f, 0.3f, 0.8f), transform.position, 0, 1f, 0.3f, 2.2f);
            Destroy(gameObject);
        }
    }
}
