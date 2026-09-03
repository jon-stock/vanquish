using UnityEngine;
using Vanquish.Data.Drones;
using Vanquish.Data.Missiles;

namespace Vanquish.Data
{
    /// <summary>Aggregated, ready-to-spawn stats computed from a MissileLoadout.</summary>
    public struct MissileRuntimeStats
    {
        public float massKg;
        public float fuelMassKg;
        public float thrustNewtons;
        public float dragCoefficient;
        public float maxGForce;
        public float radarCrossSection;
        public float infraredSignature;
        public float directDamage;
        public float splashDamage;
        public float blastRadiusMeters;
        public float seekerRangeMeters;
        public float seekerFieldOfViewDegrees;
        public float jamResistance;

        /// <summary>Susceptibility to decoys/countermeasures, 0-1 — SeekerDefinition.countermeasureSusceptibility
        /// passed through so GuidanceController can weight an inbound decoy's success chance by how easily
        /// THIS seeker specifically gets spoofed (a Multi-Spectral seeker resists a flare much better than a
        /// basic IR one does, even against the exact same countermeasure).</summary>
        public float countermeasureSusceptibility;

        /// <summary>Effective thrust duration in seconds — MissileEngineDefinition.burnTimeSeconds scaled by
        /// fuelFillFraction, not the engine's full-tank rating. Feeds MissileBurnController so a half-full
        /// tank genuinely cuts thrust early instead of only weighing less. See that class's own doc comment
        /// for why this needed to exist at all.</summary>
        public float effectiveBurnTimeSeconds;

        /// <summary>Airframe's MTOW limit in kg. 0 or less means no limit is configured.</summary>
        public float maxTakeOffMassKg;

        /// <summary>True if massKg is within maxTakeOffMassKg (or no limit is configured).</summary>
        public bool isWithinMtow;
    }

    /// <summary>Aggregated, ready-to-spawn stats computed from a DroneLoadout.</summary>
    public struct DroneRuntimeStats
    {
        public float massKg;
        public float fuelMassKg;
        public float thrustNewtons;
        public float dragCoefficient;
        public float maxGForce;
        public float radarCrossSection;
        public float infraredSignature;
        public float maxHealth;
        public float sensorRangeMeters;
        public float sensorFieldOfViewDegrees;
        public bool sharesContactsWithTeam;

        /// <summary>True for fixed-wing/jet propulsion (constant thrust, orients to velocity). False for
        /// omnidirectional multirotor propulsion (hovers/strafes via vectored steering). Mirrors
        /// PropulsionDefinition.requiresForwardFlight — VehicleFactory reads this to configure FlightBody.</summary>
        public bool requiresForwardFlight;

        /// <summary>Wing lift coefficient (WingOrPropellerDefinition.liftCoefficient) — only meaningful
        /// when requiresForwardFlight is true; feeds FlightBody's aerodynamic lift model (an angle-of-attack
        /// curve scaled by speed^2 — see FlightBody.ComputeLiftFactor) for fixed-wing/jet drones.</summary>
        public float liftCoefficient;

        /// <summary>The four angle-of-attack lift-curve/induced-drag stats from the design's wing part —
        /// only meaningful when requiresForwardFlight is true. See WingOrPropellerDefinition's own tooltips
        /// for what each means; threaded through here so FlightBody's fixed-wing Configure overload doesn't
        /// need to reach back into the loadout/part assets itself.</summary>
        public float zeroLiftAoADegrees;
        public float referenceAoADegrees;
        public float criticalAoADegrees;
        public float inducedDragFactor;

        /// <summary>Airframe's MTOW limit in kg. 0 or less means no limit is configured.</summary>
        public float maxTakeOffMassKg;

        /// <summary>True if massKg is within maxTakeOffMassKg (or no limit is configured).</summary>
        public bool isWithinMtow;

        /// <summary>True if the airframe/wing-or-propeller/propulsion/engine slots all agree on the same
        /// FlightConfiguration (see DroneCompatibility) — false for an incoherent design, e.g. a jet engine
        /// paired with a multirotor airframe. Defaults true for an incomplete loadout (nothing to disagree
        /// with yet); mirrors isWithinMtow as a second "not actually combat-ready" gate.</summary>
        public bool isFlightConfigurationCompatible;

        /// <summary>Human-readable explanation of the first detected mismatch, or null when compatible.</summary>
        public string flightConfigurationMismatchReason;

        /// <summary>True if the fuel part's FuelType matches what the propulsion part actually needs
        /// (see DroneCompatibility.IsFuelCompatible) — false for e.g. a Battery in a Supersonic Jet
        /// propulsion slot. Defaults true for an incomplete loadout, same as isFlightConfigurationCompatible.</summary>
        public bool isFuelCompatible;

        /// <summary>Human-readable explanation of the fuel/propulsion mismatch, or null when compatible.</summary>
        public string fuelMismatchReason;

        /// <summary>Actual ammo carried after clamping DroneLoadout.ammoCount to the weapon bay's real
        /// maxMunitionCount — see WeaponBayDefinition.maxMunitionCount's own tooltip for why this is now
        /// enforced instead of an arbitrary free-typed number.</summary>
        public int effectiveAmmoCount;

