using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Vanquish.Core;
using Vanquish.Data;
using Vanquish.Data.Drones;
using Vanquish.Data.Missiles;
using Vanquish.Data.Shared;
using Vanquish.Data.Support;
using Vanquish.Data.TechTree;

namespace Vanquish.Workshop
{
    /// <summary>
    /// Workshop: shows the linear tech tree with unlock buttons, a real multi-option
    /// part picker for every missile (2A) and drone (2B) slot that has more than one
    /// unlocked variant, the resulting missile/drone design's computed stats once
    /// enough parts are unlocked, and a button to enter combat. Built with UI Toolkit
    /// (UIDocument + Workshop.uxml/.uss under Assets/_Project/UI/Workshop/) rather than
    /// OnGUI. Phase1WorkshopSceneBuilder wires the UIDocument's
    /// visualTreeAsset/panelSettings and all the part option arrays when it builds the
    /// scene. Sensor suites (basic/scout) stay single-option fields rather than picker
    /// slots since they're fixed by drone role (strike vs. scout), not a player choice.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class WorkshopController : MonoBehaviour
    {
        public TechNode[] techTree;
        public string combatSceneName = "Combat_Arena01";

        [Header("Missile: single-option slots (only one variant seeded so far)")]
        public MissileAirframeDefinition missileAirframe;
        public FuelDefinition missileFuel;

        [Header("Missile: multi-option picker slots (2A part breadth)")]
        [Tooltip("Every candidate engine, unlocked or not — the picker filters to unlocked options at runtime.")]
        public MissileEngineDefinition[] missileEngineOptions;
        public SeekerDefinition[] missileSeekerOptions;
        public MissilePayloadDefinition[] missilePayloadOptions;
        [Tooltip("Optional slot — a design can have no countermeasure equipped.")]
        public CountermeasureDefinition[] missileCountermeasureOptions;
        [Tooltip("Optional slot — a design can have no jamming/ECM equipment equipped.")]
        public JammingDefinition[] missileJammingOptions;
        [Tooltip("Optional slot (Phase 2C) — a design can fly without a datalink network (own-seeker-only " +
            "for its whole flight) or with one that supports mid-course updates (see DatalinkMidCourseGuidance).")]
        public DatalinkNetworkDefinition[] missileDatalinkOptions;

        [Header("Drone: single-option slots (only one variant seeded so far)")]
        public SensorSuiteDefinition droneSensorBasic;
        public SensorSuiteDefinition droneSensorScout;

        [Header("Drone: multi-option picker slots (2B part breadth)")]
        [Tooltip("Every candidate propulsion type, unlocked or not — the picker filters to unlocked options at runtime.")]
        public PropulsionDefinition[] dronePropulsionOptions;
        public DroneAirframeDefinition[] droneAirframeOptions;
        public WingOrPropellerDefinition[] droneWingOptions;
        public HullMaterialDefinition[] droneHullOptions;
        public DroneEngineDefinition[] droneEngineOptions;
        public FuelDefinition[] droneFuelOptions;
        public WeaponBayDefinition[] droneWeaponBayOptions;
        [Tooltip("Optional slot (Phase 2C) — a design can carry no decoy/flare-chaff countermeasure, or " +
            "one that gives it a chance to break an inbound missile's lock (see CountermeasureController).")]
        public CountermeasureDefinition[] droneCountermeasureOptions;

        [Header("Continuous Sliders")]
        [Tooltip("Missile fuel tank fill level (0-1). Trades range/burn time against mass and MTOW headroom.")]
        [Range(0f, 1f)]
        public float missileFuelFill = 1f;

        // Current picker selections for the multi-option missile slots above. Not
        // serialized/persisted across sessions — resolved to a sensible default
        // (first unlocked option) each refresh by ResolveSelection.
        private MissileEngineDefinition _selectedEngine;
        private SeekerDefinition _selectedSeeker;
        private MissilePayloadDefinition _selectedPayload;
        private CountermeasureDefinition _selectedCountermeasure; // optional, may stay null
        private JammingDefinition _selectedJamming; // optional, may stay null
        private DatalinkNetworkDefinition _selectedDatalink; // optional, may stay null

        // Current picker selections for the multi-option drone slots above (2B).
        private PropulsionDefinition _selectedDronePropulsion;
        private DroneAirframeDefinition _selectedDroneAirframe;
        private WingOrPropellerDefinition _selectedDroneWing;
        private HullMaterialDefinition _selectedDroneHull;
        private DroneEngineDefinition _selectedDroneEngine;
        private FuelDefinition _selectedDroneFuel;
        private WeaponBayDefinition _selectedDroneWeaponBay;
        private CountermeasureDefinition _selectedDroneCountermeasure; // optional, may stay null

        private UIDocument _document;
        private Label _currencyLabel;
        private ScrollView _techTreeScroll;
        private ScrollView _partPickerScroll;
        private VisualElement _designPreviewContent;
        private Slider _missileFuelSlider;
        private Label _missileFuelLabel;
        private Button _enterCombatButton;
        private Button _debugAddCurrencyButton;

        /// <summary>How much currency the debug "+10,000 (Debug)" button grants per click.
        /// Editor/development-build only — see OnEnable.</summary>
        private const int DebugCurrencyGrant = 10000;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            VisualElement root = _document.rootVisualElement;
            _currencyLabel = root.Q<Label>("currency-label");
            _techTreeScroll = root.Q<ScrollView>("tech-tree-scroll");
            _partPickerScroll = root.Q<ScrollView>("part-picker-scroll");
            _designPreviewContent = root.Q<VisualElement>("design-preview-content");
            _missileFuelSlider = root.Q<Slider>("missile-fuel-slider");
            _missileFuelLabel = root.Q<Label>("missile-fuel-label");
            _enterCombatButton = root.Q<Button>("enter-combat-button");
            _debugAddCurrencyButton = root.Q<Button>("debug-add-currency-button");

            _enterCombatButton.clicked += OnEnterCombatClicked;

            // Debug-only currency cheat for testing the tech tree/part picker without
            // grinding combat victories — never shown in a non-development player build.
            // Application.isEditor covers Editor Play mode; Debug.isDebugBuild covers
            // Development Builds so QA/testers get it too without shipping it in a
            // release build.
            bool showDebugTools = Application.isEditor || Debug.isDebugBuild;
            _debugAddCurrencyButton.style.display = showDebugTools ? DisplayStyle.Flex : DisplayStyle.None;
            if (showDebugTools)
                _debugAddCurrencyButton.clicked += OnDebugAddCurrencyClicked;

            _missileFuelSlider.lowValue = 0f;
            _missileFuelSlider.highValue = 1f;
            _missileFuelSlider.value = missileFuelFill;
            _missileFuelSlider.RegisterValueChangedCallback(OnMissileFuelFillChanged);
        }

