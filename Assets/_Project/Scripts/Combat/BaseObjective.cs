using UnityEngine;
using Vanquish.Simulation.Damage;

namespace Vanquish.Combat
{
    /// <summary>
    /// The "Base" combat-instance target type (PLAN.md's recommended first target
    /// type for Phase 0 — simplest defense profile; this is the old "troop
    /// concentration" slot). Phase 0 keeps this as a plain structure objective; full
    /// operator personnel/permadeath depth (PLAN.md "Personnel & Bases") is added in
    /// Phase 2 once the theatre map exists.
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public class BaseObjective : MonoBehaviour
    {
        [Tooltip("Fraction of the base's health (0..1) the attacker must destroy to win.")]
        [Range(0f, 1f)]
        public float attackerWinDestroyedFraction = 0.75f;

        private Damageable _damageable;

        public Damageable Damageable => _damageable != null ? _damageable : (_damageable = GetComponent<Damageable>());

        /// <summary>0 = untouched, 1 = fully destroyed.</summary>
        public float DestroyedFraction01 => 1f - Damageable.HealthFraction01;

        public bool HasMetAttackerWinCondition => Damageable.IsDestroyed || DestroyedFraction01 >= attackerWinDestroyedFraction;
    }
}
