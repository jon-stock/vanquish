using UnityEngine;
using UnityEngine.SceneManagement;
using Vanquish.Core;
using Vanquish.Data;
using Vanquish.Data.Drones;
using Vanquish.Data.Missiles;
using Vanquish.Data.Shared;
using Vanquish.Data.TechTree;

namespace Vanquish.Workshop
{
    /// <summary>
    /// Phase 1 MVP Workshop: shows the linear tech tree with unlock buttons, the
    /// resulting missile/drone design's computed stats once enough parts are
    /// unlocked, and a button to enter combat. Implemented with OnGUI immediate-mode
    /// rendering — no art/UI assets required, matching the MVP's "ugly art is fine"
    /// scope (same rationale as HUDController). Replace with a proper UI in Phase 3.
    /// </summary>
    public class WorkshopController : MonoBehaviour
    {
        public TechNode[] techTree;
        public string combatSceneName = "Combat_Arena01";

        [Header("Tier-0 Parts (for design stat preview)")]
        public MissileAirframeDefinition missileAirframe;
        public MissileEngineDefinition missileEngine;
        public SeekerDefinition missileSeeker;
        public MissilePayloadDefinition missilePayload;
        public FuelDefinition missileFuel;

        public PropulsionDefinition dronePropulsion;
        public DroneAirframeDefinition droneAirframe;
        public WingOrPropellerDefinition droneWing;
        public HullMaterialDefinition droneHull;
        public DroneEngineDefinition droneEngine;
        public FuelDefinition droneFuel;
        public WeaponBayDefinition droneWeaponBay;
        public SensorSuiteDefinition droneSensorBasic;
        public SensorSuiteDefinition droneSensorScout;

        private Vector2 _techTreeScroll;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;

        private void Start()
        {
            if (PlayerProgress.Instance != null)
                PlayerProgress.Instance.Load();
        }

        private void OnGUI()
        {
            _headerStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            _labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.white } };

            var progress = PlayerProgress.Instance;

            GUI.Box(new Rect(10, 10, 300, 40), GUIContent.none);
            GUI.Label(new Rect(20, 15, 280, 30), $"Currency: {(progress != null ? progress.Currency : 0)}", _headerStyle);

