using UnityEngine;
using Vanquish.Data.Support;

namespace Vanquish.Combat
{
    /// <summary>
    /// Runtime state for one defending <see cref="PointDefenseDefinition"/> instance
    /// within a combat instance. This is the mechanical heart of PLAN.md's stockpile-
    /// tactics thesis: every attacker commit that reaches a battery costs it one
    /// interceptor attempt (win or lose), so cheap decoys can deliberately drain an
    /// expensive interceptor stock before the real strike arrives.
    /// </summary>
    public class PointDefenseBattery : MonoBehaviour
    {
        public PointDefenseDefinition definition;

        /// <summary>Runtime-only remaining ammo. &lt;= 0 at configure time means unlimited (see PointDefenseDefinition.ammoCount).</summary>
        public int RemainingAmmo { get; private set; }

        private bool _unlimitedAmmo;

        private void Awake()
        {
            Configure(definition);
        }

        public void Configure(PointDefenseDefinition newDefinition)
        {
            definition = newDefinition;
            _unlimitedAmmo = definition == null || definition.ammoCount <= 0;
            RemainingAmmo = _unlimitedAmmo ? 0 : definition.ammoCount;
        }

        public bool IsDepleted => !_unlimitedAmmo && RemainingAmmo <= 0;

        /// <summary>
        /// Attempt to intercept one incoming attacker unit. Consumes one interceptor
        /// (unless ammo is unlimited) regardless of outcome — this is deliberate: a
        /// battery that fires at a cheap decoy and misses (or even fires and hits a
        /// decoy) has still spent a real, scarce shot on a target that didn't matter.
        /// Returns false immediately (no ammo spent) if the battery is already
        /// depleted or has no definition configured.
        /// </summary>
        public bool TryIntercept()
        {
            if (definition == null || IsDepleted)
                return false;

            if (!_unlimitedAmmo)
                RemainingAmmo--;

            return Random.value < definition.interceptProbability;
        }
    }
}
