using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vanquish.Combat;
using Vanquish.Simulation.Flight;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Fixed-wing flight-model rework: builds a standalone "little flying rectangle"
    /// test rig — a plain stretched-cube body with FlightBody configured directly in
    /// aerodynamic-lift mode (bypassing DroneLoadout/DesignStatsCalculator/
    /// VehicleFactory entirely) and a live PlayerDroneController, so thrust/lift/
    /// maneuvering can be validated and felt in isolation before any real fixed-wing
    /// craft content exists, per the plan's own ask ("first, a little flying rectangle
    /// where thrust, lift and maneuvering works correctly"). Deliberately a disposable
    /// prototype scene, not wired into the Workshop/Combat flow, MainMenu, or any
    /// ScenarioDefinition — open it directly and press Play.
    /// </summary>
    public static class Phase3GFixedWingPrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/FixedWingPrototype.unity";

        [MenuItem("Vanquish/Phase 3G/Build Fixed-Wing Prototype Scene")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Phase3GFixedWingPrototypeSceneBuilder] Cannot rebuild while in Play mode.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Phase1CombatSceneBuilder.BuildGround();
            Phase1CombatSceneBuilder.BuildLight();

            GameObject rig = BuildFlyingRectangle(new Vector3(0f, 60f, 0f));

            Phase1CombatSceneBuilder.BuildCamera(rig.transform);

            System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Phase1CombatSceneBuilder.EnsureSceneInBuildSettings(ScenePath);

            Debug.Log($"[Phase3GFixedWingPrototypeSceneBuilder] Scene built and saved to {ScenePath}. " +
                "Open it and press Play: A/D roll, W/S pitch, Shift/Space throttle up/down. Spawns at 60m " +
                "altitude with the throttle already at full so it's airborne immediately rather than needing " +
                "a takeoff roll from the ground.");
        }

        /// <summary>
        /// Hand-tuned, real-world-plausible numbers for a small prototype aircraft —
        /// deliberately NOT derived from any seeded DroneAirframeDefinition/
        /// WingOrPropellerDefinition/PropulsionDefinition asset, since the entire
        /// point of this rig is to validate FlightBody's aerodynamic model in
        /// isolation before any real part content depends on it. Worked backward from
        /// "cruise at ~25 m/s (90 km/h) at the wing's referenceAoADegrees" to pick
        /// liftCoefficient, and from "thrust comfortably exceeds cruise drag but isn't
        /// absurd" for thrustNewtons — see the inline math in each comment.
        /// </summary>
        private static GameObject BuildFlyingRectangle(Vector3 spawnPosition)
        {
            const float mass = 20f; // kg
            const float dragCoefficient = 0.05f; // parasite drag
            const float maxGForce = 8f; // gentle for a hand-tuned prototype, not a fighter
            const float zeroLiftAoA = -2f;
            const float referenceAoA = 5f;
            const float criticalAoA = 15f;
            const float inducedDragFactor = 0.02f;

            // liftCoefficient * cruiseSpeed^2 * 1.0(at referenceAoA) == weight
            // weight = 20kg * 9.81 = 196.2N; cruiseSpeed = 25 m/s -> liftCoefficient = 196.2 / 625.
            const float cruiseSpeed = 25f;
            float weight = mass * 9.81f;
            float liftCoefficient = weight / (cruiseSpeed * cruiseSpeed);

            // Thrust well above cruise drag (drag at cruise, referenceAoA lift factor=1:
            // (dragCoefficient + inducedDragFactor) * cruiseSpeed^2 =~ 44N) so the rig
            // can climb and accelerate, without being absurdly overpowered.
            const float thrustNewtons = 180f;

            var rig = new GameObject("FixedWingPrototype");
            rig.transform.position = spawnPosition;
            rig.transform.rotation = Quaternion.identity;

            var collider = rig.AddComponent<BoxCollider>();
            collider.size = new Vector3(4f, 0.3f, 1.2f); // roughly matches the visual below

            var rb = rig.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.mass = mass;

            var flightBody = rig.AddComponent<FlightBody>();
            flightBody.Configure(mass, thrustNewtons, dragCoefficient, maxGForce, liftCoefficient,
                zeroLiftAoA, referenceAoA, criticalAoA, inducedDragFactor);
            flightBody.isThrusting = true;
            flightBody.orientToVelocity = false; // player-piloted, see PlayerDroneController
            flightBody.throttleFraction = 1f; // spawn already at cruise throttle, not idle-on-the-ground

            var controller = rig.AddComponent<PlayerDroneController>();

            var telemetry = rig.AddComponent<FixedWingPrototypeTelemetry>();
            telemetry.flightBody = flightBody;
            telemetry.body = rb;

            BuildRectangleVisual(rig.transform);

            return rig;
        }

        /// <summary>
        /// The literal "rectangle" — a single stretched, brightly-colored cube. No
        /// wings/fuselage/tail silhouette at all: this rig is about validating the
        /// physics, not previewing art, and any silhouette would risk implying this is
        /// meant to look like real content (DroneVisualBuilder's job, once real
        /// fixed-wing parts exist on top of this validated model).
        /// </summary>
        private static void BuildRectangleVisual(Transform parent)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "RectangleVisual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(parent, worldPositionStays: false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(4f, 0.3f, 1.2f); // wide/flat/short — reads as a "flying rectangle"

            var renderer = visual.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = new Color(1f, 0.55f, 0.1f) }; // bright orange, easy to see against sky/ground
            renderer.sharedMaterial = material;
        }
    }
}
