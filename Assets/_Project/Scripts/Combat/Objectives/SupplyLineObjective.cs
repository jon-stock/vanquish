using UnityEngine;
using Vanquish.Simulation.Damage;

namespace Vanquish.Combat
{
    /// <summary>
    /// The "Supply line" combat-instance target type (PLAN.md Phase 1) — a moving
    /// convoy. Unlike the static structure objectives, this has its own genuine
    /// defender-win condition (the convoy reaching safety) rather than relying solely
    /// on the generic attacker-stockpile-exhausted/timeout path.
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public class SupplyLineObjective : MonoBehaviour, IObjective
    {
        [Tooltip("Fraction of the convoy's health (0..1) the attacker must destroy to win.")]
        [Range(0f, 1f)]
        public float attackerWinDestroyedFraction = 0.6f;

        [Tooltip("Escort progress gained per second toward safety while the convoy survives (1.0 = fully escorted through in 1 second).")]
        public float escortProgressPerSecond = 0.05f;

        private Damageable _damageable;

        public Damageable Damageable => _damageable != null ? _damageable : (_damageable = GetComponent<Damageable>());

        /// <summary>0 = just entered the instance, 1 = reached safety (defender win).</summary>
        public float EscortProgress01 { get; private set; }

        public float DestroyedFraction01 => 1f - Damageable.HealthFraction01;

        public bool HasMetAttackerWinCondition => Damageable.IsDestroyed || DestroyedFraction01 >= attackerWinDestroyedFraction;

        /// <summary>True once the convoy has been escorted all the way to safety.</summary>
        public bool HasMetDefenderWinCondition => EscortProgress01 >= 1f;

        public void Tick(float deltaTime)
        {
            // A convoy that's already destroyed enough to hand the attacker the win
            // stops making progress — no point advancing escort progress on a wreck.
            if (Damageable.IsDestroyed || HasMetAttackerWinCondition)
                return;

            EscortProgress01 = Mathf.Clamp01(EscortProgress01 + escortProgressPerSecond * deltaTime);
        }
    }
}
