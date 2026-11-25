using Uncrowned.Combat;
using Uncrowned.Core;
using UnityEngine;

namespace Uncrowned.Player
{
    /// <summary>Builds the hero GameObject for stages (with combat) and for the town (without).</summary>
    public static class PlayerFactory
    {
        /// <summary>Half the collider height; the knight is ~29 px (~1.8 units) tall at 16 PPU.</summary>
        public const float HalfHeight = 0.9f;

        public static GameObject Create(Vector2 position, bool withCombat)
        {
            var go = new GameObject("Hero");
            go.layer = Arena.CharacterLayer;
            go.transform.position = position;

            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(go.transform, false);
            shadow.transform.localPosition = new Vector3(0, -HalfHeight, 0);
            shadow.transform.localScale = new Vector3(1f, 0.25f, 1);
            SpriteFactory.AddRenderer(shadow, SpriteFactory.Circle, new Color(0, 0, 0, 0.35f), 1);

            // Visual root sits at the feet (sprites pivot at the bottom) and is flipped to face the mouse.
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0, -HalfHeight, 0);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            go.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, HalfHeight * 2f);

            var hp = go.AddComponent<Health>();
            hp.SetMax(150f);
            hp.numberColor = new Color(1f, 0.4f, 0.4f);
            hp.Damaged += _ => GameFeel.Shake(0.12f, 0.12f);

            go.AddComponent<PlayerController>();
            var look = go.AddComponent<PlayerAppearance>();
            look.Build(visual.transform, 10);
            hp.body = look.BodyRenderer;

            if (withCombat)
            {
                go.AddComponent<Mana>();
                go.AddComponent<WarriorCombat>();
            }
            return go;
        }
    }
}
