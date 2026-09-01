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

        /// <summary>Airframe's MTOW limit in kg. 0 or less means no limit is configured.</summary>
        public float maxTakeOffMassKg;

        /// <summary>True if massKg is within maxTakeOffMassKg (or no limit is configured).</summary>
        public bool isWithinMtow;
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

            stats.maxGForce = loadout.airframe.maxGForce
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

            stats.jamResistance = loadout.seeker.jamResistance
                                   + (loadout.jamming != null ? loadout.jamming.counterJammingStrength : 0f);

            return stats;
        }

        public static DroneRuntimeStats Calculate(DroneLoadout loadout)
        {
            var stats = new DroneRuntimeStats();
            if (loadout == null || !loadout.IsComplete)
                return stats;

            float missileMass = loadout.missileLoadout != null && loadout.missileLoadout.IsComplete
                ? Calculate(loadout.missileLoadout).massKg * loadout.ammoCount
                : 0f;

            // Fuel/battery mass scales with the continuous fill-level slider rather than
            // always assuming a full tank — see DroneLoadout.fuelFillFraction (Phase 2B,
            // mirrors MissileLoadout.fuelFillFraction from 2A).
            stats.fuelMassKg = loadout.fuel.capacityKg * Mathf.Clamp01(loadout.fuelFillFraction);

            stats.massKg = loadout.propulsion.massKg + loadout.airframe.massKg + loadout.airframe.structuralMassKg
                           + loadout.wingOrPropeller.massKg + loadout.hullMaterial.massKg + loadout.engine.massKg
                           + loadout.fuel.massKg + stats.fuelMassKg + loadout.weaponBay.massKg
                           + loadout.sensorSuite.massKg + missileMass;

            stats.maxTakeOffMassKg = loadout.airframe.maxTakeOffMassKg;
            stats.isWithinMtow = stats.maxTakeOffMassKg <= 0f || stats.massKg <= stats.maxTakeOffMassKg;

            // Phase 1 simplification: DroneEngineDefinition.powerOutput is treated directly
            // as thrust in Newtons regardless of propulsion type. Revisit once electric vs.
            // jet propulsion need genuinely different force models.
            stats.thrustNewtons = loadout.engine.powerOutput;
            stats.dragCoefficient = loadout.airframe.dragCoefficient + loadout.wingOrPropeller.dragCoefficient;
            stats.requiresForwardFlight = loadout.propulsion.requiresForwardFlight;

            // Phase 1 simplification: no real turn-rate-to-lateral-G conversion yet.
            // Keep this modest — quadcopters/small drones don't pull fighter-jet-style
            // G-loads; the earlier formula (turnRate/15, ~15G here) let guidance
            // massively overshoot at high speed before it could correct, sending
            // AI-controlled drones rocketing far off the arena.
            stats.maxGForce = 0.5f + (loadout.wingOrPropeller.turnRateDegreesPerSecond / 90f);

            stats.radarCrossSection = loadout.airframe.baseRadarCrossSection * loadout.hullMaterial.radarCrossSectionMultiplier;
            stats.infraredSignature = loadout.propulsion.infraredSignature + loadout.engine.infraredSignature;

            // Phase 1 simplification: health derived from hull armor rating + a flat base.
            stats.maxHealth = 50f + loadout.hullMaterial.armorRating * 10f;

            stats.sensorRangeMeters = loadout.sensorSuite.radarRangeMeters;
            stats.sensorFieldOfViewDegrees = loadout.sensorSuite.radarFieldOfViewDegrees;
            stats.sharesContactsWithTeam = loadout.sensorSuite.sharesContactsWithTeam;

            return stats;
        }
    }
}
