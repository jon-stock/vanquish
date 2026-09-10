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
    /// <see cref="PlanPartCatalog"/>), not just its category/accent color.
    /// </summary>
    public static class PlanPreviewBuilder
    {
        public static GameObject Build(Transform parent, DronePlan plan, float scale = 0.6f)
        {
            var root = new GameObject($"Preview_{plan.Name}");
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * scale;

            if (plan.Category == UnitCategory.Missile)
                BuildMissilePreview(root.transform, plan);
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

        /// <summary>
        /// A three-part missile showcase — nose (tinted by the chosen Warhead),
        /// body (length driven by the chosen Propulsion's size), and small tail
        /// fins (tinted by the chosen Guidance, omitted entirely for an unguided
        /// design) — so a missile design's actual parts read as visually distinct,
        /// not just one plain accent-colored capsule.
        /// </summary>
        private static void BuildMissilePreview(Transform parent, DronePlan plan)
        {
            PlanPartOption? warhead = PlanPartCatalog.Find(PartSlot.Warhead, plan.WarheadId);
            PlanPartOption? guidance = PlanPartCatalog.Find(PartSlot.Guidance, plan.GuidanceId);
            PlanPartOption? propulsion = PlanPartCatalog.Find(PartSlot.Propulsion, plan.PropulsionId);

            float bodyLengthScale = propulsion?.VisualSizeMultiplier ?? 1f;
            Color bodyColor = propulsion?.VisualColor ?? plan.AccentColor;
            Color noseColor = warhead?.VisualColor ?? plan.AccentColor;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(parent, false);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.16f, 0.4f * bodyLengthScale, 0.16f);
            Tint(body, bodyColor);

            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nose.name = "Nose";
            nose.transform.SetParent(parent, false);
            nose.transform.localPosition = new Vector3(0f, 0.4f * bodyLengthScale + 0.06f, 0f);
            nose.transform.localScale = new Vector3(0.17f, 0.14f, 0.17f);
            Tint(nose, noseColor);

            if (guidance != null && guidance.Value.Id != PlanPartCatalog.NoGuidanceId)
                BuildFins(parent, bodyLengthScale, guidance.Value.VisualColor);
        }

        private static void BuildFins(Transform parent, float bodyLengthScale, Color finColor)
        {
            float tailY = -0.4f * bodyLengthScale;
            foreach (float angleDeg in new[] { 0f, 90f, 180f, 270f })
            {
                GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fin.name = "Fin";
                fin.transform.SetParent(parent, false);
                fin.transform.localRotation = Quaternion.Euler(0f, angleDeg, 0f);
                fin.transform.localPosition = Quaternion.Euler(0f, angleDeg, 0f) * new Vector3(0.09f, 0f, 0f) + new Vector3(0f, tailY, 0f);
                fin.transform.localScale = new Vector3(0.12f, 0.02f, 0.06f);
                Tint(fin, finColor);
            }
        }

        private static void Tint(GameObject go, Color color)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
        }
    }
}
