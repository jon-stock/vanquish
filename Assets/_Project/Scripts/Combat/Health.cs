using System;
using UnityEngine;

namespace Vanquish.Combat
{
    /// <summary>
    /// Generic hit-point pool for any destructible combat entity (drones, base
    /// defenses, launch platforms). Damage amounts come from MissilePayloadDefinition
    /// stats (direct/splash damage) applied by whatever detonates near/on this object.
    /// </summary>
    public class Health : MonoBehaviour
    {
        public float maxHealth = 100f;
        public float CurrentHealth { get; private set; }
        public bool IsDestroyed { get; private set; }

        public event Action<Health> OnDestroyed;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void SetMaxHealth(float value, bool refill = true)
        {
            maxHealth = value;
            if (refill)
                CurrentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDestroyed || amount <= 0f)
                return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            Debug.Log($"[Combat] {name} took {amount:F0} damage, {CurrentHealth:F0}/{maxHealth:F0} HP remaining");
            if (CurrentHealth <= 0f)
            {
                IsDestroyed = true;
                OnDestroyed?.Invoke(this);

                // Actually remove the unit — without this it stays in the scene as a
                // still-targetable, still-hittable "dead" husk (confirmed by a missile
                // scoring a hit against an already-lethally-damaged drone). A short
                // delay leaves room for a future death VFX/sound before removal.
                Destroy(gameObject, 0.15f);
            }
        }
    }
}
