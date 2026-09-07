using UnityEngine;
using Vanquish.Combat;
using Vanquish.Data;
using Vanquish.Data.Drones;
using Vanquish.Simulation.Flight;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// The actual playable Phase 1 combat instance — a flyable quadcopter (WASD +
    /// Space/Shift altitude, mouse to fire) against a physical, destructible "Base"
    /// target, built entirely in code at Start() (no imported art/prefabs). This is
    /// where the Phase 0/1 stockpile-economy/damage/win-condition logic (which
    /// previously only had OnGUI debug-button coverage — see EngagementDebugHarness)
    /// becomes an actual real-time 3D game, reusing/adapting the pre-pivot project's
    /// flight-control and procedural-visual layer (PlayerDroneController,
    /// WeaponController, MissileFactory, DroneVisualBuilder, ChaseCamera, etc. — see
    /// AGENTS.md) wired to this pivot's EngagementController/BaseObjective/Damageable
    /// instead of that project's old CombatManager/Health.
    ///
    /// To use: open Assets/_Project/Scenes/Phase1_FlightTest.unity and press Play.
    /// </summary>
    public class FlightTestHarness : MonoBehaviour
    {
        private const string MissilePartId = "flighttest.missile";

        private void Start()
        {
            Build();
        }

        /// <summary>
        /// The actual scene-assembly logic, factored out of <see cref="Start"/> so it
        /// can be driven directly from a headless smoke test (Unity does not
        /// reliably fire Start/Awake for components added to a GameObject outside a
        /// loaded scene/Play mode — same reasoning as EngagementController.Initialize).
        /// </summary>
        public void Build()
        {
            BuildGround();
            BuildLight();

            BaseObjective objective = BuildObjective();
            EngagementController engagementController = BuildEngagementController(objective);
            GameObject drone = BuildPlayerDrone(engagementController);
            BuildCamera(drone.transform, objective.transform);
            BuildHud(engagementController, drone, objective);
        }

        private static void BuildGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(20f, 1f, 20f); // Unity's default plane is 10x10 units at scale 1
            var renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.25f, 0.32f, 0.22f);
        }

        private static void BuildLight()
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static BaseObjective BuildObjective()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Objective (Base)";
            go.transform.position = new Vector3(0f, 2f, 60f);
            go.transform.localScale = new Vector3(6f, 4f, 6f);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.6f, 0.2f, 0.2f);

            var objective = go.AddComponent<BaseObjective>();
            objective.attackerWinDestroyedFraction = 0.75f;
            objective.Damageable.Configure(newMaxHealth: 200f, newHardness: 25f, newSoftCapResidualFraction: 0.2f);
            return objective;
        }

        private static EngagementController BuildEngagementController(BaseObjective objective)
        {
            var missilePart = ScriptableObject.CreateInstance<DroneAirframeDefinition>();
            missilePart.id = MissilePartId;
            missilePart.displayName = "Flight Test Missile";

            var controllerGo = new GameObject("EngagementController");
            var controller = controllerGo.AddComponent<EngagementController>();
            controller.Objective = objective;
            controller.attackerLoadout = new[]
            {
                new StockpileEntry { part = missilePart, startingCount = 8, rawDamage = 30f, payloadSize = 20f },
            };
            controller.defenderLoadout = new StockpileEntry[0];
            controller.timeLimitSeconds = 180f;
            controller.Initialize();
            return controller;
        }

        private static GameObject BuildPlayerDrone(EngagementController engagementController)
        {
            var drone = new GameObject("Player Drone");
            drone.transform.position = new Vector3(0f, 5f, 0f);

            var rigidbody = drone.AddComponent<Rigidbody>();
            rigidbody.linearDamping = 0f;

            var collider = drone.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 0.5f, 1f);

            var flightBody = drone.AddComponent<FlightBody>();
            flightBody.Configure(mass: 8f, thrust: 0f, drag: 1.2f, maxG: 6f, gravity: false, orientToVel: false);

            Transform visualRoot = DroneVisualBuilder.Build(drone.transform, new Color(0.2f, 0.7f, 0.9f));

            var tilt = drone.AddComponent<QuadcopterTiltVisual>();
            tilt.body = rigidbody;
            tilt.visualRoot = visualRoot;

            var weapon = drone.AddComponent<WeaponController>();
            weapon.engagementController = engagementController;
            weapon.missilePartId = MissilePartId;
            weapon.target = engagementController.Objective.Damageable.transform;
            weapon.fireCooldownSeconds = 1.2f;
            weapon.launchOffset = new Vector3(0f, -0.2f, 0.6f);

            drone.AddComponent<PlayerDroneController>();

            return drone;
        }

        private static void BuildCamera(Transform droneTransform, Transform objectiveTransform)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();

            var chase = cameraGo.AddComponent<ChaseCamera>();
            chase.primary = droneTransform;
            chase.secondary = objectiveTransform;
        }

        private static void BuildHud(EngagementController engagementController, GameObject drone, BaseObjective objective)
        {
            var hudGo = new GameObject("HUD");
            var hud = hudGo.AddComponent<FlightHUD>();
            hud.engagementController = engagementController;
            hud.weapon = drone.GetComponent<WeaponController>();
            hud.objective = objective;
        }
    }
}
