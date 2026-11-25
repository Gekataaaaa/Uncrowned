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
    /// Greatsword warrior kit. Skills cost mana; basic attacks are free and restore a little mana on hit.
    /// The ultimate "Crown's Judgement" launches nearby enemies, then slams them down for fixed damage.
    /// Numbers are first-pass placeholders for tuning.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(Mana), typeof(Health))]
    public class WarriorCombat : MonoBehaviour
    {
        [Header("Movement while attacking")]
        [Tooltip("Walk speed multiplier while a swing is in progress.")]
        public float attackMoveSpeed = 0.675f;

        [Header("Combo (LMB) — free")]
        public float[] comboDamage = { 10f, 10f, 16f };
        public float comboRange = 2.55f;
        public float comboArc = 110f;
        public float comboFinisherArc = 170f;
        public float comboStep = 0.28f;
        public float comboWindow = 0.8f;
        public float manaPerHit = 3f;

        [Header("Dash (Shift)")]
        public float dashDamage = 12f;
        public float dashDistance = 6.75f;
        public float dashTime = 0.2f;
        public float dashCooldown = 4f;
        public float dashHitRadius = 1.35f;
        public float dashMana = 19f;

        [Header("Wide Swing (RMB)")]
        [Tooltip("Base damage of each cut; the swing cuts wideCuts times.")]
        public float wideDamage = 7f;
        public int wideCuts = 4;
        public float wideRange = 3.45f;
        public float wideCooldown = 5f;
        public float wideMana = 25f;

        [Header("War Cry (Q)")]
        public float warCryDuration = 6f;
        public float warCryAttackSpeed = 1.2f;
        public float warCryDamage = 1.2f;
        public float warCryCooldown = 15f;
        public float warCryMana = 38f;

        [Header("Crown's Judgement (R)")]
        public float ultLaunchDamage = 15f;
        public float ultLaunchRadius = 4.5f;
        public float ultSlamDamage = 75f;
        public float ultSlamRadius = 4.95f;
        [Tooltip("Time from the launching sweep to the vertical cleave (matches the salto animation).")]
        public float ultDelay = 0.74f;
        public float ultCooldown = 20f;
        public float ultMana = 63f;

        public Mana Mana { get; private set; }
        public bool WarCryActive => warCryTimer > 0;

        PlayerController controller;
        Health health;
        Rigidbody2D rb;
        PlayerAppearance look;

        // Animation frames from the knight pack (see PlayerAppearance). "~" = mirrored frame.
        static readonly string[] SlashA = { "atkA0", "atkA1", "atkA2", "atkA3", "atkA4" };
        static readonly string[] SlashB = { "atkB0", "atkB1", "atkB2", "atkB3" };
        // Spin: a full 360 of the blade around the waist (hand-drawn spin0..7).
        static readonly string[] Spin = { "spin0", "spin1", "spin2", "spin3", "spin4", "spin5", "spin6", "spin7" };
        static readonly string[] DashPose = { "dash0", "dash1", "dash0", "dash1" };
        static readonly string[] Shout = { "cry0", "cry1", "cry2", "cry1", "cry2" };
        static readonly Color BerserkDark = new(1f, 0.32f, 0.3f);
        static readonly Color BerserkBright = new(1f, 0.55f, 0.45f);
        // Ult (hand-drawn ult0..12), in 30 ms ticks: launching horizontal sweep, quick salto, then a heavy vertical cleave.
        const float UltTick = 0.03f;
        static readonly string[] UltWindup = Ticks(
            ("ult0", 3), ("ult1", 5), ("ult2", 5), ("ult3", 2),               // crouch + horizontal sweep that throws enemies up
            ("ult4", 2), ("ult5", 2), ("ult6", 2), ("ult7", 2),               // salto
            ("ult8", 4), ("ult9", 1));                                       // unfold at the top, start of the downward arc
        static readonly string[] UltSlam = Ticks(("ult10", 11), ("ult11", 5), ("ult12", 8)); // impact, shockwave, settle
        const float UltLaunchAt = 0.1f; // the sweep connects on ult1

        static string[] Ticks(params (string frame, int ticks)[] parts)
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var (frame, ticks) in parts)
                for (int i = 0; i < ticks; i++) list.Add(frame);
            return list.ToArray();
        }

        readonly float[] cooldown = new float[5];
        readonly float[] cooldownMax = new float[5];
        readonly float[] manaCost = new float[5];
        float busy;
        float actionLock; // no other skill or attack until the current one's animation has finished
        int comboIndex;
        float comboTimer;
        float warCryTimer;
        bool ulting;

        float AttackSpeed => WarCryActive ? warCryAttackSpeed : 1f;
        float DamageMult => WarCryActive ? warCryDamage : 1f;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            Mana = GetComponent<Mana>();
            health = GetComponent<Health>();
            rb = GetComponent<Rigidbody2D>();
            look = GetComponent<PlayerAppearance>();

            cooldownMax[(int)Ability.Dash] = dashCooldown;
            cooldownMax[(int)Ability.WideSwing] = wideCooldown;
            cooldownMax[(int)Ability.WarCry] = warCryCooldown;
            cooldownMax[(int)Ability.Ultimate] = ultCooldown;

            manaCost[(int)Ability.Dash] = dashMana;
            manaCost[(int)Ability.WideSwing] = wideMana;
            manaCost[(int)Ability.WarCry] = warCryMana;
            manaCost[(int)Ability.Ultimate] = ultMana;
        }

        /// <summary>0 = ready, 1 = just used.</summary>
        public float CooldownFraction(Ability a)
        {
            float max = cooldownMax[(int)a];
            return max <= 0 ? 0 : Mathf.Clamp01(cooldown[(int)a] / max);
        }

        public float CooldownRemaining(Ability a) => Mathf.Max(0, cooldown[(int)a]);
        public float ManaCost(Ability a) => manaCost[(int)a];
        public bool CanAfford(Ability a) => Mana.Has(manaCost[(int)a]);

        void Update()
        {
            for (int i = 0; i < cooldown.Length; i++) cooldown[i] -= Time.deltaTime;
            busy -= Time.deltaTime;
            actionLock -= Time.deltaTime;
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0) comboIndex = 0;
            if (warCryTimer > 0) warCryTimer -= Time.deltaTime;
            // Berserk: the hero glows pulsing red for as long as War Cry lasts.
            if (look != null)
                look.Tint = WarCryActive && !health.IsDead
                    ? Color.Lerp(BerserkDark, BerserkBright, 0.5f + 0.5f * Mathf.Sin(Time.time * 9f))
                    : null;

            controller.FacingLocked = actionLock > 0 || ulting;
            if (!ulting) controller.SpeedMultiplier = busy > 0 ? attackMoveSpeed : 1f;
            if (health.IsDead || ulting || actionLock > 0) return;

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            if (kb.rKey.wasPressedThisFrame && Use(Ability.Ultimate)) { StartCoroutine(Ultimate()); return; }
            if ((kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame) && Use(Ability.Dash)) { StartCoroutine(Dash()); return; }
            if (kb.qKey.wasPressedThisFrame && Use(Ability.WarCry)) WarCry();
            if (busy > 0) return;
            if (mouse.rightButton.wasPressedThisFrame && Use(Ability.WideSwing)) { StartCoroutine(WideSwing()); return; }
            if (mouse.leftButton.isPressed) ComboHit();
        }

        /// <summary>Checks cooldown and mana; on success spends the mana and starts the cooldown.</summary>
        bool Use(Ability a)
        {
            if (cooldown[(int)a] > 0 || !Mana.TrySpend(manaCost[(int)a])) return false;
            cooldown[(int)a] = cooldownMax[(int)a];
            return true;
        }

        // ---------- Abilities ----------

        void ComboHit()
        {
            bool finisher = comboIndex == 2;
            float arc = finisher ? comboFinisherArc : comboArc;
            float range = finisher ? comboRange * 1.15f : comboRange;
            int hits = Strike(controller.Aim, range, arc, comboDamage[comboIndex], finisher ? 6f : 3f);
            if (hits > 0) Mana.Restore(manaPerHit);

            var color = finisher ? new Color(1f, 0.85f, 0.5f, 0.9f) : new Color(1f, 1f, 1f, 0.75f);
            SpawnArcFx(controller.Aim, range, color);
            if (hits > 0)
            {
                GameFeel.HitStop(finisher ? 0.06f : 0.03f);
                GameFeel.Shake(finisher ? 0.15f : 0.06f, 0.1f);
            }

            busy = comboStep / AttackSpeed;
            Animate(comboIndex == 1 ? SlashB : SlashA, busy);
            comboIndex = (comboIndex + 1) % 3;
            comboTimer = comboWindow;
        }

        IEnumerator WideSwing()
        {
            // Hits both sides: the crowd-control swing. Damage lands while the blade goes round,
            // in cuts spread evenly over the spin, alternating front and behind.
            float duration = 0.4f / AttackSpeed;
            busy = duration;
            Animate(Spin, duration);

            var fx = new Color(0.7f, 0.9f, 1f, 0.85f);
            float elapsed = 0f;
            for (int i = 0; i < wideCuts; i++)
            {
                float at = duration * (i + 0.5f) / wideCuts;
                if (at > elapsed) { yield return new WaitForSeconds(at - elapsed); elapsed = at; }
                if (health.IsDead) yield break;

                bool last = i == wideCuts - 1;
                var side = new Vector2(i % 2 == 1 ? -controller.Facing : controller.Facing, 0f);
                int hits = Strike(side, wideRange, 360f, wideDamage, last ? 7f : 1.5f);
                SpawnArcFx(side, wideRange, fx);
                if (hits > 0) { GameFeel.HitStop(last ? 0.05f : 0.025f); GameFeel.Shake(last ? 0.12f : 0.06f, 0.08f); }
            }
        }

        IEnumerator Dash()
        {
            // The only horizontal-only skill: dashes toward the side the mouse is on.
            Vector2 dir = new Vector2(controller.Facing, 0);
            float gravity = rb.gravityScale;
            rb.gravityScale = 0f;
            health.Invulnerable = true;
            controller.ForcedVelocity = dir * (dashDistance / dashTime);
            Animate(DashPose, dashTime);

            var hitSet = new System.Collections.Generic.HashSet<Enemy>();
            float t = 0;
            while (t < dashTime)
            {
                t += Time.deltaTime;
                foreach (var e in CombatUtil.EnemiesInArc(transform.position, dir, dashHitRadius, 360f))
                {
                    if (!hitSet.Add(e)) continue;
                    e.Health.TakeDamage(dashDamage * DamageMult);
                    e.Knockback(new Vector2(dir.x * 3f, 6f));
                }
                FadeAndDie.Spawn(SpriteFactory.Square, new Color(0.9f, 0.9f, 1f, 0.35f), transform.position, 0, 0.8f, 0.15f);
                yield return null;
            }

            controller.ForcedVelocity = null;
            rb.linearVelocity = new Vector2(dir.x * 2f, 0f);
            rb.gravityScale = gravity;
            health.Invulnerable = false;
            if (hitSet.Count > 0) GameFeel.Shake(0.08f, 0.08f);
        }

        void WarCry()
        {
            warCryTimer = warCryDuration;
            Animate(Shout, 0.5f);
            FadeAndDie.Spawn(SpriteFactory.Ring, new Color(1f, 0.4f, 0.2f, 0.8f), transform.position, 0, 0.5f, 0.35f, 4f);
            GameFeel.Shake(0.1f, 0.2f);
        }

        IEnumerator Ultimate()
        {
            ulting = true;
            controller.SpeedMultiplier = 0f;
            Animate(UltWindup, UltWindup.Length * UltTick);
            yield return new WaitForSeconds(UltLaunchAt);

            // Phase 1: circular swing that launches enemies.
            foreach (var e in CombatUtil.EnemiesInArc(transform.position, Vector2.right, ultLaunchRadius, 360f))
            {
                e.Health.TakeDamage(ultLaunchDamage * DamageMult);
                e.KnockUp(12f, 0.9f);
            }
            FadeAndDie.Spawn(SpriteFactory.Ring, new Color(1f, 1f, 1f, 0.9f), transform.position, 0, ultLaunchRadius * 0.6f, 0.25f, 1.7f);
            GameFeel.Shake(0.15f, 0.15f);

            yield return new WaitForSeconds(ultDelay);
            Animate(UltSlam, UltSlam.Length * UltTick);

            // Phase 2: slam them back into the ground for fixed damage.
            foreach (var e in CombatUtil.EnemiesInArc(transform.position, Vector2.right, ultSlamRadius, 360f))
            {
                e.Health.TakeDamage(ultSlamDamage * DamageMult, big: true);
                float side = Mathf.Sign(e.transform.position.x - transform.position.x);
                e.Knockback(new Vector2(side * 10f, -14f), 0.5f);
            }

            var slamColor = new Color(1f, 0.8f, 0.2f, 1f);
            FadeAndDie.Spawn(SpriteFactory.Ring, slamColor, transform.position, 0, 0.6f, 0.4f, ultSlamRadius * 1.8f);
            FadeAndDie.Spawn(SpriteFactory.Circle, new Color(slamColor.r, slamColor.g, slamColor.b, 0.5f), transform.position, 0, 1.5f, 0.3f, 3f);
            GameFeel.Shake(0.5f, 0.35f);
            GameFeel.HitStop(0.18f);

            yield return new WaitForSeconds(UltSlam.Length * UltTick); // locked until the whole cleave animation has played
            ulting = false;
        }

        // ---------- Helpers ----------

        void Animate(string[] frames, float duration)
        {
            // Face the cursor once at the start, then hold that side until the animation ends.
            if (!controller.FacingLocked) controller.FaceAim();
            controller.FacingLocked = true;
            actionLock = Mathf.Max(actionLock, duration);
            if (look != null) look.PlayAction(frames, duration);
        }

        int Strike(Vector2 dir, float range, float arc, float damage, float knock)
        {
            var targets = CombatUtil.EnemiesInArc(transform.position, dir, range, arc);
            int count = targets.Count;
            foreach (var e in targets.ToArray())
            {
                e.Health.TakeDamage(damage * DamageMult);
                float side = Mathf.Sign(e.transform.position.x - transform.position.x);
                e.Knockback(new Vector2(side * knock, 0f));
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