        private void OnDisable()
        {
            if (_enterCombatButton != null)
                _enterCombatButton.clicked -= OnEnterCombatClicked;
            if (_debugAddCurrencyButton != null)
                _debugAddCurrencyButton.clicked -= OnDebugAddCurrencyClicked;
            if (_missileFuelSlider != null)
                _missileFuelSlider.UnregisterValueChangedCallback(OnMissileFuelFillChanged);
        }

        private void OnDebugAddCurrencyClicked()
        {
            PlayerProgress.Instance?.AddCurrency(DebugCurrencyGrant);
            RefreshAll();
        }

        private void OnMissileFuelFillChanged(ChangeEvent<float> evt)
        {
            missileFuelFill = evt.newValue;
            RefreshDesignPreview(PlayerProgress.Instance);
        }

        private void Start()
        {
            if (PlayerProgress.Instance != null)
                PlayerProgress.Instance.Load();

            RefreshAll();
        }

        private void RefreshAll()
        {
            PlayerProgress progress = PlayerProgress.Instance;

            _currencyLabel.text = $"Currency: {(progress != null ? progress.Currency : 0)}";

            RefreshTechTree(progress);
            RefreshPartPicker(progress);
            RefreshDesignPreview(progress);
        }

        private void RefreshTechTree(PlayerProgress progress)
        {
            _techTreeScroll.contentContainer.Clear();

            if (techTree == null)
                return;

            foreach (TechNode node in techTree)
                _techTreeScroll.contentContainer.Add(BuildTechRow(node, progress));
        }

