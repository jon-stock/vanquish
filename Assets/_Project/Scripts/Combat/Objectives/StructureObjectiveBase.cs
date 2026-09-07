using UnityEngine;
using Vanquish.Simulation.Damage;

namespace Vanquish.Combat
{
    /// <summary>
    /// Shared logic for every static "structure" target type — base, factory,
    /// warehouse (PLAN.md Phase 1: these are mechanically identical, differing only
    /// by data — hardness, max health, win threshold — not by code). Attacker wins by
    /// destroying enough of the structure's health; these target types have no
    /// defender-win condition of their own (defender relies solely on the generic
    /// attacker-stockpile-exhausted/timeout path in <see cref="EngagementController"/>).
    /// Concrete subclasses exist per target type purely so scene objects/tooling can
    /// identify which target type a given objective represents (e.g.
    /// <c>GetComponent&lt;FactoryObjective&gt;()</c>), not because the behavior differs.
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public abstract class StructureObjectiveBase : MonoBehaviour, IObjective
    {
        [Tooltip("Fraction of the structure's health (0..1) the attacker must destroy to win.")]
        [Range(0f, 1f)]
        public float attackerWinDestroyedFraction = 0.75f;

        private Damageable _damageable;

        public Damageable Damageable => _damageable != null ? _damageable : (_damageable = GetComponent<Damageable>());

        /// <summary>0 = untouched, 1 = fully destroyed.</summary>
        public float DestroyedFraction01 => 1f - Damageable.HealthFraction01;

        public bool HasMetAttackerWinCondition => Damageable.IsDestroyed || DestroyedFraction01 >= attackerWinDestroyedFraction;

        /// <summary>Static structures have no objective-specific defender-win condition.</summary>
        public bool HasMetDefenderWinCondition => false;

        /// <summary>Static structures have no time-based state to advance.</summary>
        public void Tick(float deltaTime) { }
    }
}
