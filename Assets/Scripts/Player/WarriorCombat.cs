using System.Collections;
using Uncrowned.Combat;
using Uncrowned.Core;
using Uncrowned.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Uncrowned.Player
{
    public enum Ability { Combo, Dash, WideSwing, WarCry, Ultimate }

    /// <summary>
    /// Greatsword warrior kit. Every hit builds Valor; the ultimate "Crown's Judgement"
    /// launches nearby enemies, then slams for bonus damage per stored charge and consumes them.
    /// Numbers are first-pass placeholders for tuning.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(Valor), typeof(Health))]
    public class WarriorCombat : MonoBehaviour
    {
        [Header("Combo (LMB)")]
        public float[] comboDamage = { 10f, 10f, 16f };
        public float comboRange = 1.7f;
        public float comboArc = 110f;
        public float comboFinisherArc = 170f;
        public float comboStep = 0.28f;
        public float comboWindow = 0.8f;

        [Header("Dash (Space)")]
        public float dashDamage = 12f;
        public float dashDistance = 4.5f;
        public float dashTime = 0.16f;
        public float dashCooldown = 4f;
        public int dashValor = 2;

        [Header("Wide Swing (RMB)")]
        public float wideDamage = 14f;
        public float wideRange = 2.3f;
        public float wideCooldown = 3f;

        [Header("War Cry (Q)")]
        public float warCryDuration = 6f;
        public float warCryAttackSpeed = 1.2f;
        public float warCryCooldown = 15f;

        [Header("Crown's Judgement (R)")]
        public float ultLaunchDamage = 15f;
        public float ultLaunchRadius = 3f;
        public float ultSlamBaseDamage = 30f;
        public float ultBonusPerCharge = 0.15f;
        public float ultSlamRadius = 3.3f;
        public float ultDelay = 0.45f;
        public float ultCooldown = 18f;

        public Valor Valor { get; private set; }
        public bool WarCryActive => warCryTimer > 0;

        PlayerController controller;
        Health health;
        Collider2D bodyCollider;

        readonly float[] cooldown = new float[5];
        readonly float[] cooldownMax = new float[5];
        float busy;
        int comboIndex;
        float comboTimer;
        float warCryTimer;
        bool ulting;

        float AttackSpeed => WarCryActive ? warCryAttackSpeed : 1f;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            Valor = GetComponent<Valor>();
            health = GetComponent<Health>();
            bodyCollider = GetComponent<Collider2D>();
            cooldownMax[(int)Ability.Dash] = dashCooldown;
            cooldownMax[(int)Ability.WideSwing] = wideCooldown;
            cooldownMax[(int)Ability.WarCry] = warCryCooldown;
            cooldownMax[(int)Ability.Ultimate] = ultCooldown;
        }

        /// <summary>0 = ready, 1 = just used.</summary>
        public float CooldownFraction(Ability a)
        {
            float max = cooldownMax[(int)a];
            return max <= 0 ? 0 : Mathf.Clamp01(cooldown[(int)a] / max);
        }

        public float CooldownRemaining(Ability a) => Mathf.Max(0, cooldown[(int)a]);

        void Update()
        {
            for (int i = 0; i < cooldown.Length; i++) cooldown[i] -= Time.deltaTime;
            busy -= Time.deltaTime;
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0) comboIndex = 0;

            if (warCryTimer > 0)
            {
                warCryTimer -= Time.deltaTime;
                if (warCryTimer <= 0) Valor.DecayFrozen = false;
            }

            if (!ulting) controller.SpeedMultiplier = busy > 0 ? 0.35f : 1f;
            if (health.IsDead || ulting) return;

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            if (kb.rKey.wasPressedThisFrame && Ready(Ability.Ultimate)) { StartCoroutine(Ultimate()); return; }
            if (kb.spaceKey.wasPressedThisFrame && Ready(Ability.Dash)) { StartCoroutine(Dash()); return; }
            if (kb.qKey.wasPressedThisFrame && Ready(Ability.WarCry)) WarCry();
            if (busy > 0) return;
            if (mouse.rightButton.wasPressedThisFrame && Ready(Ability.WideSwing)) { WideSwing(); return; }
            if (mouse.leftButton.isPressed) ComboHit();
        }

        bool Ready(Ability a) => cooldown[(int)a] <= 0;

        void StartCooldown(Ability a) => cooldown[(int)a] = cooldownMax[(int)a];

        // ---------- Abilities ----------

        void ComboHit()
        {
            bool finisher = comboIndex == 2;
            float arc = finisher ? comboFinisherArc : comboArc;
            float range = finisher ? comboRange * 1.15f : comboRange;
            int hits = Strike(controller.Aim, range, arc, comboDamage[comboIndex], finisher ? 6f : 3f, 1);

            var color = finisher ? new Color(1f, 0.85f, 0.5f, 0.9f) : new Color(1f, 1f, 1f, 0.75f);
            SpawnArcFx(controller.Aim, range, color);
            if (hits > 0)
            {
                GameFeel.HitStop(finisher ? 0.06f : 0.03f);
                GameFeel.Shake(finisher ? 0.15f : 0.06f, 0.1f);
            }

            busy = comboStep / AttackSpeed;
            comboIndex = (comboIndex + 1) % 3;
            comboTimer = comboWindow;
        }

        void WideSwing()
        {
            int hits = Strike(controller.Aim, wideRange, 180f, wideDamage, 7f, 1);
            SpawnArcFx(controller.Aim, wideRange, new Color(0.7f, 0.9f, 1f, 0.85f));
            if (hits > 0) { GameFeel.HitStop(0.05f); GameFeel.Shake(0.12f, 0.12f); }
            busy = 0.35f / AttackSpeed;
            StartCooldown(Ability.WideSwing);
        }

        IEnumerator Dash()
        {
            StartCooldown(Ability.Dash);
            Vector2 dir = controller.Aim;
            Vector2 start = transform.position;
            health.Invulnerable = true;
            bodyCollider.enabled = false;
            controller.ForcedVelocity = dir * (dashDistance / dashTime);

            var hitSet = new System.Collections.Generic.HashSet<Enemy>();
            float t = 0;
            while (t < dashTime)
            {
                t += Time.deltaTime;
                foreach (var e in CombatUtil.EnemiesInArc(transform.position, dir, 0.9f, 360f))
                {
                    if (!hitSet.Add(e)) continue;
                    e.Health.TakeDamage(dashDamage);
                    e.Knockback(Vector2.Perpendicular(dir) * Mathf.Sign(Random.value - 0.5f) * 5f);
                }
                FadeAndDie.Spawn(SpriteFactory.Square, new Color(0.9f, 0.9f, 1f, 0.35f), transform.position, 0, 0.8f, 0.15f);
                yield return null;
            }

            controller.ForcedVelocity = null;
            bodyCollider.enabled = true;
            health.Invulnerable = false;
            if (hitSet.Count > 0) { Valor.Add(dashValor); GameFeel.Shake(0.08f, 0.08f); }
        }

        void WarCry()
        {
            StartCooldown(Ability.WarCry);
            warCryTimer = warCryDuration;
            Valor.DecayFrozen = true;
            FadeAndDie.Spawn(SpriteFactory.Ring, new Color(1f, 0.4f, 0.2f, 0.8f), transform.position, 0, 0.5f, 0.35f, 4f);
            GameFeel.Shake(0.1f, 0.2f);
        }

        IEnumerator Ultimate()
        {
            ulting = true;
            StartCooldown(Ability.Ultimate);
            controller.SpeedMultiplier = 0f;

            // Phase 1: circular swing that launches enemies.
            foreach (var e in CombatUtil.EnemiesInArc(transform.position, Vector2.right, ultLaunchRadius, 360f))
            {
                e.Health.TakeDamage(ultLaunchDamage);
                e.KnockUp(1.0f);
                Valor.Add(1);
            }
            FadeAndDie.Spawn(SpriteFactory.Ring, new Color(1f, 1f, 1f, 0.9f), transform.position, 0, ultLaunchRadius * 0.6f, 0.25f, 1.7f);
            GameFeel.Shake(0.15f, 0.15f);

            yield return new WaitForSeconds(ultDelay);

            // Phase 2: slam. Damage scales with every stored Valor charge, which are all consumed.
            bool full = Valor.IsFull;
            int charges = Valor.ConsumeAll();
            float dmg = ultSlamBaseDamage * (1f + ultBonusPerCharge * charges);
            foreach (var e in CombatUtil.EnemiesInArc(transform.position, Vector2.right, ultSlamRadius, 360f))
            {
                e.Health.TakeDamage(dmg, big: true);
                Vector2 away = ((Vector2)(e.transform.position - transform.position)).normalized;
                e.Knockback(away * (6f + charges), 0.4f);
            }

            var slamColor = full ? new Color(1f, 0.8f, 0.2f, 1f) : new Color(1f, 0.95f, 0.8f, 0.9f);
            FadeAndDie.Spawn(SpriteFactory.Ring, slamColor, transform.position, 0, 0.6f, 0.4f, ultSlamRadius * 1.8f);
            FadeAndDie.Spawn(SpriteFactory.Circle, new Color(slamColor.r, slamColor.g, slamColor.b, 0.5f), transform.position, 0, 1.5f, 0.3f, 3f);
            GameFeel.Shake(0.2f + charges * 0.04f, 0.3f);
            GameFeel.HitStop(full ? 0.14f : 0.06f);

            yield return new WaitForSeconds(0.2f);
            ulting = false;
        }

        // ---------- Helpers ----------

        int Strike(Vector2 dir, float range, float arc, float damage, float knock, int valorPerHit)
        {
            var targets = CombatUtil.EnemiesInArc(transform.position, dir, range, arc);
            int count = targets.Count;
            foreach (var e in targets.ToArray())
            {
                e.Health.TakeDamage(damage);
                Vector2 away = ((Vector2)(e.transform.position - transform.position)).normalized;
                e.Knockback(away * knock);
                Valor.Add(valorPerHit);
            }
            return count;
        }

        void SpawnArcFx(Vector2 dir, float range, Color color)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            FadeAndDie.Spawn(SpriteFactory.Arc, color, transform.position, angle, range, 0.12f, 1.1f);
        }
    }
}