        /// <summary>How many of effectiveAmmoCount are carried externally (beyond the bay's
        /// internalCapacity) — each one adds to radarCrossSection and gets a visible mounted-missile mesh;
        /// the rest ride hidden internally. See WeaponBayDefinition.internalCapacity's own tooltip.</summary>
        public int externallyMountedAmmoCount;
    }

    /// <summary>
    /// Converts player-facing part choices (Loadout) into concrete numbers the
    /// simulation layer (FlightBody, DetectableSignature, Health, etc.) consumes.
    /// Formulas here are deliberately simple, tunable placeholders appropriate for
    /// Phase 1's MVP scope — expect a real balancing pass once Phase 2 adds part
    /// breadth and meaningful trade-offs.
    /// </summary>
    public static class DesignStatsCalculator
    {
        public static MissileRuntimeStats Calculate(MissileLoadout loadout)
        {
            var stats = new MissileRuntimeStats();
            if (loadout == null || !loadout.IsComplete)
                return stats;

            // Fuel mass scales with the continuous fill-level slider rather than always
            // assuming a full tank — see MissileLoadout.fuelFillFraction.
            stats.fuelMassKg = loadout.fuel.capacityKg * Mathf.Clamp01(loadout.fuelFillFraction);

            stats.massKg = loadout.airframe.massKg + loadout.airframe.structuralMassKg
                           + loadout.engine.massKg
                           + loadout.payload.massKg + loadout.payload.warheadMassKg
                           + loadout.seeker.massKg
                           + loadout.fuel.massKg + stats.fuelMassKg
                           + (loadout.countermeasure != null ? loadout.countermeasure.massKg : 0f)
                           + (loadout.jamming != null ? loadout.jamming.massKg : 0f);

            stats.maxTakeOffMassKg = loadout.airframe.maxTakeOffMassKg;
            stats.isWithinMtow = stats.maxTakeOffMassKg <= 0f || stats.massKg <= stats.maxTakeOffMassKg;

            stats.thrustNewtons = loadout.engine.thrustNewtons;
            stats.dragCoefficient = loadout.airframe.dragCoefficient;
            stats.effectiveBurnTimeSeconds = loadout.engine.burnTimeSeconds * Mathf.Clamp01(loadout.fuelFillFraction);

            // Depth pass (direct user feedback: "engine type should affect maneuverability"):
            // engine.maneuverabilityMultiplier scales the airframe's own maxGForce ceiling —
            // a short-burn solid rocket can pull noticeably harder corrections than a
            // sustained-cruise scramjet on the same airframe, not just fly faster/further.
            stats.maxGForce = loadout.airframe.maxGForce * loadout.engine.maneuverabilityMultiplier
                               + (loadout.countermeasure != null ? loadout.countermeasure.maxGForceBonus : 0f);

            float rcsMultiplier = loadout.countermeasure != null ? loadout.countermeasure.radarCrossSectionMultiplier : 1f;
            stats.radarCrossSection = loadout.airframe.baseRadarCrossSection * rcsMultiplier;

            float irMultiplier = loadout.countermeasure != null ? loadout.countermeasure.infraredSignatureMultiplier : 1f;
            stats.infraredSignature = loadout.engine.infraredSignature * irMultiplier;

            stats.directDamage = loadout.payload.directDamage;
            stats.splashDamage = loadout.payload.splashDamage;
            stats.blastRadiusMeters = loadout.payload.blastRadiusMeters;

            stats.seekerRangeMeters = loadout.seeker.detectionRangeMeters;
            stats.seekerFieldOfViewDegrees = loadout.seeker.fieldOfViewDegrees;
            stats.countermeasureSusceptibility = loadout.seeker.countermeasureSusceptibility;

            stats.jamResistance = loadout.seeker.jamResistance
                                   + (loadout.jamming != null ? loadout.jamming.counterJammingStrength : 0f);

            return stats;
        }

