using UnityEngine;

namespace Vanquish.Combat
{
    /// <summary>
    /// Procedurally builds a multirotor drone's visual mesh — central body, N arms in
    /// an "X" configuration, and a spinning rotor blade at each arm tip — entirely
    /// from Unity primitives. No imported 3D models needed, matching the project's
    /// existing convention of composing GameObjects from primitives in code (see
    /// Phase0TestSceneBuilder's missile capsule). Revisit with real modeled/imported
    /// assets in Phase 3's art pass; this is intentionally a cheap prototype
    /// silhouette, not a final look.
    ///
    /// `rotorCount` will eventually come from DroneAirframeDefinition.rotorCount once
    /// the quadcopter→hexacopter upgrade path (Phase 2B) exists; for now VehicleFactory
    /// hardcodes 4 to match the Tier-0 SmallQuad airframe.
    /// </summary>
    public static class DroneVisualBuilder
    {
        public static Transform BuildMultirotorVisual(Transform parent, int rotorCount = 4, float armLength = 0.9f)
        {
            var root = new GameObject("Visual");
            root.transform.SetParent(parent, worldPositionStays: false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            BuildBody(root.transform);

            int clampedRotorCount = Mathf.Max(3, rotorCount);
            float angleStep = 360f / clampedRotorCount;
            for (int i = 0; i < clampedRotorCount; i++)
            {
                // Start at 45 degrees for an "X" configuration (arms between the body's
                // forward/back/left/right axes), the common FPV/multirotor look.
                float angleDeg = 45f + i * angleStep;
                BuildArmAndRotor(root.transform, angleDeg, armLength);
            }

            return root.transform;
        }

        private static void BuildBody(Transform parent)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            DestroyCollider(body);
            body.transform.SetParent(parent, worldPositionStays: false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.6f, 0.25f, 0.6f);
        }

        private static void BuildArmAndRotor(Transform parent, float angleDeg, float armLength)
        {
            Quaternion armRotation = Quaternion.Euler(0f, angleDeg, 0f);
            Vector3 tipPosition = armRotation * Vector3.forward * armLength;

            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Arm";
            DestroyCollider(arm);
            arm.transform.SetParent(parent, worldPositionStays: false);
            arm.transform.localRotation = armRotation;
            arm.transform.localPosition = tipPosition * 0.5f;
            arm.transform.localScale = new Vector3(0.08f, 0.05f, armLength);

            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "RotorHub";
            DestroyCollider(hub);
            hub.transform.SetParent(parent, worldPositionStays: false);
            hub.transform.localPosition = tipPosition + Vector3.up * 0.05f;
            hub.transform.localScale = new Vector3(0.12f, 0.03f, 0.12f);

            // Spin pivot is a sibling of the hub (not its child) so it doesn't inherit
            // the hub's non-uniform scale when positioning/sizing the blade.
            var spinPivot = new GameObject("RotorSpin");
            spinPivot.transform.SetParent(parent, worldPositionStays: false);
            spinPivot.transform.localPosition = tipPosition + Vector3.up * 0.08f;
            var spinner = spinPivot.AddComponent<RotorSpinner>();
            spinner.degreesPerSecond = 1600f;

            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blades";
            DestroyCollider(blade);
            blade.transform.SetParent(spinPivot.transform, worldPositionStays: false);
            blade.transform.localPosition = Vector3.zero;
            blade.transform.localScale = new Vector3(armLength * 0.55f, 0.01f, 0.05f);
        }

        private static void DestroyCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(collider);
            else
                Object.DestroyImmediate(collider);
        }
    }
}
