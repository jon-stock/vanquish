using System;
using UnityEngine;

namespace Vanquish.Simulation.Damage
{
    /// <summary>
    /// Default <see cref="IDamageable"/> implementation shared by flying units and
    /// static objectives. Runtime state (current health) lives here on the
    /// component; <see cref="DamageResolver"/> holds the actual (pure, testable)
    /// damage math per the coding standards' ScriptableObject-vs-MonoBehaviour split.
    /// </summary>
    public class Damageable : MonoBehaviour, IDamageable
    {
        [Header("Configured at spawn from part/site data")]
        [SerializeField] private float maxHealth = 100f;

        [Tooltip(
            "0 for flying units (no hardness — always full damage). > 0 for hardened " +
            "structures (bases, factories, warehouses, point-defense/radar sites).")]
        [SerializeField] private float hardness = 0f;

        [Range(0f, 1f)]
        [Tooltip("See DamageResolver — fraction of maxHealth that sub-threshold payloads cannot reduce below.")]
        [SerializeField] private float softCapResidualFraction = 0.15f;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;
        public float Hardness => hardness;
        public bool IsDestroyed { get; private set; }

        public event Action OnDestroyed;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        /// <summary>Call once at spawn to (re)configure from assembled site/unit stats.</summary>
        public void Configure(float newMaxHealth, float newHardness, float newSoftCapResidualFraction = 0.15f)
        {
            maxHealth = newMaxHealth;
            hardness = newHardness;
            softCapResidualFraction = newSoftCapResidualFraction;
            CurrentHealth = maxHealth;
            IsDestroyed = false;
        }

        public void TakeDamage(float rawDamage, float payloadSize)
        {
            if (IsDestroyed)
                return;

            CurrentHealth = DamageResolver.ApplyHit(
                CurrentHealth, maxHealth, hardness, softCapResidualFraction, rawDamage, payloadSize);

            if (!IsDestroyed && DamageResolver.IsDestroyed(CurrentHealth))
            {
                IsDestroyed = true;
                OnDestroyed?.Invoke();
            }
        }

        /// <summary>Damaged fraction remaining, for objective/win-condition checks (e.g. "destroy N% of the base").</summary>
        public float HealthFraction01 => maxHealth <= 0f ? 0f : CurrentHealth / maxHealth;
    }
}
