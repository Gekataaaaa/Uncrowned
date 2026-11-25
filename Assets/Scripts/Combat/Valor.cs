using System;
using UnityEngine;

namespace Uncrowned.Combat
{
    /// <summary>
    /// Warrior resource. Hits build charges; after a pause without hits they burn off one by one.
    /// The ultimate consumes all charges and scales with how many were stored.
    /// </summary>
    public class Valor : MonoBehaviour
    {
        public int max = 10;
        public float decayDelay = 4f;
        public float decayInterval = 0.5f;

        public int Charges { get; private set; }
        public bool IsFull => Charges >= max;
        /// <summary>While true (War Cry), charges never decay.</summary>
        public bool DecayFrozen { get; set; }
        public float TimeSinceGain { get; private set; } = 999f;

        public event Action<int> Changed;

        float decayTimer;

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Charges = Mathf.Min(max, Charges + amount);
            TimeSinceGain = 0f;
            decayTimer = 0f;
            Changed?.Invoke(Charges);
        }

        /// <summary>Removes every charge and returns how many there were.</summary>
        public int ConsumeAll()
        {
            int had = Charges;
            Charges = 0;
            Changed?.Invoke(Charges);
            return had;
        }

        void Update()
        {
            TimeSinceGain += Time.deltaTime;
            if (DecayFrozen || Charges == 0 || TimeSinceGain < decayDelay) return;

            decayTimer += Time.deltaTime;
            if (decayTimer >= decayInterval)
            {
                decayTimer = 0f;
                Charges--;
                Changed?.Invoke(Charges);
            }
        }
    }
}