            DrawTechTree(progress);
            DrawDesignPreview(progress);
            DrawEnterCombatButton(progress);
        }

        private void DrawTechTree(PlayerProgress progress)
        {
            const float panelX = 10, panelY = 60, panelW = 340, panelH = 500;
            GUI.Box(new Rect(panelX, panelY, panelW, panelH), GUIContent.none);
            GUI.Label(new Rect(panelX + 10, panelY + 5, panelW - 20, 25), "Tech Tree", _headerStyle);

            _techTreeScroll = GUI.BeginScrollView(
                new Rect(panelX + 5, panelY + 35, panelW - 10, panelH - 45),
                _techTreeScroll,
                new Rect(0, 0, panelW - 30, techTree.Length * 60));

            for (int i = 0; i < techTree.Length; i++)
            {
                TechNode node = techTree[i];
                float y = i * 60;
                bool unlocked = progress != null && progress.IsUnlocked(node);
                bool prereqsMet = ArePrerequisitesMet(progress, node);
                bool affordable = progress != null && progress.CanAfford(node.researchCost);

                GUI.Label(new Rect(5, y, 220, 25), node.displayName, _labelStyle);
                GUI.Label(new Rect(5, y + 22, 220, 20), unlocked ? "Unlocked" : $"Cost: {node.researchCost}", _labelStyle);

                GUI.enabled = !unlocked && prereqsMet && affordable && progress != null;
                if (GUI.Button(new Rect(230, y + 5, 80, 30), unlocked ? "Done" : "Unlock"))
                {
                    progress.TryUnlock(node);
                }
                GUI.enabled = true;
            }

            GUI.EndScrollView();
        }

        private static bool ArePrerequisitesMet(PlayerProgress progress, TechNode node)
        {
            if (progress == null)
                return false;
            foreach (var prereq in node.prerequisites)
            {
                if (!progress.IsUnlocked(prereq))
                    return false;
            }
            return true;
        }

        private void DrawDesignPreview(PlayerProgress progress)
        {
            const float panelX = 360, panelY = 60, panelW = 380, panelH = 500;
            GUI.Box(new Rect(panelX, panelY, panelW, panelH), GUIContent.none);
            GUI.Label(new Rect(panelX + 10, panelY + 5, panelW - 20, 25), "Current Design", _headerStyle);

            float y = panelY + 40;

            if (TryBuildMissileLoadout(progress, out MissileLoadout missileLoadout))
            {
                var stats = DesignStatsCalculator.Calculate(missileLoadout);
                GUI.Label(new Rect(panelX + 10, y, panelW - 20, 20), "Missile: READY", _labelStyle); y += 22;
                GUI.Label(new Rect(panelX + 10, y, panelW - 20, 20), $"  Mass: {stats.massKg:F0} kg  Thrust: {stats.thrustNewtons:F0} N", _labelStyle); y += 20;
                GUI.Label(new Rect(panelX + 10, y, panelW - 20, 20), $"  Damage: {stats.directDamage:F0} (+{stats.splashDamage:F0} splash, {stats.blastRadiusMeters:F0}m)", _labelStyle); y += 20;
                GUI.Label(new Rect(panelX + 10, y, panelW - 20, 20), $"  Seeker range: {stats.seekerRangeMeters:F0} m", _labelStyle); y += 28;
            }
            else
            {
                GUI.Label(new Rect(panelX + 10, y, panelW - 20, 20), "Missile: incomplete — unlock more tech", _labelStyle); y += 28;
            }

            if (TryBuildDroneLoadout(progress, missileLoadout, includeWeapon: true, out DroneLoadout droneLoadout))
            {
                var stats = DesignStatsCalculator.Calculate(droneLoadout);
                GUI.Label(new Rect(panelX + 10, y, panelW - 20, 20), "Strike Drone: READY", _labelStyle); y += 22;
                GUI.Label(new Rect(panelX + 10, y, panelW - 20, 20), $"  Mass: {stats.massKg:F0} kg  Health: {stats.maxHealth:F0}", _labelStyle); y += 20;
                GUI.Label(new Rect(panelX + 10, y, panelW - 20, 20), $"  Sensor range: {stats.sensorRangeMeters:F0} m", _labelStyle); y += 28;
            }
            else
            {
                GUI.Label(new Rect(panelX + 10, y, panelW - 20, 20), "Strike Drone: incomplete — unlock more tech", _labelStyle); y += 28;
            }

            if (TryBuildDroneLoadout(progress, null, includeWeapon: false, out DroneLoadout scoutLoadout, useScoutSensor: true))
            {
                var stats = DesignStatsCalculator.Calculate(scoutLoadout);
                GUI.Label(new Rect(panelX + 10, y, panelW - 20, 20), "Scout Drone: READY", _labelStyle); y += 22;
                GUI.Label(new Rect(panelX + 10, y, panelW - 20, 20), $"  Sensor range: {stats.sensorRangeMeters:F0} m (shares contacts)", _labelStyle); y += 20;
            }
            else
            {
                GUI.Label(new Rect(panelX + 10, y, panelW - 20, 20), "Scout Drone: incomplete — unlock more tech", _labelStyle);
            }
        }

        private void DrawEnterCombatButton(PlayerProgress progress)
        {
            bool ready = TryBuildMissileLoadout(progress, out _) &&
                         TryBuildDroneLoadout(progress, null, true, out _) &&
                         TryBuildDroneLoadout(progress, null, false, out _, useScoutSensor: true);

            GUI.enabled = ready;
            if (GUI.Button(new Rect(360, 570, 380, 50), ready ? "Enter Combat" : "Unlock more tech to proceed"))
            {
                SceneManager.LoadScene(combatSceneName);
            }
            GUI.enabled = true;
        }

        private bool TryBuildMissileLoadout(PlayerProgress progress, out MissileLoadout loadout)
        {
            loadout = new MissileLoadout { designName = "Basic Missile" };
            if (progress == null)
                return false;

            loadout.airframe = IsUnlocked(progress, missileAirframe) ? missileAirframe : null;
            loadout.engine = IsUnlocked(progress, missileEngine) ? missileEngine : null;
            loadout.seeker = IsUnlocked(progress, missileSeeker) ? missileSeeker : null;
            loadout.payload = IsUnlocked(progress, missilePayload) ? missilePayload : null;
            loadout.fuel = IsUnlocked(progress, missileFuel) ? missileFuel : null;
            return loadout.IsComplete;
        }

        private bool TryBuildDroneLoadout(PlayerProgress progress, MissileLoadout missileLoadout, bool includeWeapon,
            out DroneLoadout loadout, bool useScoutSensor = false)
        {
            loadout = new DroneLoadout { designName = useScoutSensor ? "Basic Scout Drone" : "Basic Strike Drone" };
            if (progress == null)
                return false;

            loadout.propulsion = IsUnlocked(progress, dronePropulsion) ? dronePropulsion : null;
            loadout.airframe = IsUnlocked(progress, droneAirframe) ? droneAirframe : null;
            loadout.wingOrPropeller = IsUnlocked(progress, droneWing) ? droneWing : null;
            loadout.hullMaterial = IsUnlocked(progress, droneHull) ? droneHull : null;
            loadout.engine = IsUnlocked(progress, droneEngine) ? droneEngine : null;
            loadout.fuel = IsUnlocked(progress, droneFuel) ? droneFuel : null;
            loadout.weaponBay = IsUnlocked(progress, droneWeaponBay) ? droneWeaponBay : null;

            SensorSuiteDefinition sensor = useScoutSensor ? droneSensorScout : droneSensorBasic;
            loadout.sensorSuite = IsUnlocked(progress, sensor) ? sensor : null;

            if (includeWeapon)
            {
                loadout.missileLoadout = missileLoadout;
                loadout.ammoCount = 4;
            }

            return loadout.IsComplete;
        }

        private bool IsUnlocked(PlayerProgress progress, PartDefinition part)
        {
            return progress != null && progress.IsPartUnlocked(part, techTree);
        }
    }
}
