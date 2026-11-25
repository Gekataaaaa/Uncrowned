using Uncrowned.Combat;
using Uncrowned.Core;
using UnityEngine;

namespace Uncrowned.Enemies
{
    public enum EnemyKind { Grunt, Brute, Boss }

    /// <summary>Side-view melee enemy: walks to the player, winds up (turns yellow), then hits if still in reach.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        public float speed = 2.4f;
        public float damage = 8f;
        public float reach = 1.2f;
        public float windup = 0.45f;
        public float attackCooldown = 1.2f;
        /// <summary>Bosses and brutes resist knockback.</summary>
        public float poise = 1f;
        /// <summary>Enemies stand idle along the level until the player comes this close (or hits them).</summary>
        public float aggroRange = 11f;

        bool aggro;

        public Health Health { get; private set; }
        public EnemyKind Kind { get; private set; }

        SpriteRenderer bodySr;
        Color baseColor;
        Rigidbody2D rb;
        Health target;

        float stunTimer;
        float windupTimer = -1f;
        float cooldownTimer;
        float knockX;

        public static Enemy Create(EnemyKind kind, Vector2 pos, Health target)
        {
            var go = new GameObject(kind.ToString());
            go.layer = Arena.CharacterLayer;
            go.transform.position = pos;

            Vector2 size = kind switch
            {
                EnemyKind.Brute => new Vector2(1.4f, 1.9f),
                EnemyKind.Boss => new Vector2(2.4f, 3.2f),
                _ => new Vector2(0.9f, 1.4f)
            };
            Color color = kind switch
            {
                EnemyKind.Brute => new Color(0.55f, 0.15f, 0.2f),
                EnemyKind.Boss => new Color(0.35f, 0.1f, 0.4f),
                _ => new Color(0.75f, 0.3f, 0.25f)
            };

            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(size.x, size.y, 1);
            var sr = SpriteFactory.AddRenderer(body, SpriteFactory.Square, color, 5);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.mass = kind == EnemyKind.Grunt ? 1f : 4f;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            var hp = go.AddComponent<Health>();
            hp.SetMax(kind switch { EnemyKind.Brute => 160f, EnemyKind.Boss => 900f, _ => 40f });
            hp.body = sr;

            var e = go.AddComponent<Enemy>();
            e.Kind = kind;
            e.target = target;
            if (kind == EnemyKind.Brute)
            {
                e.speed = 1.7f; e.damage = 18f; e.reach = 1.8f; e.windup = 0.7f; e.poise = 0.4f;
            }
            else if (kind == EnemyKind.Boss)
            {
                e.speed = 1.5f; e.damage = 28f; e.reach = 2.6f; e.windup = 0.9f; e.attackCooldown = 1.6f; e.poise = 0.15f;
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
            bodySr = transform.Find("Body").GetComponent<SpriteRenderer>();
            baseColor = bodySr.color;
            Health.Died += OnDied;
            Health.Damaged += _ => aggro = true;
        }

        public void Knockback(Vector2 force, float stun = 0.15f)
        {
            knockX += force.x * poise;
            if (force.y != 0) rb.linearVelocity = new Vector2(rb.linearVelocity.x, force.y * Mathf.Max(poise, 0.3f));
            stunTimer = Mathf.Max(stunTimer, stun * poise);
            if (poise >= 0.5f) CancelWindup();
        }

        /// <summary>Launch into the air; helpless until it lands. Bosses only stagger.</summary>
        public void KnockUp(float upSpeed, float stun)
        {
            if (Kind == EnemyKind.Boss) { stunTimer = Mathf.Max(stunTimer, 0.3f); return; }
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, upSpeed * (Kind == EnemyKind.Brute ? 0.7f : 1f));
            stunTimer = Mathf.Max(stunTimer, stun);
            CancelWindup();
        }

        void CancelWindup()
        {
            windupTimer = -1f;
            if (bodySr != null) bodySr.color = baseColor;
        }

        void Update()
        {
            if (Health.IsDead) return;

            cooldownTimer -= Time.deltaTime;
            if (stunTimer > 0) { stunTimer -= Time.deltaTime; return; }
            if (target == null || target.IsDead) return;

            Vector2 to = target.transform.position - transform.position;
            if (!aggro && Mathf.Abs(to.x) < aggroRange) aggro = true;
            if (!aggro) return;
            bool inReach = Mathf.Abs(to.x) <= reach && Mathf.Abs(to.y) < 2f;

            if (windupTimer >= 0)
            {
                windupTimer += Time.deltaTime;
                bodySr.color = Color.Lerp(baseColor, new Color(1f, 0.9f, 0.3f), windupTimer / windup);
                if (windupTimer >= windup)
                {
                    if (inReach || Mathf.Abs(to.x) <= reach + 0.4f && Mathf.Abs(to.y) < 2f) target.TakeDamage(damage);
                    CancelWindup();
                    cooldownTimer = attackCooldown;
                }
            }
            else if (inReach && cooldownTimer <= 0)
            {
                windupTimer = 0f;
            }
        }

        void FixedUpdate()
        {
            if (Health.IsDead) return;
            float move = 0;
            if (aggro && stunTimer <= 0 && windupTimer < 0 && target != null && !target.IsDead)
            {
                float dx = target.transform.position.x - transform.position.x;
                if (Mathf.Abs(dx) > reach * 0.75f) move = Mathf.Sign(dx) * speed;
            }
            rb.linearVelocity = new Vector2(move + knockX, rb.linearVelocity.y);
            knockX = Mathf.Lerp(knockX, 0, 8f * Time.fixedDeltaTime);

            var p = rb.position;
            float cx = Arena.ClampX(p.x);
            if (cx != p.x) { rb.position = new Vector2(cx, p.y); knockX = 0; }
        }

        void OnDied()
        {
            FadeAndDie.Spawn(SpriteFactory.Circle, new Color(1f, 0.5f, 0.3f, 0.8f), transform.position, 0, 1f, 0.3f, Kind == EnemyKind.Boss ? 5f : 2.2f);
            if (Kind == EnemyKind.Boss) { GameFeel.Shake(0.4f, 0.5f); GameFeel.HitStop(0.2f); }
            Destroy(gameObject);
        }
    }
}
