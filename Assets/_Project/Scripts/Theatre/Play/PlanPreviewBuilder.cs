using UnityEngine;
using Vanquish.Combat.Play;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// Builds a small, static (no physics/flight components — purely a showcase
    /// prop) 3D preview of a <see cref="DronePlan"/>, so a design can actually be
    /// seen — not just named in a list — both at the Lab that designed it and at
    /// any Factory building it. Reuses the exact same procedural visual builder the
    /// flyable combat-instance drones use (<see cref="DroneVisualBuilder"/>) for
    /// quad/hexacopter plans, so a plan's in-world preview looks like the same
    /// hardware it'll actually fly as once built — including its actual selected
    /// Propeller/Battery parts (blade color/size, battery size — see
    /// <see cref="PlanPartCatalog"/>), not just its category/accent color. Missile
    /// plans are built from scratch by <see cref="MissileVisualBuilder"/>.
    /// </summary>
    public static class PlanPreviewBuilder
    {
        public static GameObject Build(Transform parent, DronePlan plan, float scale = 0.6f)
        {
            var root = new GameObject($"Preview_{plan.Name}");
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * scale;

            if (plan.Category == UnitCategory.Missile)
                MissileVisualBuilder.Build(root.transform, plan);
            else
                BuildMultirotorPreview(root.transform, plan);

            return root;
        }

        private static void BuildMultirotorPreview(Transform parent, DronePlan plan)
        {
            DroneRotorConfiguration rotorConfiguration = plan.Category == UnitCategory.Hexacopter
                ? DroneRotorConfiguration.Hexacopter
                : DroneRotorConfiguration.Quadcopter;

            PlanPartOption? propeller = PlanPartCatalog.Find(PartSlot.Propeller, plan.PropellerId);
            PlanPartOption? battery = PlanPartCatalog.Find(PartSlot.Battery, plan.BatteryId);

            // hardpointCount: 0 — this is a showcase model, not a combat unit; it
            // doesn't need mounted-missile sockets.
            DroneVisualBuilder.Build(parent, plan.AccentColor, rotorConfiguration, out _, hardpointCount: 0,
                bladeColor: propeller?.VisualColor,
                bladeSizeMultiplier: propeller?.VisualSizeMultiplier ?? 1f,
                batteryColor: battery?.VisualColor,
                batterySizeMultiplier: battery?.VisualSizeMultiplier ?? 1f);
        }
    }
}
