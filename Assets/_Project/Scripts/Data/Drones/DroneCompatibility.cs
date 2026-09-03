using Vanquish.Data.Shared;

namespace Vanquish.Data.Drones
{
    /// <summary>
    /// Resolves which FlightConfiguration each drone part slot implies, and checks
    /// that a whole DroneLoadout's parts agree on one. Before this existed, nothing in
    /// the Workshop or DesignStatsCalculator prevented (for example) equipping a
    /// SubsonicJet PropulsionDefinition — which forces FlightBody into
    /// constant-thrust/orient-to-velocity/aerodynamic-lift mode — onto a SmallQuad
    /// airframe with Propeller-type wings, producing an incoherent design that neither
    /// flies like a multirotor nor like a fixed-wing aircraft. This is the single
    /// place that mapping lives, used by both WorkshopController (to filter the new
    /// "Airframe Type" toggle's part options to compatible choices) and
    /// DesignStatsCalculator (to flag an inconsistent design as not combat-ready, the
    /// same way an over-MTOW design already is).
    /// </summary>
    public static class DroneCompatibility
    {
        public static FlightConfiguration GetFlightConfiguration(DroneAirframeDefinition airframe)
        {
            // rotorCount is already the established source of truth for "is this a
            // multirotor silhouette" — see DroneVisualBuilder/VehicleFactory, which
            // both already branch on rotorCount > 0 rather than airframeClass directly.
            return airframe != null && airframe.rotorCount > 0 ? FlightConfiguration.Multirotor : FlightConfiguration.FixedWing;
        }

        public static FlightConfiguration GetFlightConfiguration(WingOrPropellerDefinition wingOrPropeller)
        {
            return wingOrPropeller != null && wingOrPropeller.liftSurfaceType == LiftSurfaceType.Propeller
                ? FlightConfiguration.Multirotor
                : FlightConfiguration.FixedWing;
        }

        public static FlightConfiguration GetFlightConfiguration(PropulsionDefinition propulsion)
        {
            return propulsion != null && propulsion.requiresForwardFlight
                ? FlightConfiguration.FixedWing
                : FlightConfiguration.Multirotor;
        }

        public static FlightConfiguration GetFlightConfiguration(DroneEngineDefinition engine)
        {
            return engine != null && engine.requiresForwardFlight
                ? FlightConfiguration.FixedWing
                : FlightConfiguration.Multirotor;
        }

        /// <summary>
        /// True if every part slot that implies a flight configuration
        /// (airframe/wing-or-propeller/propulsion/engine) agrees on the same one.
        /// Sensor suite, hull material, fuel, and weapon bay are flight-model-agnostic
        /// by design (see their own doc comments) and never checked here. Returns
        /// true (nothing to flag) for an incomplete loadout — MTOW-style "not
        /// combat-ready" gating already handles incompleteness separately; this check
        /// is purely about parts that ARE chosen disagreeing with each other.
        /// </summary>
        public static bool IsLoadoutFlightConfigurationConsistent(DroneLoadout loadout, out string mismatchReason)
        {
            mismatchReason = null;
            if (loadout == null)
                return true;

            FlightConfiguration? config = null;
            string configuredBy = null;

            if (!CheckAndSet(GetConfigOrNull(loadout.airframe), "airframe", ref config, ref configuredBy, out mismatchReason))
                return false;
            if (!CheckAndSet(GetConfigOrNull(loadout.wingOrPropeller), "wing/rotor", ref config, ref configuredBy, out mismatchReason))
                return false;
            if (!CheckAndSet(GetConfigOrNull(loadout.propulsion), "propulsion", ref config, ref configuredBy, out mismatchReason))
                return false;
            if (!CheckAndSet(GetConfigOrNull(loadout.engine), "engine", ref config, ref configuredBy, out mismatchReason))
                return false;

            return true;
        }

