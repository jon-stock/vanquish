using System.Collections.Generic;
using UnityEngine;
using Vanquish.Combat;
using Vanquish.Data;
using Vanquish.Data.Drones;
using Vanquish.Simulation.Flight;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// The actual playable Phase 1 combat instance — two flyable multirotor units
    /// (a quadcopter and a hexacopter, switchable with 1/2 — see
    /// <see cref="PlayerUnitSwitcher"/>), each with its own independent missile
    /// stockpile, against a physical, destructible "Base" target. Built entirely in
    /// code at Start() (no imported art/prefabs). This is where the Phase 0/1
    /// stockpile-economy/damage/win-condition logic (which previously only had OnGUI
    /// debug-button coverage — see EngagementDebugHarness) becomes an actual
    /// real-time 3D game, reusing/adapting the pre-pivot project's flight-control and
    /// procedural-visual layer (PlayerDroneController, WeaponController,
    /// MissileFactory, DroneVisualBuilder, ChaseCamera, etc. — see AGENTS.md) wired
    /// to this pivot's EngagementController/BaseObjective/Damageable instead of that
    /// project's old CombatManager/Health.
    ///
    /// To use: open Assets/_Project/Scenes/Phase1_FlightTest.unity and press Play.
    /// </summary>
    public class FlightTestHarness : MonoBehaviour
    {
        private const string QuadMissilePartId = "flighttest.quad.missile";
        private const string HexMissilePartId = "flighttest.hex.missile";
        private const int StartingMissileCount = 4;

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

            UnitBuildResult quad = BuildUnit(
                "Quadcopter", new Vector3(-2.5f, 5f, 0f), QuadMissilePartId,
                DroneRotorConfiguration.Quadcopter, new Color(0.2f, 0.7f, 0.9f), engagementController);

            UnitBuildResult hex = BuildUnit(
                "Hexacopter", new Vector3(2.5f, 5f, 0f), HexMissilePartId,
                DroneRotorConfiguration.Hexacopter, new Color(0.9f, 0.55f, 0.15f), engagementController);

            CameraModeController cameraModeController = BuildCamera(objective.transform);
            PlayerUnitSwitcher switcher = BuildSwitcher(cameraModeController, quad, hex);

            // Don't rely solely on PlayerUnitSwitcher.Start() to pick the initial
            // active unit — see this method's own doc comment on why Build() must be
            // self-sufficient outside Play mode too.
            switcher.SetActive(0);

            BuildHud(engagementController, objective);
            BuildUnitRoster(switcher, quad, hex);
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
            var quadPart = MakeMissilePart(QuadMissilePartId, "Quadcopter Missile");
            var hexPart = MakeMissilePart(HexMissilePartId, "Hexacopter Missile");

            var controllerGo = new GameObject("EngagementController");
            var controller = controllerGo.AddComponent<EngagementController>();
            controller.Objective = objective;
            controller.attackerLoadout = new[]
            {
                // Two independent stockpile entries — one per unit's own part id — so
                // firing one unit's missiles never touches the other's count, while
                // EngagementController's existing "attacker fully depleted" check
                // (which already requires ALL entries to be empty) correctly means
                // "both units are out of ammo" with no extra code needed.
                new StockpileEntry { part = quadPart, startingCount = StartingMissileCount, rawDamage = 30f, payloadSize = 20f },
                new StockpileEntry { part = hexPart, startingCount = StartingMissileCount, rawDamage = 30f, payloadSize = 20f },
            };
            controller.defenderLoadout = new StockpileEntry[0];
            controller.timeLimitSeconds = 180f;
            controller.Initialize();
            return controller;
        }

        private static DroneAirframeDefinition MakeMissilePart(string id, string displayName)
        {
            var part = ScriptableObject.CreateInstance<DroneAirframeDefinition>();
            part.id = id;
            part.displayName = displayName;
            return part;
        }

        private class UnitBuildResult
        {
            public string Label;
            public GameObject GameObject;
            public WeaponController Weapon;
            public PlayerDroneController Controller;
        }

        private static UnitBuildResult BuildUnit(
            string label, Vector3 spawnPosition, string missilePartId,
            DroneRotorConfiguration rotorConfiguration, Color color, EngagementController engagementController)
        {
            var drone = new GameObject(label);
            drone.transform.position = spawnPosition;

            var rigidbody = drone.AddComponent<Rigidbody>();
            rigidbody.linearDamping = 0f;

            var collider = drone.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 0.5f, 1f);

            var flightBody = drone.AddComponent<FlightBody>();
            flightBody.Configure(mass: 8f, thrust: 0f, drag: 1.2f, maxG: 6f, gravity: false, orientToVel: false);

            Transform visualRoot = DroneVisualBuilder.Build(
                drone.transform, color, rotorConfiguration,
                out Transform[] hardpoints, hardpointCount: StartingMissileCount);

            var tilt = drone.AddComponent<QuadcopterTiltVisual>();
            tilt.body = rigidbody;
            tilt.visualRoot = visualRoot;

            var weapon = drone.AddComponent<WeaponController>();
            weapon.engagementController = engagementController;
            weapon.missilePartId = missilePartId;
            weapon.target = engagementController.Objective.Damageable.transform;
            weapon.fireCooldownSeconds = 1.2f;
            weapon.launchOffset = new Vector3(0f, -0.2f, 0.6f);

            // Mount one visible missile prop per hardpoint (matching StartingMissileCount)
            // and wire them to visually deplete as this unit's WeaponController.Fire() succeeds.
            var mountedVisuals = new List<Transform>();
            foreach (Transform hardpoint in hardpoints)
                mountedVisuals.Add(DroneVisualBuilder.BuildMountedMissileProp(hardpoint).transform);

            var mountedMissileVisuals = drone.AddComponent<MountedMissileVisuals>();
            mountedMissileVisuals.Initialize(weapon, mountedVisuals);

            var controller = drone.AddComponent<PlayerDroneController>();

            return new UnitBuildResult { Label = label, GameObject = drone, Weapon = weapon, Controller = controller };
        }

        private static CameraModeController BuildCamera(Transform objectiveTransform)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();

            var chase = cameraGo.AddComponent<ChaseCamera>();

            var modeGo = new GameObject("CameraModeController");
            var mode = modeGo.AddComponent<CameraModeController>();
            mode.chaseCamera = chase;
            mode.objective = objectiveTransform;
            return mode;
        }

        private static PlayerUnitSwitcher BuildSwitcher(CameraModeController cameraModeController, UnitBuildResult quad, UnitBuildResult hex)
        {
            var switcherGo = new GameObject("PlayerUnitSwitcher");
            var switcher = switcherGo.AddComponent<PlayerUnitSwitcher>();
            switcher.cameraModeController = cameraModeController;
            switcher.units.Add(new PlayerUnitSwitcher.UnitEntry { label = quad.Label, controller = quad.Controller, cameraTarget = quad.GameObject.transform });
            switcher.units.Add(new PlayerUnitSwitcher.UnitEntry { label = hex.Label, controller = hex.Controller, cameraTarget = hex.GameObject.transform });
            return switcher;
        }

        private static void BuildHud(EngagementController engagementController, BaseObjective objective)
        {
            var hudGo = new GameObject("HUD");
            var hud = hudGo.AddComponent<FlightHUD>();
            hud.engagementController = engagementController;
            hud.objective = objective;
        }

        private static void BuildUnitRoster(PlayerUnitSwitcher switcher, UnitBuildResult quad, UnitBuildResult hex)
        {
            var rosterGo = new GameObject("UnitRosterHud");
            var roster = rosterGo.AddComponent<UnitRosterHud>();
            roster.switcher = switcher;
            roster.entries.Add(new UnitRosterHud.RosterEntry { label = quad.Label, weapon = quad.Weapon });
            roster.entries.Add(new UnitRosterHud.RosterEntry { label = hex.Label, weapon = hex.Weapon });
        }
    }
}
