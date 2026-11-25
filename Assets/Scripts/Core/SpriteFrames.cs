using UnityEngine;

namespace Uncrowned.Core
{
    /// <summary>Loops a few sprite frames on this object's SpriteRenderer (campfire and the like).</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFrames : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 8f;

        SpriteRenderer sr;

        void Awake() => sr = GetComponent<SpriteRenderer>();

        void Update()
        {
            if (frames == null || frames.Length == 0) return;
            sr.sprite = frames[(int)(Time.time * fps) % frames.Length];
        }

        /// <summary>Animates `sr` with Resources/Art/<prefix>0, 1, 2...</summary>
        public static SpriteFrames Play(SpriteRenderer sr, string prefix, int count, float fps)
        {
            var anim = sr.gameObject.AddComponent<SpriteFrames>();
            anim.frames = new Sprite[count];
            for (int i = 0; i < count; i++) anim.frames[i] = ArtKit.Sprite(prefix + i);
            anim.fps = fps;
            return anim;
        }
    }
}
