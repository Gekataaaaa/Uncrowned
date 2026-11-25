using UnityEngine;

namespace Uncrowned.Combat
{
    /// <summary>Resource spent by skills. Regenerates over time and on basic-attack hits.</summary>
    public class Mana : MonoBehaviour
    {
        public float max = 100f;
        public float regenPerSecond = 5f;

        public float Current { get; private set; }

        void Awake() => Current = max;

        void Update()
        {
            Current = Mathf.Min(max, Current + regenPerSecond * Time.deltaTime);
        }

        public bool Has(float amount) => Current >= amount;

        public bool TrySpend(float amount)
        {
            if (Current < amount) return false;
            Current -= amount;
            return true;
        }

        public void Restore(float amount) => Current = Mathf.Min(max, Current + amount);
    }
}