        public static DroneRuntimeStats Calculate(DroneLoadout loadout)
        {
            var stats = new DroneRuntimeStats();
            if (loadout == null || !loadout.IsComplete)
                return stats;

            // Depth pass (direct user feedback: "smaller craft should be able to store
            // fewer missiles" / "weapons bay doesn't affect much"): ammoCount used to be
            // a free-typed number with no relationship to the weapon bay's own
            // maxMunitionCount at all — a design could set any ammoCount regardless of
            // what its bay could actually hold. Now clamped to the bay's real capacity,
            // and split into internal (hidden, zero RCS) vs. external (visible, adds
            // RCS) per WeaponBayDefinition.internalCapacity's "internal first, then
            // pylon overflow" rule.
            MissileRuntimeStats missileStats = loadout.missileLoadout != null && loadout.missileLoadout.IsComplete
                ? Calculate(loadout.missileLoadout)
                : default;
            int bayCapacity = loadout.weaponBay != null ? Mathf.Max(0, loadout.weaponBay.maxMunitionCount) : 0;
            stats.effectiveAmmoCount = Mathf.Clamp(loadout.ammoCount, 0, bayCapacity);
            int internalCapacity = loadout.weaponBay != null ? Mathf.Max(0, loadout.weaponBay.internalCapacity) : 0;
            stats.externallyMountedAmmoCount = Mathf.Max(0, stats.effectiveAmmoCount - internalCapacity);
            float missileMass = missileStats.massKg * stats.effectiveAmmoCount;

            // Fuel/battery mass scales with the continuous fill-level slider rather than
            // always assuming a full tank — see DroneLoadout.fuelFillFraction (Phase 2B,
            // mirrors MissileLoadout.fuelFillFraction from 2A).
            stats.fuelMassKg = loadout.fuel.capacityKg * Mathf.Clamp01(loadout.fuelFillFraction);

            stats.massKg = loadout.propulsion.massKg + loadout.airframe.massKg + loadout.airframe.structuralMassKg
                           + loadout.wingOrPropeller.massKg + loadout.hullMaterial.massKg + loadout.engine.massKg
                           + loadout.fuel.massKg + stats.fuelMassKg + loadout.weaponBay.massKg
                           + loadout.sensorSuite.massKg + missileMass
                           + (loadout.countermeasure != null ? loadout.countermeasure.massKg : 0f);

            stats.maxTakeOffMassKg = loadout.airframe.maxTakeOffMassKg;
            stats.isWithinMtow = stats.maxTakeOffMassKg <= 0f || stats.massKg <= stats.maxTakeOffMassKg;

            // Phase 1 simplification: DroneEngineDefinition.powerOutput is treated directly
            // as thrust in Newtons regardless of propulsion type. Revisit once electric vs.
            // jet propulsion need genuinely different force models.
            stats.thrustNewtons = loadout.engine.powerOutput;
            stats.dragCoefficient = loadout.airframe.dragCoefficient + loadout.wingOrPropeller.dragCoefficient;
            stats.requiresForwardFlight = loadout.propulsion.requiresForwardFlight;
            stats.liftCoefficient = loadout.wingOrPropeller.liftCoefficient;
            stats.zeroLiftAoADegrees = loadout.wingOrPropeller.zeroLiftAoADegrees;
            stats.referenceAoADegrees = loadout.wingOrPropeller.referenceAoADegrees;
            stats.criticalAoADegrees = loadout.wingOrPropeller.criticalAoADegrees;
            stats.inducedDragFactor = loadout.wingOrPropeller.inducedDragFactor;

            stats.isFlightConfigurationCompatible = DroneCompatibility
                .IsLoadoutFlightConfigurationConsistent(loadout, out stats.flightConfigurationMismatchReason);
            stats.isFuelCompatible = DroneCompatibility
                .IsFuelCompatible(loadout.propulsion, loadout.fuel, out stats.fuelMismatchReason);

            // Phase 1 simplification: no real turn-rate-to-lateral-G conversion yet.
            // Keep this modest — quadcopters/small drones don't pull fighter-jet-style
            // G-loads; the earlier formula (turnRate/15, ~15G here) let guidance
            // massively overshoot at high speed before it could correct, sending
            // AI-controlled drones rocketing far off the arena.
            stats.maxGForce = 0.5f + (loadout.wingOrPropeller.turnRateDegreesPerSecond / 90f);

            // Phase 2C: fold in the optional decoy countermeasure's stealth multipliers
            // alongside the hull material's, same pattern MissileRuntimeStats already
            // uses for its own (separate) countermeasure slot.
            float droneRcsMultiplier = loadout.countermeasure != null ? loadout.countermeasure.radarCrossSectionMultiplier : 1f;
            float droneIrMultiplier = loadout.countermeasure != null ? loadout.countermeasure.infraredSignatureMultiplier : 1f;
            // Depth pass: each externally-mounted missile is itself a radar reflector
            // hanging in the airstream — adds a fraction of its own RCS to the
            // carrier's exposed signature. Internally-carried rounds (within the bay's
            // internalCapacity) contribute nothing, same as before.
            float externalOrdnanceRcs = stats.externallyMountedAmmoCount * missileStats.radarCrossSection * 0.5f;
            stats.radarCrossSection = loadout.airframe.baseRadarCrossSection * loadout.hullMaterial.radarCrossSectionMultiplier * droneRcsMultiplier
                                       + externalOrdnanceRcs;
            stats.infraredSignature = (loadout.propulsion.infraredSignature + loadout.engine.infraredSignature) * droneIrMultiplier;

            // Phase 1 simplification: health derived from hull armor rating + a flat base.
            stats.maxHealth = 50f + loadout.hullMaterial.armorRating * 10f;

            stats.sensorRangeMeters = loadout.sensorSuite.radarRangeMeters;
            stats.sensorFieldOfViewDegrees = loadout.sensorSuite.radarFieldOfViewDegrees;
            stats.sharesContactsWithTeam = loadout.sensorSuite.sharesContactsWithTeam;

            return stats;
        }
    }
}