        private VisualElement BuildTechRow(TechNode node, PlayerProgress progress)
        {
            bool unlocked = progress != null && progress.IsUnlocked(node);
            bool prereqsMet = ArePrerequisitesMet(progress, node);
            bool affordable = progress != null && progress.CanAfford(node.researchCost);

            var row = new VisualElement();
            row.AddToClassList("tech-row");

            var info = new VisualElement();
            info.AddToClassList("tech-row-info");

            var nameLabel = new Label(node.displayName);
            nameLabel.AddToClassList("tech-name-label");

            var statusLabel = new Label(unlocked ? "Unlocked" : $"Cost: {node.researchCost}");
            statusLabel.AddToClassList("tech-status-label");

            info.Add(nameLabel);
            info.Add(statusLabel);

            var unlockButton = new Button(() => OnUnlockClicked(node)) { text = unlocked ? "Done" : "Unlock" };
            unlockButton.AddToClassList("unlock-button");
            unlockButton.SetEnabled(!unlocked && prereqsMet && affordable && progress != null);

            row.Add(info);
            row.Add(unlockButton);
            return row;
        }

        private void OnUnlockClicked(TechNode node)
        {
            PlayerProgress.Instance?.TryUnlock(node);
            RefreshAll();
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

        /// <summary>
        /// Builds the missile part picker: one row per multi-option slot, each row a
        /// horizontal list of buttons for every currently-unlocked variant (filtered
        /// from the full option arrays via PlayerProgress.IsPartUnlocked, same source
        /// of truth the tech tree uses). Clicking an option button selects it and
        /// immediately refreshes the design preview stats.
        /// </summary>
        private void RefreshPartPicker(PlayerProgress progress)
        {
            if (_partPickerScroll == null)
                return;

            // Resolve selections before building rows so the "selected" highlight and
            // TryBuildMissileLoadout (called from RefreshDesignPreview right after this)
            // agree on the same choice.
            _selectedPayload = ResolveSelection(progress, missilePayloadOptions, _selectedPayload);
            _selectedEngine = ResolveSelection(progress, missileEngineOptions, _selectedEngine);
            _selectedSeeker = ResolveSelection(progress, missileSeekerOptions, _selectedSeeker);
            _selectedCountermeasure = ResolveOptionalSelection(progress, missileCountermeasureOptions, _selectedCountermeasure);
            _selectedJamming = ResolveOptionalSelection(progress, missileJammingOptions, _selectedJamming);

            _selectedDronePropulsion = ResolveSelection(progress, dronePropulsionOptions, _selectedDronePropulsion);
            _selectedDroneAirframe = ResolveSelection(progress, droneAirframeOptions, _selectedDroneAirframe);
            _selectedDroneWing = ResolveSelection(progress, droneWingOptions, _selectedDroneWing);
            _selectedDroneHull = ResolveSelection(progress, droneHullOptions, _selectedDroneHull);
            _selectedDroneEngine = ResolveSelection(progress, droneEngineOptions, _selectedDroneEngine);
            _selectedDroneFuel = ResolveSelection(progress, droneFuelOptions, _selectedDroneFuel);
            _selectedDroneWeaponBay = ResolveSelection(progress, droneWeaponBayOptions, _selectedDroneWeaponBay);
            _selectedDatalink = ResolveOptionalSelection(progress, missileDatalinkOptions, _selectedDatalink);
            _selectedDroneCountermeasure = ResolveOptionalSelection(progress, droneCountermeasureOptions, _selectedDroneCountermeasure);

            _partPickerScroll.contentContainer.Clear();

            _partPickerScroll.contentContainer.Add(BuildPickerSectionHeader("Missile Loadout"));
            _partPickerScroll.contentContainer.Add(BuildPartSlotRow("Payload", missilePayloadOptions, progress,
                _selectedPayload, allowNone: false, onSelect: selected => { _selectedPayload = selected; RefreshDesignPreview(progress); }));
            _partPickerScroll.contentContainer.Add(BuildPartSlotRow("Engine", missileEngineOptions, progress,
                _selectedEngine, allowNone: false, onSelect: selected => { _selectedEngine = selected; RefreshDesignPreview(progress); }));
            _partPickerScroll.contentContainer.Add(BuildPartSlotRow("Seeker", missileSeekerOptions, progress,
                _selectedSeeker, allowNone: false, onSelect: selected => { _selectedSeeker = selected; RefreshDesignPreview(progress); }));
            _partPickerScroll.contentContainer.Add(BuildPartSlotRow("Countermeasure", missileCountermeasureOptions, progress,
                _selectedCountermeasure, allowNone: true, onSelect: selected => { _selectedCountermeasure = selected; RefreshDesignPreview(progress); }));
            _partPickerScroll.contentContainer.Add(BuildPartSlotRow("Jamming / ECM", missileJammingOptions, progress,
                _selectedJamming, allowNone: true, onSelect: selected => { _selectedJamming = selected; RefreshDesignPreview(progress); }));
            _partPickerScroll.contentContainer.Add(BuildPartSlotRow("Datalink", missileDatalinkOptions, progress,
                _selectedDatalink, allowNone: true, onSelect: selected => { _selectedDatalink = selected; RefreshDesignPreview(progress); }));

            _partPickerScroll.contentContainer.Add(BuildPickerSectionHeader("Drone Loadout"));
            _partPickerScroll.contentContainer.Add(BuildPartSlotRow("Propulsion", dronePropulsionOptions, progress,
                _selectedDronePropulsion, allowNone: false, onSelect: selected => { _selectedDronePropulsion = selected; RefreshDesignPreview(progress); }));
            _partPickerScroll.contentContainer.Add(BuildPartSlotRow("Airframe", droneAirframeOptions, progress,
                _selectedDroneAirframe, allowNone: false, onSelect: selected => { _selectedDroneAirframe = selected; RefreshDesignPreview(progress); }));
            _partPickerScroll.contentContainer.Add(BuildPartSlotRow("Wing / Rotor", droneWingOptions, progress,
                _selectedDroneWing, allowNone: false, onSelect: selected => { _selectedDroneWing = selected; RefreshDesignPreview(progress); }));
            _partPickerScroll.contentContainer.Add(BuildPartSlotRow("Hull Material", droneHullOptions, progress,
                _selectedDroneHull, allowNone: false, onSelect: selected => { _selectedDroneHull = selected; RefreshDesignPreview(progress); }));
            _partPickerScroll.contentContainer.Add(BuildPartSlotRow("Engine", droneEngineOptions, progress,
                _selectedDroneEngine, allowNone: false, onSelect: selected => { _selectedDroneEngine = selected; RefreshDesignPreview(progress); }));
            _partPickerScroll.contentContainer.Add(BuildPartSlotRow("Fuel", droneFuelOptions, progress,
                _selectedDroneFuel, allowNone: false, onSelect: selected => { _selectedDroneFuel = selected; RefreshDesignPreview(progress); }));
            _partPickerScroll.contentContainer.Add(BuildPartSlotRow("Weapon Bay", droneWeaponBayOptions, progress,
                _selectedDroneWeaponBay, allowNone: false, onSelect: selected => { _selectedDroneWeaponBay = selected; RefreshDesignPreview(progress); }));
            _partPickerScroll.contentContainer.Add(BuildPartSlotRow("Countermeasure", droneCountermeasureOptions, progress,
                _selectedDroneCountermeasure, allowNone: true, onSelect: selected => { _selectedDroneCountermeasure = selected; RefreshDesignPreview(progress); }));
        }

        private static VisualElement BuildPickerSectionHeader(string text)
        {
            var header = new Label(text);
            header.AddToClassList("part-picker-section-header");
            return header;
        }

        private VisualElement BuildPartSlotRow<T>(string slotLabel, T[] options, PlayerProgress progress, T selected,
            bool allowNone, System.Action<T> onSelect) where T : PartDefinition
        {
            var row = new VisualElement();
            row.AddToClassList("part-slot-row");

            var slotNameLabel = new Label(slotLabel);
            slotNameLabel.AddToClassList("part-slot-label");
            row.Add(slotNameLabel);

            var optionsRow = new VisualElement();
            optionsRow.AddToClassList("part-option-row");

            var unlockedOptions = new List<T>();
            if (options != null && progress != null)
            {
                foreach (T option in options)
                {
                    if (option != null && progress.IsPartUnlocked(option, techTree))
                        unlockedOptions.Add(option);
                }
            }

            if (allowNone)
            {
                var noneButton = new Button(() => onSelect(null)) { text = "None" };
                noneButton.AddToClassList("part-option-button");
                if (selected == null)
                    noneButton.AddToClassList("part-option-button-selected");
                optionsRow.Add(noneButton);
            }

            foreach (T option in unlockedOptions)
            {
                var optionButton = new Button(() => onSelect(option)) { text = option.displayName };
                optionButton.AddToClassList("part-option-button");
                if (selected == option)
                    optionButton.AddToClassList("part-option-button-selected");
                optionsRow.Add(optionButton);
            }

            if (unlockedOptions.Count == 0 && !allowNone)
            {
                var emptyLabel = new Label("Unlock more tech");
                emptyLabel.AddToClassList("part-slot-empty-label");
                optionsRow.Add(emptyLabel);
            }

            row.Add(optionsRow);
            return row;
        }

        /// <summary>
        /// Required-slot selection resolution: keep the current selection if it's still
        /// unlocked, otherwise fall back to the first unlocked option (so a player who
        /// only has the Tier-0 variant unlocked still gets a working default, and a
        /// stale/locked selection from a prior save never silently persists).
        /// </summary>
        private T ResolveSelection<T>(PlayerProgress progress, T[] options, T current) where T : PartDefinition
        {
            if (current != null && progress != null && progress.IsPartUnlocked(current, techTree))
                return current;

            if (options == null || progress == null)
                return null;

            foreach (T option in options)
            {
                if (option != null && progress.IsPartUnlocked(option, techTree))
                    return option;
            }
            return null;
        }

        /// <summary>
        /// Optional-slot selection resolution: never auto-picks an option (an empty
        /// countermeasure/jamming slot is a valid deliberate choice) — only clears the
        /// selection if it becomes locked.
        /// </summary>
        private T ResolveOptionalSelection<T>(PlayerProgress progress, T[] options, T current) where T : PartDefinition
        {
            if (current == null)
                return null;
            if (progress == null || !progress.IsPartUnlocked(current, techTree))
                return null;
            return current;
        }

        private void RefreshDesignPreview(PlayerProgress progress)
        {
            _designPreviewContent.Clear();

            _missileFuelLabel.text = $"Fuel fill: {missileFuelFill * 100f:F0}%";

            bool missileWithinMtow = true;

            if (TryBuildMissileLoadout(progress, out MissileLoadout missileLoadout))
            {
                var stats = DesignStatsCalculator.Calculate(missileLoadout);
                missileWithinMtow = stats.isWithinMtow;

                AddDesignLine(missileWithinMtow ? "Missile: READY" : "Missile: OVER MTOW LIMIT", ready: missileWithinMtow);
                if (stats.maxTakeOffMassKg > 0f)
                    AddDesignLine($"  Mass: {stats.massKg:F0} / {stats.maxTakeOffMassKg:F0} kg MTOW  Thrust: {stats.thrustNewtons:F0} N",
                        ready: missileWithinMtow ? (bool?)null : false);
                else
                    AddDesignLine($"  Mass: {stats.massKg:F0} kg  Thrust: {stats.thrustNewtons:F0} N");
                AddDesignLine($"  Fuel: {stats.fuelMassKg:F1} kg ({missileFuelFill * 100f:F0}% fill)");
                AddDesignLine($"  Damage: {stats.directDamage:F0} (+{stats.splashDamage:F0} splash, {stats.blastRadiusMeters:F0}m)");
                AddDesignLine($"  Seeker range: {stats.seekerRangeMeters:F0} m");
            }
            else
            {
                AddDesignLine("Missile: incomplete — unlock more tech", ready: false);
            }

            bool strikeDroneWithinMtow = true;

            if (TryBuildDroneLoadout(progress, missileLoadout, includeWeapon: true, out DroneLoadout droneLoadout))
            {
                var stats = DesignStatsCalculator.Calculate(droneLoadout);
                strikeDroneWithinMtow = stats.isWithinMtow;

                AddDesignLine(strikeDroneWithinMtow ? "Strike Drone: READY" : "Strike Drone: OVER MTOW LIMIT",
                    ready: strikeDroneWithinMtow, header: true);
                if (stats.maxTakeOffMassKg > 0f)
                    AddDesignLine($"  Mass: {stats.massKg:F0} / {stats.maxTakeOffMassKg:F0} kg MTOW  Health: {stats.maxHealth:F0}",
                        ready: strikeDroneWithinMtow ? (bool?)null : false);
                else
                    AddDesignLine($"  Mass: {stats.massKg:F0} kg  Health: {stats.maxHealth:F0}");
                AddDesignLine($"  Sensor range: {stats.sensorRangeMeters:F0} m");
                // Airframe (visual shape) and Propulsion (actual flight physics) are two
                // separate picker slots — a Fixed-Wing/Flying-Wing airframe does NOT
                // automatically switch propulsion away from electric. Surface both here
                // explicitly so "picked a jet-looking airframe but it still flies like a
                // quadcopter" is visible in the picker instead of only discoverable by
                // flying it (which is exactly what happened testing this feature).
                string flightModel = stats.requiresForwardFlight ? "Fixed-wing/jet" : "Multirotor";
                AddDesignLine($"  Propulsion: {droneLoadout.propulsion.displayName} ({flightModel} flight model)");
            }
            else
            {
                AddDesignLine("Strike Drone: incomplete — unlock more tech", ready: false, header: true);
            }

            if (TryBuildDroneLoadout(progress, null, includeWeapon: false, out DroneLoadout scoutLoadout, useScoutSensor: true))
            {
                var stats = DesignStatsCalculator.Calculate(scoutLoadout);
                AddDesignLine("Scout Drone: READY", ready: true, header: true);
                AddDesignLine($"  Sensor range: {stats.sensorRangeMeters:F0} m (shares contacts)");
            }
            else
            {
                AddDesignLine("Scout Drone: incomplete — unlock more tech", ready: false, header: true);
            }

            bool combatReady = missileLoadout.IsComplete && missileWithinMtow && strikeDroneWithinMtow &&
                                TryBuildDroneLoadout(progress, null, true, out _) &&
                                TryBuildDroneLoadout(progress, null, false, out _, useScoutSensor: true);

            _enterCombatButton.SetEnabled(combatReady);
            _enterCombatButton.text = combatReady
                ? "Enter Combat"
                : (!missileWithinMtow ? "Missile over MTOW — reduce fuel/parts"
                    : (!strikeDroneWithinMtow ? "Strike drone over MTOW — reduce fuel/parts" : "Unlock more tech to proceed"));
        }

        private void AddDesignLine(string text, bool? ready = null, bool header = false)
        {
            var label = new Label(text);
            label.AddToClassList("design-line");
            if (header)
                label.AddToClassList("design-header-line");
            if (ready.HasValue)
                label.AddToClassList(ready.Value ? "design-line-ready" : "design-line-incomplete");
            _designPreviewContent.Add(label);
        }

        private void OnEnterCombatClicked()
        {
            if (!_enterCombatButton.enabledSelf)
                return;

            // Stash the actual configured designs on PlayerProgress (a DontDestroyOnLoad
            // singleton that survives the scene load) so the combat scene's
            // CombatPlayerLoadoutApplier can spawn the player's real chosen loadout
            // instead of Combat_Arena01's editor-time-baked Tier-0 default. Without
            // this, every part-picker selection in this UI would be purely cosmetic —
            // it would compute preview stats here and then have zero effect on the
            // actual battle.
            PlayerProgress progress = PlayerProgress.Instance;
            if (progress != null)
            {
                TryBuildMissileLoadout(progress, out MissileLoadout missileLoadout);
                if (TryBuildDroneLoadout(progress, missileLoadout, includeWeapon: true, out DroneLoadout strikeLoadout))
                    progress.PendingStrikeDroneLoadout = strikeLoadout;
                if (TryBuildDroneLoadout(progress, null, includeWeapon: false, out DroneLoadout scoutLoadout, useScoutSensor: true))
                    progress.PendingScoutDroneLoadout = scoutLoadout;
            }

            // Phase 2E: load whichever scenario was picked via ScenarioPickerOverlay,
            // if any, falling back to the single hardcoded default scene so
            // Combat_Arena01 stays reachable even when no picker was ever shown (e.g.
            // headless batch regression tests that never touch the Workshop scene at all).
            string sceneToLoad = progress != null && progress.PendingScenario != null
                ? progress.PendingScenario.sceneName
                : combatSceneName;
            SceneManager.LoadScene(sceneToLoad);
        }

        private bool TryBuildMissileLoadout(PlayerProgress progress, out MissileLoadout loadout)
        {
            loadout = new MissileLoadout { designName = "Basic Missile" };
            if (progress == null)
                return false;

            loadout.airframe = IsUnlocked(progress, missileAirframe) ? missileAirframe : null;
            loadout.engine = IsUnlocked(progress, _selectedEngine) ? _selectedEngine : null;
            loadout.seeker = IsUnlocked(progress, _selectedSeeker) ? _selectedSeeker : null;
            loadout.payload = IsUnlocked(progress, _selectedPayload) ? _selectedPayload : null;
            loadout.fuel = IsUnlocked(progress, missileFuel) ? missileFuel : null;
            loadout.countermeasure = IsUnlocked(progress, _selectedCountermeasure) ? _selectedCountermeasure : null;
            loadout.jamming = IsUnlocked(progress, _selectedJamming) ? _selectedJamming : null;
            loadout.datalink = IsUnlocked(progress, _selectedDatalink) ? _selectedDatalink : null;
            loadout.fuelFillFraction = missileFuelFill;
            return loadout.IsComplete;
        }

        private bool TryBuildDroneLoadout(PlayerProgress progress, MissileLoadout missileLoadout, bool includeWeapon,
            out DroneLoadout loadout, bool useScoutSensor = false)
        {
            loadout = new DroneLoadout { designName = useScoutSensor ? "Basic Scout Drone" : "Basic Strike Drone" };
            if (progress == null)
                return false;

            loadout.propulsion = IsUnlocked(progress, _selectedDronePropulsion) ? _selectedDronePropulsion : null;
            loadout.airframe = IsUnlocked(progress, _selectedDroneAirframe) ? _selectedDroneAirframe : null;
            loadout.wingOrPropeller = IsUnlocked(progress, _selectedDroneWing) ? _selectedDroneWing : null;
            loadout.hullMaterial = IsUnlocked(progress, _selectedDroneHull) ? _selectedDroneHull : null;
            loadout.engine = IsUnlocked(progress, _selectedDroneEngine) ? _selectedDroneEngine : null;
            loadout.fuel = IsUnlocked(progress, _selectedDroneFuel) ? _selectedDroneFuel : null;
            loadout.weaponBay = IsUnlocked(progress, _selectedDroneWeaponBay) ? _selectedDroneWeaponBay : null;
            loadout.countermeasure = IsUnlocked(progress, _selectedDroneCountermeasure) ? _selectedDroneCountermeasure : null;

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
