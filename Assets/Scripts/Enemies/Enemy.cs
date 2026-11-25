using Uncrowned.Combat;
using Uncrowned.Core;
using UnityEngine;

namespace Uncrowned.Enemies
{
    public enum EnemyKind { Grunt, Brute, Boss }

    /// <summary>
    /// Side-view melee enemy: walks to the player, winds up (turns yellow), then hits if still in reach.
    /// Drawn with a sprite from Resources/Art/Mobs (they face left and are flipped toward the player).
    /// </summary>
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
        bool flying;
        float hoverY;   // flyers drift back to this height after a hit sends them up or down
        float bodyY;
        Transform shadow;
        SpriteRenderer shadowSr;
        Vector3 shadowScale;
        Rigidbody2D rb;
        Health target;

        float stunTimer;
        float windupTimer = -1f;
        float cooldownTimer;
        float knockX;
        float hitFlash;   // seconds left of the red flash after a hit
        const float HitFlashTime = 0.1f;
        static readonly Color HitColor = new(1f, 0.3f, 0.3f);
        static readonly Color WindupColor = new(1f, 0.9f, 0.3f);

        /// <param name="look">Sprite name in Resources/Art/Mobs. Its size sets the collider.</param>
        /// <param name="flying">Hovers at its spawn height instead of falling to the ground.</param>
        public static Enemy Create(EnemyKind kind, Vector2 pos, Health target, string look, bool flying = false)
        {
            var go = new GameObject(kind + " " + look);
            go.layer = Arena.CharacterLayer;
            go.transform.position = pos;

            var art = ArtKit.Sprite("Mobs/" + look);
            Vector2 px = art != null ? art.rect.size : new Vector2(16, 24);
            // The outline and the empty top of a sprite should not block hits: the collider is a bit smaller.
            var size = new Vector2(px.x * 0.6f, px.y * 0.8f) / ArtKit.PPU;

            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0, -size.y / 2f, 0);   // sprite pivot is at the feet
            var sr = SpriteFactory.AddRenderer(body, art, Color.white, 5);

            // Same soft ellipse as the hero's, sized to the mob. The Sovereign floats in its own light: no shadow.
            Transform shadow = null;
            if (look != "eyeless_sovereign")
            {
                var sh = new GameObject("Shadow");
                sh.transform.SetParent(go.transform, false);
                sh.transform.localPosition = new Vector3(0, -size.y / 2f, 0);
                float w = Mathf.Max(size.x * 1.15f, 0.7f);
                sh.transform.localScale = new Vector3(w, w * 0.25f, 1);
                SpriteFactory.AddRenderer(sh, SpriteFactory.Circle, new Color(0, 0, 0, 0.35f), 1);
                shadow = sh.transform;
            }

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = flying ? 0f : 3f;
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
            e.flying = flying;
            e.hoverY = pos.y;
            e.shadow = shadow;
            e.bodyY = body.transform.localPosition.y;
            if (look == "eyeless_sovereign")
                go.AddComponent<OrbitingEyes>().center = new Vector2(0, e.bodyY + 2.75f);
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
            if (shadow != null) { shadowSr = shadow.GetComponent<SpriteRenderer>(); shadowScale = shadow.localScale; }
            Health.Died += OnDied;
            Health.Damaged += _ => { aggro = true; hitFlash = HitFlashTime; };
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
        }

        /// <summary>
        /// Body tint: yellow while winding up, a short red flash on a hit on top of it.
        /// The shadow stays on the ground; it shrinks and fades as the mob rises (flyers, knock-ups).
        /// </summary>
        void LateUpdate()
        {
            var c = windupTimer >= 0 ? Color.Lerp(baseColor, WindupColor, windupTimer / windup) : baseColor;
            if (hitFlash > 0)
            {
                hitFlash -= Time.deltaTime;
                c = Color.Lerp(c, HitColor, Mathf.Clamp01(hitFlash / HitFlashTime));
            }
            bodySr.color = c;

            if (shadow == null) return;
            float feet = transform.position.y + bodyY;
            float height = Mathf.Max(0, feet - Arena.GroundY);
            float k = Mathf.Clamp01(1f - height / 4f);
            shadow.position = new Vector3(transform.position.x, Mathf.Min(feet, Arena.GroundY), 0);
            shadow.localScale = shadowScale * Mathf.Lerp(0.5f, 1f, k);
            shadowSr.color = new Color(0, 0, 0, Mathf.Lerp(0.12f, 0.35f, k));
        }

        void Update()
        {
            if (Health.IsDead) return;

            cooldownTimer -= Time.deltaTime;
            if (stunTimer > 0) { stunTimer -= Time.deltaTime; return; }
            if (target == null || target.IsDead) return;

            Vector2 to = target.transform.position - transform.position;
            if (Mathf.Abs(to.x) > 0.1f) bodySr.flipX = to.x > 0;   // sprites face left
            if (flying)
                bodySr.transform.localPosition = new Vector3(0, bodyY + Mathf.Sin(Time.time * 3f + transform.position.x) * 0.15f, 0);
            if (!aggro && Mathf.Abs(to.x) < aggroRange) aggro = true;
            if (!aggro) return;
            bool inReach = Mathf.Abs(to.x) <= reach && Mathf.Abs(to.y) < 2f;

            if (windupTimer >= 0)
            {
                windupTimer += Time.deltaTime;
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
            float vy = rb.linearVelocity.y;
            // No gravity pulls a flyer back after a launch, so steer it back to its hover height.
            if (flying) vy = Mathf.Lerp(vy, (hoverY - rb.position.y) * 4f, 6f * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(move + knockX, vy);
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
