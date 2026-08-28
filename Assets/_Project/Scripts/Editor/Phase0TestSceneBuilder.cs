using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vanquish.Combat;
using Vanquish.Simulation.Flight;
using Vanquish.Simulation.Guidance;
using Vanquish.Simulation.Sensors;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Programmatically builds the Phase 0 validation scene: a capsule "missile"
    /// using FlightBody + GuidanceController (PursuitGuidance) chasing a weaving
    /// cube "target" with a Rigidbody, while a DetectionSensor/DetectableSignature
    /// pair validates the binary detection prototype. Also callable headlessly via
    /// `Unity.exe -batchmode -quit -executeMethod Vanquish.EditorTools.Phase0TestSceneBuilder.BuildTestScene`.
    /// </summary>
    public static class Phase0TestSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Phase0_MissileTest.unity";

        [MenuItem("Vanquish/Phase 0/Build Missile Test Scene")]
        public static void BuildTestScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Phase0TestSceneBuilder] Cannot rebuild while in Play mode — changes would be " +
                                "discarded on stop. Press Stop first, then rebuild.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildGround();
            BuildLight();
            Transform target = BuildTarget();
            Transform missile = BuildMissile(target);
            BuildCamera(missile, target);
            BuildTestHarness(missile, target);

            System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"[Phase0TestSceneBuilder] Scene built and saved to {ScenePath}");
        }

        private static void BuildGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(50f, 1f, 50f); // ~500x500m
        }

        private static void BuildLight()
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static Transform BuildTarget()
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "Target_Drone";
            target.transform.position = new Vector3(0f, 5f, 150f);
            target.transform.localScale = new Vector3(2f, 2f, 2f);

            var rb = target.GetComponent<Rigidbody>();
            if (rb == null)
                rb = target.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var mover = target.AddComponent<TargetMover>();
            mover.speedMetersPerSecond = 15f;
            mover.weave = true;
            mover.weaveAmplitude = 8f;
            mover.weaveFrequency = 0.15f;

            var signature = target.AddComponent<DetectableSignature>();
            signature.radarCrossSection = 3f; // larger, easier to detect (a drone-scale target)
            signature.infraredSignature = 2f;

            return target.transform;
        }
        private static Transform BuildMissile(Transform target)
        {
            // Physics root: its rotation IS the true heading (thrust applies along
            // transform.forward), so it must always point exactly where guidance/flight
            // says it does — no mesh-orientation correction is ever baked in here.
            var missile = new GameObject("Missile_Test");
            missile.transform.position = new Vector3(0f, 5f, 0f);

            Vector3 initialDirection = (target.position - missile.transform.position).normalized;
            missile.transform.rotation = Quaternion.LookRotation(initialDirection, Vector3.up);

            // Capsule collider sized to match the visual capsule, oriented along local Z
            // (direction = 2) to match the forward-pointing physics root, instead of the
            // default Y-axis orientation.
            var collider = missile.AddComponent<CapsuleCollider>();
            collider.direction = 2; // Z-axis
            collider.radius = 0.5f;
            collider.height = 2.4f;

            var rb = missile.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.useGravity = false;

            var flightBody = missile.AddComponent<FlightBody>();
            flightBody.Configure(mass: 25f, thrust: 3500f, drag: 0.08f, maxG: 25f);
            flightBody.isThrusting = true;

            var guidance = missile.AddComponent<GuidanceController>();
            guidance.SetTarget(target);

            var signature = missile.AddComponent<DetectableSignature>();
            signature.radarCrossSection = 0.05f; // small, low-observable munition
            signature.infraredSignature = 1.5f; // hot exhaust plume

            var impact = missile.AddComponent<MissileImpact>();

            var sensor = missile.AddComponent<DetectionSensor>();
            sensor.baseRangeMeters = 8000f;
            sensor.scanIntervalSeconds = 0.25f;

            // Proximity fuse: a trigger-only child collider that detonates the warhead
            // once the target enters its radius, rather than requiring a direct hull
            // collision (see MissilePayloadDefinition.requiresProximityFuse).
            var fuseGo = new GameObject("ProximityFuse");
            fuseGo.transform.SetParent(missile.transform, worldPositionStays: false);
            fuseGo.transform.localPosition = Vector3.zero;
            var fuseCollider = fuseGo.AddComponent<SphereCollider>();
            fuseCollider.isTrigger = true;
            fuseCollider.radius = 3f;
            var fuseRelay = fuseGo.AddComponent<ProximityFuseRelay>();
            fuseRelay.owner = impact;

            // Visual-only child: Unity's default Capsule primitive has its long axis
            // along local Y, not Z, so this child is rotated +90 degrees on X to point
            // its nose along the parent's forward (Z) axis. Its own collider is removed
            // since the physics root above already has the real one.
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(missile.transform, worldPositionStays: false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visual.transform.localScale = new Vector3(0.5f, 1.2f, 0.5f);

            return missile.transform;
        }

        private static void BuildCamera(Transform missile, Transform target)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.farClipPlane = 2000f;

            Vector3 midpoint = (missile.position + target.position) * 0.5f;
            camGo.transform.position = midpoint + new Vector3(-80f, 60f, -40f);
            camGo.transform.LookAt(midpoint);

            camGo.AddComponent<AudioListener>();

            var chaseCam = camGo.AddComponent<Phase0ChaseCamera>();
            chaseCam.missile = missile;
            chaseCam.target = target;
        }

        private static void BuildTestHarness(Transform missile, Transform target)
        {
            var harnessGo = new GameObject("Phase0_TestHarness");
            var harness = harnessGo.AddComponent<Phase0TestHarness>();
            harness.missile = missile;
            harness.target = target;
            harness.missileSensor = missile.GetComponent<DetectionSensor>();
            harness.missileImpact = missile.GetComponent<MissileImpact>();
        }
    }
}
