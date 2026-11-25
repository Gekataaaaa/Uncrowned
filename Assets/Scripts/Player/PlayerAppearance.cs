using System.Collections.Generic;
using Uncrowned.Combat;
using UnityEngine;

namespace Uncrowned.Player
{
    /// <summary>
    /// Visual layers, drawn back to front in this order. Clothes slots change with gear;
    /// Front (arm + greatsword) is always on top so armor never hides the blade.
    /// </summary>
    public enum AppearanceSlot { Body, Pants, Boots, Armor, Hair, Helmet, Front }

    /// <summary>Hero's eye expression, his reaction to NPC lines. Baked by Tools/ArtGen/eyes.py.</summary>
    public enum EyeMood { Neutral, Angry, Narrow, Feral }

    /// <summary>
    /// Paper-doll renderer for the hero. Every slot is its own SpriteRenderer layer that plays the
    /// same animation frame, so clothes can be swapped at any moment - including mid-fight.
    /// Sprites live in Resources/Art/Hero as "<prefix>_<frame>.png" (e.g. knight_walk2).
    /// Combat plays one-shot actions (attacks, spin, ult) on top of the idle/walk/jump loop.
    /// </summary>
    public class PlayerAppearance : MonoBehaviour
    {
        /// <summary>A frame name starting with this is drawn mirrored (used for the back half of a spin).</summary>
        public const char Mirror = '~';

        static readonly string[] Idle = { "idle0", "idle1", "idle2", "idle3" };
        static readonly string[] Walk = { "walk0", "walk1", "walk2", "walk3", "walk4", "walk5" };
        static readonly string[] Rise = { "jump1" };
        static readonly string[] Fall = { "jump2" };
        static readonly string[] Hurt = { "hit0", "hit1", "hit2" };
        static readonly string[] Death = { "death0", "death1", "death2", "death3", "death4", "death5" };

        readonly Dictionary<AppearanceSlot, SpriteRenderer> layers = new();
        readonly Dictionary<AppearanceSlot, string> equipped = new();
        static readonly Dictionary<string, Sprite> Cache = new();

        Rigidbody2D rb;
        PlayerController controller;
        Health health;
        string[] clip = Idle;
        float clipTime;

        // One-shot action (attack, hurt, death) that overrides the locomotion loop.
        string[] action;
        float actionTime, actionFrameTime;
        bool holdLast;

        public SpriteRenderer BodyRenderer => layers[AppearanceSlot.Body];

        /// <summary>Colour multiplied over every layer (e.g. the red berserk glow of War Cry); null = normal.</summary>
        public Color? Tint { get; set; }
        bool tinted;

        /// <summary>Builds the layer stack under `visualRoot` (which sits at the feet).</summary>
        public void Build(Transform visualRoot, int baseOrder)
        {
            foreach (AppearanceSlot slot in System.Enum.GetValues(typeof(AppearanceSlot)))
            {
                var go = new GameObject(slot.ToString());
                go.transform.SetParent(visualRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = baseOrder + (int)slot;
                layers[slot] = sr;
            }
            // The knight pack is one combined figure, so everything is in the Body layer for now.
            Equip(AppearanceSlot.Body, "knight");
        }

        /// <summary>Puts on a visual set by sprite prefix (null or empty to take it off). Works any time.</summary>
        public void Equip(AppearanceSlot slot, string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) equipped.Remove(slot);
            else equipped[slot] = prefix;
            Refresh();
        }

        /// <summary>The hero on screen; dialogue and cutscenes call <see cref="React"/> on him.</summary>
        public static PlayerAppearance Hero { get; private set; }
        public EyeMood Eyes { get; private set; }

        /// <summary>Hero's reaction to a dialogue line. Safe to call when no hero is spawned.</summary>
        public static void React(EyeMood mood)
        {
            if (Hero != null) Hero.SetEyes(mood);
        }

        /// <summary>Swaps the knight frame set for the one with this eye expression; animations carry on.</summary>
        public void SetEyes(EyeMood mood)
        {
            Eyes = mood;
            Equip(AppearanceSlot.Body, mood == EyeMood.Neutral ? "knight" : "knight_" + mood.ToString().ToLowerInvariant());
        }

        /// <summary>Plays `frames` once over `duration` seconds, then returns to idle/walk/jump.</summary>
        public void PlayAction(string[] frames, float duration, bool hold = false)
        {
            if (health != null && health.IsDead && frames != Death) return;
            action = frames;
            actionTime = 0;
            actionFrameTime = duration / frames.Length;
            holdLast = hold;
            Refresh();
        }

        void OnDestroy()
        {
            if (Hero == this) Hero = null;
        }

        void Awake()
        {
            Hero = this;
            rb = GetComponent<Rigidbody2D>();
            controller = GetComponent<PlayerController>();
            health = GetComponent<Health>();
            if (health != null)
            {
                health.Damaged += _ => { if (action == null && !health.IsDead) PlayAction(Hurt, 0.24f); };
                health.Died += () => PlayAction(Death, 0.9f, hold: true);
            }
        }

        void Update()
        {
            var next = Idle;
            if (controller != null && !controller.Grounded) next = rb != null && rb.linearVelocity.y > 0 ? Rise : Fall;
            else if (rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.2f) next = Walk;
            if (next != clip) { clip = next; clipTime = 0; }
            clipTime += Time.deltaTime;

            if (action != null)
            {
                actionTime += Time.deltaTime;
                if (actionTime >= actionFrameTime * action.Length && !holdLast) action = null;
            }
            Refresh();
        }

        void Refresh()
        {
            if (layers.Count == 0) return;
            string frame;
            if (action != null)
            {
                int i = Mathf.Min((int)(actionTime / actionFrameTime), action.Length - 1);
                frame = action[i];
            }
            else
            {
                float frameTime = clip == Walk ? 0.1f : 0.18f;
                frame = clip[(int)(clipTime / frameTime) % clip.Length];
            }

            // Only touch colours while tinted (and once to reset), so Health's hit flash still works normally.
            bool applyColor = Tint.HasValue || tinted;
            tinted = Tint.HasValue;
            var color = Tint ?? Color.white;

            bool mirrored = frame[0] == Mirror;
            if (mirrored) frame = frame.Substring(1);
            foreach (var kv in layers)
            {
                kv.Value.sprite = equipped.TryGetValue(kv.Key, out var prefix) ? Load($"Art/Hero/{prefix}_{frame}") : null;
                kv.Value.flipX = mirrored;
                if (applyColor) kv.Value.color = color;
            }
        }

        static Sprite Load(string path)
        {
            if (!Cache.TryGetValue(path, out var s))
            {
                s = Resources.Load<Sprite>(path);
                if (s == null) Debug.LogWarning("Missing sprite: Resources/" + path);
                Cache[path] = s;
            }
            return s;
        }
    }
}