        private static FlightConfiguration? GetConfigOrNull(DroneAirframeDefinition airframe) =>
            airframe != null ? GetFlightConfiguration(airframe) : (FlightConfiguration?)null;
        private static FlightConfiguration? GetConfigOrNull(WingOrPropellerDefinition wing) =>
            wing != null ? GetFlightConfiguration(wing) : (FlightConfiguration?)null;
        private static FlightConfiguration? GetConfigOrNull(PropulsionDefinition propulsion) =>
            propulsion != null ? GetFlightConfiguration(propulsion) : (FlightConfiguration?)null;
        private static FlightConfiguration? GetConfigOrNull(DroneEngineDefinition engine) =>
            engine != null ? GetFlightConfiguration(engine) : (FlightConfiguration?)null;

        /// <summary>
        /// Depth pass (direct user feedback: "fuel type just affects mass, so you may
        /// as well have a battery powered supersonic jet"): before this existed,
        /// nothing tied a fuel PART's FuelType to the propulsion it was feeding —
        /// DesignStatsCalculator only ever read FuelDefinition.capacityKg for mass,
        /// so a Battery fuel tank in a Supersonic Jet propulsion slot was perfectly
        /// legal (and perfectly nonsensical). Mirrors
        /// IsLoadoutFlightConfigurationConsistent's shape/severity — a real, gating
        /// "not combat-ready" check, not just a cosmetic warning.
        /// </summary>
        public static bool IsFuelCompatible(PropulsionDefinition propulsion, FuelDefinition fuel, out string mismatchReason)
        {
            mismatchReason = null;
            if (propulsion == null || fuel == null)
                return true;

            if (IsFuelTypeCompatible(propulsion.propulsionType, fuel.fuelType))
                return true;

            mismatchReason = $"{propulsion.displayName} needs {DescribeRequiredFuel(propulsion.propulsionType)} fuel, " +
                $"but {fuel.displayName} is {fuel.fuelType}.";
            return false;
        }

        private static bool IsFuelTypeCompatible(PropulsionType propulsionType, FuelType fuelType)
        {
            switch (propulsionType)
            {
                case PropulsionType.Electric:
                    return fuelType == FuelType.Battery;
                case PropulsionType.InternalCombustion:
                    return fuelType == FuelType.Petrol || fuelType == FuelType.Diesel;
                case PropulsionType.SubsonicJet:
                case PropulsionType.SupersonicJet:
                case PropulsionType.Ramjet:
                case PropulsionType.Scramjet:
                    return fuelType == FuelType.JetFuel;
                case PropulsionType.SolidRocket:
                    return fuelType == FuelType.SolidPropellant;
                case PropulsionType.LiquidRocket:
                    return fuelType == FuelType.LiquidPropellant;
                case PropulsionType.HybridRocket:
                    return fuelType == FuelType.HybridPropellant;
                default:
                    return true;
            }
        }

        private static string DescribeRequiredFuel(PropulsionType propulsionType)
        {
            switch (propulsionType)
            {
                case PropulsionType.Electric: return "Battery";
                case PropulsionType.InternalCombustion: return "Petrol or Diesel";
                case PropulsionType.SubsonicJet:
                case PropulsionType.SupersonicJet:
                case PropulsionType.Ramjet:
                case PropulsionType.Scramjet:
                    return "Jet Fuel";
                case PropulsionType.SolidRocket: return "Solid Propellant";
                case PropulsionType.LiquidRocket: return "Liquid Propellant";
                case PropulsionType.HybridRocket: return "Hybrid Propellant";
                default: return "compatible";
            }
        }

        private static bool CheckAndSet(FlightConfiguration? candidate, string slotName,
            ref FlightConfiguration? config, ref string configuredBy, out string mismatchReason)
        {
            mismatchReason = null;
            if (candidate == null)
                return true; // slot not chosen yet — nothing to disagree with

            if (config == null)
            {
                config = candidate;
                configuredBy = slotName;
                return true;
            }

            if (config.Value != candidate.Value)
            {
                mismatchReason = $"{slotName} is {candidate.Value} but {configuredBy} is {config.Value} — " +
                    "every flight-relevant slot must use the same airframe type.";
                return false;
            }

            return true;
        }
    }
}
