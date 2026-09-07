using System;

namespace Vanquish.Simulation.Damage
{
    /// <summary>
    /// Implemented by anything that can be destroyed in a combat instance — flying
    /// units (drones/missiles) and static objectives (bases, factories, warehouses,
    /// point-defense/radar sites). Mode-agnostic: no Combat/ or Theatre/ references.
    /// See PLAN.md "Damage & Payload Model".
    /// </summary>
    public interface IDamageable
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }

        /// <summary>
        /// Hardness rating used by <see cref="DamageResolver"/> to decide how much of
        /// an incoming payload's damage actually lands (see PLAN.md's soft-cap rule).
        /// </summary>
        float Hardness { get; }

        bool IsDestroyed { get; }

        /// <summary>
        /// Apply an incoming hit. <paramref name="rawDamage"/> is the munition's
        /// un-mitigated damage value; <paramref name="payloadSize"/> is its payload
        /// size/yield stat (e.g. MissilePayloadDefinition.warheadMassKg or a drone's
        /// carried WeaponBayDefinition.payloadCapacityKg), used by the damage resolver
        /// to apply the hardness soft-cap.
        /// </summary>
        void TakeDamage(float rawDamage, float payloadSize);

        /// <summary>Raised once, the instant this becomes destroyed.</summary>
        event Action OnDestroyed;
    }
}
