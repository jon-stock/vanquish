using UnityEngine;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// A player-named design created at a <see cref="SiteType.Lab"/>'s Design
    /// window (<see cref="DesignController"/>) — what a Factory can choose to
    /// build. A real (if simplified) part-composition design: a Quadcopter/
    /// Hexacopter picks a Propeller + Battery, a Missile picks a Warhead +
    /// Guidance + Propulsion (see <see cref="PlanPartCatalog"/>), and the chosen
    /// parts drive both this Plan's derived stats (<see cref="WeightKg"/>/
    /// <see cref="SpeedKph"/>/<see cref="RangeKm"/>/<see cref="PayloadKg"/>) and its
    /// actual rendered appearance everywhere it's shown (Lab/Factory/Warehouse/
    /// Airfield/army composition cards — see <see cref="PlanPreviewBuilder"/>/
    /// <see cref="PlanIconRenderer"/>). Mutable (not a readonly record) since the
    /// Design window supports editing an existing Plan in place, not just creating
    /// new ones — see <see cref="ApplyDesign"/>.
    /// </summary>
    public class DronePlan
    {
        public string Name { get; private set; }
        public UnitCategory Category { get; }
        public Color AccentColor { get; }

        // ---- Quadcopter/Hexacopter parts ----
        public string PropellerId { get; private set; }
        public string BatteryId { get; private set; }

        // ---- Missile parts ----
        public string WarheadId { get; private set; }
        public string GuidanceId { get; private set; }
        public string PropulsionId { get; private set; }

        public DronePlan(string name, UnitCategory category, Color accentColor,
            string propellerId = null, string batteryId = null,
            string warheadId = null, string guidanceId = null, string propulsionId = null)
        {
            Name = name;
            Category = category;
            AccentColor = accentColor;
            ApplyDesign(name, propellerId, batteryId, warheadId, guidanceId, propulsionId);
        }

        /// <summary>Updates this Plan's name and part selections in place — how the Design window saves edits to an already-created Plan without breaking references to it held elsewhere (production queues, storage, army composition).</summary>
        public void ApplyDesign(string name, string propellerId, string batteryId, string warheadId, string guidanceId, string propulsionId)
        {
            Name = name;
            PropellerId = string.IsNullOrEmpty(propellerId) ? PlanPartCatalog.DefaultId(PartSlot.Propeller) : propellerId;
            BatteryId = string.IsNullOrEmpty(batteryId) ? PlanPartCatalog.DefaultId(PartSlot.Battery) : batteryId;
            WarheadId = string.IsNullOrEmpty(warheadId) ? PlanPartCatalog.DefaultId(PartSlot.Warhead) : warheadId;
            GuidanceId = string.IsNullOrEmpty(guidanceId) ? PlanPartCatalog.DefaultId(PartSlot.Guidance) : guidanceId;
            PropulsionId = string.IsNullOrEmpty(propulsionId) ? PlanPartCatalog.DefaultId(PartSlot.Propulsion) : propulsionId;
        }

        private PlanPartOption? Propeller => PlanPartCatalog.Find(PartSlot.Propeller, PropellerId);
        private PlanPartOption? Battery => PlanPartCatalog.Find(PartSlot.Battery, BatteryId);
        private PlanPartOption? Warhead => PlanPartCatalog.Find(PartSlot.Warhead, WarheadId);
        private PlanPartOption? Guidance => PlanPartCatalog.Find(PartSlot.Guidance, GuidanceId);
        private PlanPartOption? Propulsion => PlanPartCatalog.Find(PartSlot.Propulsion, PropulsionId);

        // ---- Base hull stats before parts — open tuning numbers, same POC status as everywhere else in this design system ----
        private const float QuadHullWeightKg = 0.9f;
        private const float HexHullWeightKg = 1.4f;
        private const float MissileHullWeightKg = 0.6f;

        /// <summary>Total design weight — hull + every selected part's weight.</summary>
        public float WeightKg
        {
            get
            {
                if (Category == UnitCategory.Missile)
                    return MissileHullWeightKg + (Warhead?.WeightKg ?? 0f) + (Guidance?.WeightKg ?? 0f) + (Propulsion?.WeightKg ?? 0f);

                float hull = Category == UnitCategory.Hexacopter ? HexHullWeightKg : QuadHullWeightKg;
                return hull + (Propeller?.WeightKg ?? 0f) + (Battery?.WeightKg ?? 0f);
            }
        }

        /// <summary>Top speed, in km/h — purely additive across selected parts (a simple stand-in, not an aerodynamic simulation).</summary>
        public float SpeedKph
        {
            get
            {
                if (Category == UnitCategory.Missile)
                    return (Propulsion?.SpeedKph ?? 0f) + (Guidance?.SpeedKph ?? 0f);

                return (Propeller?.SpeedKph ?? 0f) + (Battery?.SpeedKph ?? 0f);
            }
        }

        /// <summary>Operational range, in km.</summary>
        public float RangeKm
        {
            get
            {
                if (Category == UnitCategory.Missile)
                    return (Propulsion?.RangeKm ?? 0f) + (Guidance?.RangeKm ?? 0f);

                return (Propeller?.RangeKm ?? 0f) + (Battery?.RangeKm ?? 0f);
            }
        }

        /// <summary>Payload capacity, in kg (a drone's mountable-munition capacity, or a missile's own warhead payload).</summary>
        public float PayloadKg
        {
            get
            {
                if (Category == UnitCategory.Missile)
                    return Warhead?.PayloadKg ?? 0f;

                return (Propeller?.PayloadKg ?? 0f) + (Battery?.PayloadKg ?? 0f);
            }
        }
    }
}
