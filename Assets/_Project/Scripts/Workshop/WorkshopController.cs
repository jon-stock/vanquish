using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Vanquish.Core;
using Vanquish.Data;
using Vanquish.Data.Drones;
using Vanquish.Data.Missiles;
using Vanquish.Data.Scenarios;
using Vanquish.Data.Shared;
using Vanquish.Data.Support;
using Vanquish.Data.TechTree;

namespace Vanquish.Workshop
{
    /// <summary>
    /// Workshop: shows the linear tech tree with unlock buttons, a real multi-option
    /// part picker for every missile (2A) and drone (2B) slot that has more than one
    /// unlocked variant, the resulting missile/drone design's computed stats once
    /// enough parts are unlocked, an in-UI scenario picker, and buttons to enter the
    /// Test Range or Combat. Built with UI Toolkit (UIDocument + Workshop.uxml/.uss
    /// under Assets/_Project/UI/Workshop/) rather than OnGUI — this now includes what
    /// used to be the separate OnGUI ScenarioPickerOverlay/TestRangeEntryOverlay
    /// (Phase 2E/2G scaffolding, explicitly superseded rather than stacked on top of,
    /// per Phase 3A). Phase1WorkshopSceneBuilder wires the UIDocument's
    /// visualTreeAsset/panelSettings and all the part option arrays when it builds the
    /// scene. Sensor suites (basic/scout) stay single-option fields rather than picker
    /// slots since they're fixed by drone role (strike vs. scout), not a player choice.
    /// All scene transitions go through GameFlowController (Phase 3A) rather than
    /// calling SceneManager.LoadScene directly.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class WorkshopController : MonoBehaviour
    {
        public TechNode[] techTree;
        public string combatSceneName = SceneNames.DefaultCombatArena;

        [Tooltip("Phase 2G: the Test Range scene — no win/lose, no currency, purely observational. See EnterTestRange.")]
        public string testRangeSceneName = SceneNames.TestRange;

        [Tooltip("Phase 3A: every scenario the player can choose to test against, replacing the old " +
            "ScenarioPickerOverlay OnGUI panel. Wired by Phase1WorkshopSceneBuilder.")]
        public ScenarioDefinition[] scenarioOptions = System.Array.Empty<ScenarioDefinition>();

        [Tooltip("Phase 3B: the live 3D design preview stage — see WorkshopPreviewStage and " +
            "Phase1WorkshopSceneBuilder.BuildPreviewStage. Wired by the scene builder.")]
        public WorkshopPreviewStage previewStage;

        [Tooltip("Phase 3B: the RenderTexture the preview camera renders into and the " +
            "design-preview-viewport Image element displays. Wired by the scene builder.")]
        public RenderTexture previewRenderTexture;

        [Header("Missile: single-option slots (only one variant seeded so far)")]
        public FuelDefinition missileFuel;

        [Header("Missile: multi-option picker slots (2A part breadth)")]
        [Tooltip("Depth pass: was a single fixed missileAirframe field — the only missile airframe that ever " +
            "existed (40kg MTOW) couldn't fit several of the heavier Tier2-4 engine/seeker/payload combos " +
            "the tech tree already unlocked (e.g. Scramjet + Cluster + Multi-Spectral seeker alone is ~46kg). " +
            "Now a real picker like every other multi-tier slot, so heavier loadouts have an airframe that can " +
            "actually carry them.")]
        public MissileAirframeDefinition[] missileAirframeOptions;
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
        [Tooltip("Depth pass: was the strike drone's ONLY sensor, hardcoded regardless of what was unlocked " +
            "(see droneSensorOptions below for the real, now-selectable, sensor picker). The scout drone still " +
            "always uses this Scout sensor by design — a scout's whole purpose is the shared long-range sensor.")]
        public SensorSuiteDefinition droneSensorBasic;
        public SensorSuiteDefinition droneSensorScout;

        [Header("Drone: multi-option picker slots (2B part breadth)")]
        [Tooltip("Every candidate propulsion type, unlocked or not — the picker filters to unlocked options at runtime.")]
        public PropulsionDefinition[] dronePropulsionOptions;
        [Tooltip("Depth pass: Propulsion+Engine merge — curated pairs offered as one 'Propulsion' dropdown " +
            "instead of two independent dropdowns that always had to be picked in lockstep (a Propulsion " +
            "part and its DroneEngineDefinition substantially duplicate each other — see " +
            "DronePropulsionPackageDefinition's own doc comment). dronePropulsionOptions/droneEngineOptions " +
            "below are the real underlying part assets (DesignStatsCalculator/VehicleFactory/DroneCompatibility " +
            "still only know about those two, never this type) but are no longer independently pickable in the " +
            "Workshop — every package here points at one entry from each.")]
        public DronePropulsionPackageDefinition[] dronePropulsionPackageOptions;
        public DroneAirframeDefinition[] droneAirframeOptions;
        public WingOrPropellerDefinition[] droneWingOptions;
        [Tooltip("Planform-preset pass: curated Airframe+Wing pairs offered as one merged 'Planform' dropdown " +
            "when the Airframe Type toggle is set to Fixed-Wing (see BuildAirframeTypeToggleRow). Multirotor " +
            "mode ignores this and uses droneAirframeOptions/droneWingOptions directly, since a rotor is a " +
            "genuinely separable accessory choice the way a fixed-wing planform isn't.")]
        public DronePlanformDefinition[] dronePlanformOptions;
        public HullMaterialDefinition[] droneHullOptions;
        public DroneEngineDefinition[] droneEngineOptions;
        public FuelDefinition[] droneFuelOptions;
        public WeaponBayDefinition[] droneWeaponBayOptions;
        [Tooltip("Optional slot (Phase 2C) — a design can carry no decoy/flare-chaff countermeasure, or " +
            "one that gives it a chance to break an inbound missile's lock (see CountermeasureController).")]
        public CountermeasureDefinition[] droneCountermeasureOptions;
        [Tooltip("Depth pass: the strike drone's sensor is now a real picker instead of always being " +
            "droneSensorBasic regardless of what's unlocked — see droneSensorBasic's own tooltip.")]
        public SensorSuiteDefinition[] droneSensorOptions;

        [Header("Continuous Sliders")]
        [Tooltip("Missile fuel tank fill level (0-1). Trades range/burn time against mass and MTOW headroom.")]
        [Range(0f, 1f)]
        public float missileFuelFill = 1f;

        [Tooltip("Depth pass: how many missiles the strike drone carries — was hardcoded to 4 regardless of " +
            "weapon bay capacity (see WeaponBayDefinition.maxMunitionCount's tooltip). Clamped to the " +
            "selected bay's real capacity by DesignStatsCalculator.effectiveAmmoCount either way, so dialing " +
            "this above what the bay can hold just wastes mass on rounds that never actually load.")]
        public int droneAmmoCount = 4;

        // Current picker selections for the multi-option missile slots above. Not
        // serialized/persisted across sessions — resolved to a sensible default
        // (first unlocked option) each refresh by ResolveSelection.
        private MissileAirframeDefinition _selectedMissileAirframe;
        private MissileEngineDefinition _selectedEngine;
        private SeekerDefinition _selectedSeeker;
        private MissilePayloadDefinition _selectedPayload;
        private CountermeasureDefinition _selectedCountermeasure; // optional, may stay null
        private JammingDefinition _selectedJamming; // optional, may stay null
        private DatalinkNetworkDefinition _selectedDatalink; // optional, may stay null

        // Current picker selections for the multi-option drone slots above (2B).
        // Depth pass: _selectedDronePropulsion/_selectedDroneEngine are now derived
        // from _selectedDronePropulsionPackage every refresh (see RefreshPartPicker)
        // rather than resolved independently — kept as their own fields since every
        // downstream reader (TryBuildDroneLoadout, DesignStatsCalculator,
        // VehicleFactory) still only knows about these two, not the package type.
        private DronePropulsionPackageDefinition _selectedDronePropulsionPackage;
        private PropulsionDefinition _selectedDronePropulsion;
        private DroneAirframeDefinition _selectedDroneAirframe;
        private WingOrPropellerDefinition _selectedDroneWing;
        // Planform-preset pass: the merged Airframe+Wing selection shown for
        // Fixed-Wing mode. _selectedDroneAirframe/_selectedDroneWing above are kept
        // in sync from this whenever it changes (see the Fixed-Wing branch in
        // RefreshPartPicker) so TryBuildDroneLoadout/DesignStatsCalculator/
        // VehicleFactory need zero changes — they only ever see the same two fields
        // they always have.
        private DronePlanformDefinition _selectedDronePlanform;
        private HullMaterialDefinition _selectedDroneHull;
        private DroneEngineDefinition _selectedDroneEngine;
        private FuelDefinition _selectedDroneFuel;
        private WeaponBayDefinition _selectedDroneWeaponBay;
        private CountermeasureDefinition _selectedDroneCountermeasure; // optional, may stay null
        private SensorSuiteDefinition _selectedDroneSensor;

        // Phase 3A: which scenario the player has selected to test against. Not
        // optional/nullable-by-design like the countermeasure/jamming slots above —
        // defaults to the first entry in scenarioOptions (see RefreshScenarioPicker)
        // so PlayerProgress.PendingScenario is meaningful even if the player never
        // opens this picker, mirroring the old ScenarioPickerOverlay.Start() default.
        private ScenarioDefinition _selectedScenario;

        /// <summary>
        /// Phase 3B follow-up (pulled forward from Phase 3C's designer-mode-split,
        /// per direct user feedback that missile and craft editing need visually
        /// separate sections rather than one long stacked list): which half of the
        /// design the part-picker column currently shows. The live 3D preview always
        /// shows the assembled strike drone regardless of mode — editing a missile
        /// part while in Missile mode still updates the mounted missiles visible on
        /// that same previewed craft.
        /// </summary>
        private enum DesignerMode { Craft, Missile, Research }
        private DesignerMode _designerMode = DesignerMode.Craft;

        /// <summary>
        /// Fixed-wing flight-model rework: which airframe type the Craft tab's
        /// Airframe/Wing-or-Rotor/Propulsion/Engine dropdowns are currently filtered
        /// to. This is deliberately a Workshop-only UI toggle, not a new field on
        /// DroneLoadout or a third designer mode — the tech tree and physics pipeline
        /// stay exactly as they were (one DroneLoadout, one DesignStatsCalculator, one
        /// VehicleFactory); this only changes which already-existing, already-unlocked
        /// parts the picker shows for those four slots, via DroneCompatibility.
        /// Defaults to Multirotor so an existing save/session with a multirotor design
        /// selected doesn't visually change anything the first time this ships.
        /// </summary>
        private FlightConfiguration _selectedFlightConfiguration = FlightConfiguration.Multirotor;

        private UIDocument _document;
        private Label _currencyLabel;
        private ScrollView _partPickerScroll;
        private Button _modeTabCraftButton;
        private Button _modeTabMissileButton;
        private Button _modeTabResearchButton;
        private VisualElement _designStatCard;
        private Image _designPreviewViewport;
        private VisualElement _scenarioPickerContent;
        private Button _enterCombatButton;
        private Button _enterTestRangeButton;
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
            _partPickerScroll = root.Q<ScrollView>("part-picker-scroll");
            _modeTabCraftButton = root.Q<Button>("mode-tab-craft");
            _modeTabMissileButton = root.Q<Button>("mode-tab-missile");
            _modeTabResearchButton = root.Q<Button>("mode-tab-research");
            _designStatCard = root.Q<VisualElement>("design-stat-card");
            _designPreviewViewport = root.Q<Image>("design-preview-viewport");
            _scenarioPickerContent = root.Q<VisualElement>("scenario-picker-content");
            _enterCombatButton = root.Q<Button>("enter-combat-button");
            _enterTestRangeButton = root.Q<Button>("enter-test-range-button");
            _debugAddCurrencyButton = root.Q<Button>("debug-add-currency-button");

            _enterCombatButton.clicked += OnEnterCombatClicked;
            _enterTestRangeButton.clicked += EnterTestRange;
            _modeTabCraftButton.clicked += OnCraftModeTabClicked;
            _modeTabMissileButton.clicked += OnMissileModeTabClicked;
            _modeTabResearchButton.clicked += OnResearchModeTabClicked;
            UpdateDesignerModeTabStyles();

            // Debug-only currency cheat for testing the tech tree/part picker without
            // grinding combat victories — never shown in a non-development player build.
            // Application.isEditor covers Editor Play mode; Debug.isDebugBuild covers
            // Development Builds so QA/testers get it too without shipping it in a
            // release build.
            bool showDebugTools = Application.isEditor || Debug.isDebugBuild;
            _debugAddCurrencyButton.style.display = showDebugTools ? DisplayStyle.Flex : DisplayStyle.None;
            if (showDebugTools)
                _debugAddCurrencyButton.clicked += OnDebugAddCurrencyClicked;

            // Phase 3B: live 3D design preview — the viewport Image just displays
            // whatever the preview camera renders (wired by Phase1WorkshopSceneBuilder);
            // drag-to-rotate/scroll-to-zoom are forwarded to WorkshopPreviewStage here
            // since UI Toolkit owns pointer capture, not the preview GameObject itself.
            if (_designPreviewViewport != null)
            {
                _designPreviewViewport.image = previewRenderTexture;
                _designPreviewViewport.RegisterCallback<PointerDownEvent>(OnPreviewPointerDown);
                _designPreviewViewport.RegisterCallback<PointerMoveEvent>(OnPreviewPointerMove);
                _designPreviewViewport.RegisterCallback<PointerUpEvent>(OnPreviewPointerUp);
                _designPreviewViewport.RegisterCallback<WheelEvent>(OnPreviewWheel);
            }
        }

        private void OnDisable()
        {
            if (_enterCombatButton != null)
                _enterCombatButton.clicked -= OnEnterCombatClicked;
            if (_enterTestRangeButton != null)
                _enterTestRangeButton.clicked -= EnterTestRange;
            if (_debugAddCurrencyButton != null)
                _debugAddCurrencyButton.clicked -= OnDebugAddCurrencyClicked;
            if (_modeTabCraftButton != null)
                _modeTabCraftButton.clicked -= OnCraftModeTabClicked;
            if (_modeTabMissileButton != null)
                _modeTabMissileButton.clicked -= OnMissileModeTabClicked;
            if (_modeTabResearchButton != null)
                _modeTabResearchButton.clicked -= OnResearchModeTabClicked;

            if (_designPreviewViewport != null)
            {
                _designPreviewViewport.UnregisterCallback<PointerDownEvent>(OnPreviewPointerDown);
                _designPreviewViewport.UnregisterCallback<PointerMoveEvent>(OnPreviewPointerMove);
                _designPreviewViewport.UnregisterCallback<PointerUpEvent>(OnPreviewPointerUp);
                _designPreviewViewport.UnregisterCallback<WheelEvent>(OnPreviewWheel);
            }
        }

        private bool _isDraggingPreview;

        /// <summary>
        /// Phase 3B: drag-to-rotate/scroll-to-zoom for the live 3D preview — forwarded
        /// to WorkshopPreviewStage, which owns the actual model-pivot rotation/camera
        /// dolly (see its own doc comment). Pointer capture is taken on the viewport
        /// element itself so a fast drag that briefly leaves the element's bounds
        /// still keeps delivering PointerMoveEvents rather than dropping the drag.
        /// </summary>
        private void OnPreviewPointerDown(PointerDownEvent evt)
        {
            _isDraggingPreview = true;
            _designPreviewViewport.CapturePointer(evt.pointerId);
            previewStage?.BeginDrag();
        }

        private void OnPreviewPointerMove(PointerMoveEvent evt)
        {
            if (!_isDraggingPreview)
                return;
            previewStage?.Rotate(evt.deltaPosition.x);
        }

        private void OnPreviewPointerUp(PointerUpEvent evt)
        {
            _isDraggingPreview = false;
            if (_designPreviewViewport.HasPointerCapture(evt.pointerId))
                _designPreviewViewport.ReleasePointer(evt.pointerId);
            previewStage?.EndDrag();
        }

        private void OnPreviewWheel(WheelEvent evt)
        {
            previewStage?.Zoom(evt.delta.y);
            evt.StopPropagation();
        }

        private void OnDebugAddCurrencyClicked()
        {
            PlayerProgress.Instance?.AddCurrency(DebugCurrencyGrant);
            RefreshAll();
        }

        private void OnCraftModeTabClicked() => SetDesignerMode(DesignerMode.Craft);
        private void OnMissileModeTabClicked() => SetDesignerMode(DesignerMode.Missile);
        private void OnResearchModeTabClicked() => SetDesignerMode(DesignerMode.Research);

        private void SetDesignerMode(DesignerMode mode)
        {
            if (_designerMode == mode)
                return;
            _designerMode = mode;
            UpdateDesignerModeTabStyles();
            RefreshPartPicker(PlayerProgress.Instance);
            // Switching tabs swaps what the live 3D preview shows (full strike drone
            // for Craft/Research, a close-up of just the missile for Missile — see
            // WorkshopPreviewStage's class doc comment) even if no part selection
            // actually changed.
            RefreshDesignPreview(PlayerProgress.Instance);
        }

        private void UpdateDesignerModeTabStyles()
        {
            if (_modeTabCraftButton == null || _modeTabMissileButton == null || _modeTabResearchButton == null)
                return;
            _modeTabCraftButton.EnableInClassList("designer-mode-tab-active", _designerMode == DesignerMode.Craft);
            _modeTabMissileButton.EnableInClassList("designer-mode-tab-active", _designerMode == DesignerMode.Missile);
            _modeTabResearchButton.EnableInClassList("designer-mode-tab-active", _designerMode == DesignerMode.Research);
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

            // Phase 3A: default to the first scenario so PlayerProgress.PendingScenario
            // is already meaningful even if the player never opens the scenario picker
            // — same default-selection convention as every part-picker slot, and the
            // same behavior the old ScenarioPickerOverlay.Start() provided.
            if (_selectedScenario == null && scenarioOptions != null && scenarioOptions.Length > 0)
            {
                _selectedScenario = scenarioOptions[0];
                if (PlayerProgress.Instance != null)
                    PlayerProgress.Instance.PendingScenario = _selectedScenario;
            }

            RefreshAll();
        }

        private void RefreshAll()
        {
            PlayerProgress progress = PlayerProgress.Instance;

            _currencyLabel.text = $"Currency: {(progress != null ? progress.Currency : 0)}";

            RefreshPartPicker(progress);
            RefreshDesignPreview(progress);
            RefreshScenarioPicker();
        }

        /// <summary>
        /// Phase 3A: replaces ScenarioPickerOverlay's OnGUI panel — one row per
        /// scenario (title + objective summary), highlighting the current selection.
        /// Clicking a row sets both the local selection (for the highlight) and
        /// PlayerProgress.PendingScenario (what OnEnterCombatClicked actually reads).
        /// </summary>
        private void RefreshScenarioPicker()
        {
            if (_scenarioPickerContent == null)
                return;

            _scenarioPickerContent.Clear();

            if (scenarioOptions == null || scenarioOptions.Length == 0)
            {
                var emptyLabel = new Label("No scenarios configured");
                emptyLabel.AddToClassList("part-slot-empty-label");
                _scenarioPickerContent.Add(emptyLabel);
                return;
            }

            foreach (ScenarioDefinition scenario in scenarioOptions)
            {
                if (scenario == null)
                    continue;
                _scenarioPickerContent.Add(BuildScenarioRow(scenario));
            }
        }

        private VisualElement BuildScenarioRow(ScenarioDefinition scenario)
        {
            bool isSelected = scenario == _selectedScenario;

            var row = new Button(() => OnScenarioSelected(scenario));
            row.AddToClassList("scenario-row");
            if (isSelected)
                row.AddToClassList("scenario-row-selected");

            var title = new Label(scenario.displayName);
            title.AddToClassList("scenario-row-title");

            var summary = new Label(scenario.objectiveSummary);
            summary.AddToClassList("scenario-row-summary");

            row.Add(title);
            row.Add(summary);
            return row;
        }

        private void OnScenarioSelected(ScenarioDefinition scenario)
        {
            _selectedScenario = scenario;
            if (PlayerProgress.Instance != null)
                PlayerProgress.Instance.PendingScenario = scenario;
            RefreshScenarioPicker();
        }

        /// <summary>
        /// Phase 3B follow-up: the tech tree used to be a permanent always-visible
        /// left column, taking up ~300px regardless of which slot the player was
        /// actually editing — per direct user feedback ("the tech tree shouldn't be
        /// there... not sure where it fits, but not there") it's now a third
        /// "Research" tab sharing the same part-picker column/scroll as Craft and
        /// Missile, rather than its own permanent panel. A real dedicated tech-tree
        /// graph view is still Phase 3C's job (see PLAN.md) — this is just getting it
        /// out of the way of the designer for now.
        /// </summary>
        private void RefreshTechTree(PlayerProgress progress)
        {
            _partPickerScroll.contentContainer.Clear();

            if (techTree == null)
                return;

            foreach (TechNode node in techTree)
                _partPickerScroll.contentContainer.Add(BuildTechRow(node, progress));
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
        /// Builds the active designer mode's part picker: one row per multi-option
        /// slot, each a dropdown listing every currently-unlocked variant (filtered
        /// from the full option arrays via PlayerProgress.IsPartUnlocked, same source
        /// of truth the tech tree uses — Phase 3B follow-up: replaced the original
        /// row-of-buttons-per-slot picker, which stopped scaling once every slot had
        /// several unlocked options). Craft and Missile are now separate tabs
        /// (SetDesignerMode) rather than one long stacked "Missile Loadout" +
        /// "Drone Loadout" list, per direct user feedback that the two need to read
        /// as visually distinct sections. Selecting an option immediately refreshes
        /// the design preview (stats + the live 3D model).
        /// </summary>
        private void RefreshPartPicker(PlayerProgress progress)
        {
            if (_partPickerScroll == null)
                return;

            // Resolve selections before building rows so the "selected" highlight and
            // TryBuildMissileLoadout (called from RefreshDesignPreview right after this)
            // agree on the same choice.
            _selectedMissileAirframe = ResolveSelection(progress, missileAirframeOptions, _selectedMissileAirframe);
            _selectedPayload = ResolveSelection(progress, missilePayloadOptions, _selectedPayload);
            _selectedEngine = ResolveSelection(progress, missileEngineOptions, _selectedEngine);
            _selectedSeeker = ResolveSelection(progress, missileSeekerOptions, _selectedSeeker);
            _selectedCountermeasure = ResolveOptionalSelection(progress, missileCountermeasureOptions, _selectedCountermeasure);
            _selectedJamming = ResolveOptionalSelection(progress, missileJammingOptions, _selectedJamming);

            // Propulsion+Engine merge: filtered to whichever FlightConfiguration the
            // "Airframe Type" toggle currently selects (an Electric package is
            // Multirotor-only, a Jet package is Fixed-Wing-only), via
            // DroneCompatibility, same as the Planform picker below. One merged
            // selection derives both _selectedDronePropulsion/_selectedDroneEngine so
            // every downstream reader of those two fields is unaware a merged picker
            // exists — see DronePropulsionPackageDefinition's own doc comment.
            DronePropulsionPackageDefinition[] compatiblePropulsionPackageOptions =
                FilterPropulsionPackagesByFlightConfig(dronePropulsionPackageOptions, _selectedFlightConfiguration);
            _selectedDronePropulsionPackage = ResolvePropulsionPackageSelection(progress, compatiblePropulsionPackageOptions, _selectedDronePropulsionPackage);
            _selectedDronePropulsion = _selectedDronePropulsionPackage?.propulsion;
            _selectedDroneEngine = _selectedDronePropulsionPackage?.engine;

            // Planform-preset pass: Multirotor keeps two independent Airframe/
            // Wing-or-Rotor dropdowns (unchanged); Fixed-Wing instead resolves one
            // merged Planform selection and derives _selectedDroneAirframe/
            // _selectedDroneWing from it, so every downstream reader of those two
            // fields (TryBuildDroneLoadout, DesignStatsCalculator, VehicleFactory) is
            // completely unaware a merged picker exists.
            DroneAirframeDefinition[] compatibleAirframeOptions = System.Array.Empty<DroneAirframeDefinition>();
            WingOrPropellerDefinition[] compatibleWingOptions = System.Array.Empty<WingOrPropellerDefinition>();
            if (_selectedFlightConfiguration == FlightConfiguration.Multirotor)
            {
                compatibleAirframeOptions = FilterByFlightConfig(droneAirframeOptions, _selectedFlightConfiguration);
                compatibleWingOptions = FilterByFlightConfig(droneWingOptions, _selectedFlightConfiguration);
                _selectedDroneAirframe = ResolveSelection(progress, compatibleAirframeOptions, _selectedDroneAirframe);
                _selectedDroneWing = ResolveSelection(progress, compatibleWingOptions, _selectedDroneWing);
            }
            else
            {
                _selectedDronePlanform = ResolvePlanformSelection(progress, dronePlanformOptions, _selectedDronePlanform);
                _selectedDroneAirframe = _selectedDronePlanform?.airframe;
                _selectedDroneWing = _selectedDronePlanform?.wing;
            }

            _selectedDroneHull = ResolveSelection(progress, droneHullOptions, _selectedDroneHull);
            _selectedDroneFuel = ResolveSelection(progress, droneFuelOptions, _selectedDroneFuel);
            _selectedDroneWeaponBay = ResolveSelection(progress, droneWeaponBayOptions, _selectedDroneWeaponBay);
            _selectedDatalink = ResolveOptionalSelection(progress, missileDatalinkOptions, _selectedDatalink);
            _selectedDroneCountermeasure = ResolveOptionalSelection(progress, droneCountermeasureOptions, _selectedDroneCountermeasure);
            _selectedDroneSensor = ResolveSelection(progress, droneSensorOptions, _selectedDroneSensor);

            if (_designerMode == DesignerMode.Research)
            {
                RefreshTechTree(progress);
                return;
            }

            _partPickerScroll.contentContainer.Clear();

            if (_designerMode == DesignerMode.Missile)
            {
                _partPickerScroll.contentContainer.Add(BuildPartSlotDropdown("Airframe", missileAirframeOptions, progress,
                    _selectedMissileAirframe, allowNone: false, onSelect: selected => { _selectedMissileAirframe = selected; RefreshDesignPreview(progress); }));
                _partPickerScroll.contentContainer.Add(BuildPartSlotDropdown("Payload", missilePayloadOptions, progress,
                    _selectedPayload, allowNone: false, onSelect: selected => { _selectedPayload = selected; RefreshDesignPreview(progress); }));
                _partPickerScroll.contentContainer.Add(BuildPartSlotDropdown("Engine", missileEngineOptions, progress,
                    _selectedEngine, allowNone: false, onSelect: selected => { _selectedEngine = selected; RefreshDesignPreview(progress); }));
                _partPickerScroll.contentContainer.Add(BuildPartSlotDropdown("Seeker", missileSeekerOptions, progress,
                    _selectedSeeker, allowNone: false, onSelect: selected => { _selectedSeeker = selected; RefreshDesignPreview(progress); }));
                _partPickerScroll.contentContainer.Add(BuildPartSlotDropdown("Countermeasure", missileCountermeasureOptions, progress,
                    _selectedCountermeasure, allowNone: true, onSelect: selected => { _selectedCountermeasure = selected; RefreshDesignPreview(progress); }));
                _partPickerScroll.contentContainer.Add(BuildPartSlotDropdown("Jamming / ECM", missileJammingOptions, progress,
                    _selectedJamming, allowNone: true, onSelect: selected => { _selectedJamming = selected; RefreshDesignPreview(progress); }));
                _partPickerScroll.contentContainer.Add(BuildPartSlotDropdown("Datalink", missileDatalinkOptions, progress,
                    _selectedDatalink, allowNone: true, onSelect: selected => { _selectedDatalink = selected; RefreshDesignPreview(progress); }));
                _partPickerScroll.contentContainer.Add(BuildFuelFillSliderRow());
            }
            else
            {
                _partPickerScroll.contentContainer.Add(BuildAirframeTypeToggleRow(progress));
                _partPickerScroll.contentContainer.Add(BuildPropulsionPackageSlotDropdown(progress, compatiblePropulsionPackageOptions));

                if (_selectedFlightConfiguration == FlightConfiguration.Multirotor)
                {
                    _partPickerScroll.contentContainer.Add(BuildPartSlotDropdown("Airframe", compatibleAirframeOptions, progress,
                        _selectedDroneAirframe, allowNone: false, onSelect: selected => { _selectedDroneAirframe = selected; RefreshDesignPreview(progress); }));
                    _partPickerScroll.contentContainer.Add(BuildPartSlotDropdown("Wing / Rotor", compatibleWingOptions, progress,
                        _selectedDroneWing, allowNone: false, onSelect: selected => { _selectedDroneWing = selected; RefreshDesignPreview(progress); }));
                }
                else
                {
                    _partPickerScroll.contentContainer.Add(BuildPlanformSlotDropdown(progress));
                }

                _partPickerScroll.contentContainer.Add(BuildPartSlotDropdown("Hull Material", droneHullOptions, progress,
                    _selectedDroneHull, allowNone: false, onSelect: selected => { _selectedDroneHull = selected; RefreshDesignPreview(progress); }));
                _partPickerScroll.contentContainer.Add(BuildPartSlotDropdown("Fuel", droneFuelOptions, progress,
                    _selectedDroneFuel, allowNone: false, onSelect: selected => { _selectedDroneFuel = selected; RefreshDesignPreview(progress); }));
                _partPickerScroll.contentContainer.Add(BuildPartSlotDropdown("Weapon Bay", droneWeaponBayOptions, progress,
                    _selectedDroneWeaponBay, allowNone: false, onSelect: selected => { _selectedDroneWeaponBay = selected; RefreshDesignPreview(progress); }));
                _partPickerScroll.contentContainer.Add(BuildPartSlotDropdown("Countermeasure", droneCountermeasureOptions, progress,
                    _selectedDroneCountermeasure, allowNone: true, onSelect: selected => { _selectedDroneCountermeasure = selected; RefreshDesignPreview(progress); }));
                _partPickerScroll.contentContainer.Add(BuildPartSlotDropdown("Sensor", droneSensorOptions, progress,
                    _selectedDroneSensor, allowNone: false, onSelect: selected => { _selectedDroneSensor = selected; RefreshDesignPreview(progress); }));
                _partPickerScroll.contentContainer.Add(BuildAmmoCountSliderRow(progress));
            }
        }

        /// <summary>
        /// Depth pass: was hardcoded to always build a 4-missile loadout — now a real
        /// slider, clamped to the currently-selected weapon bay's own maxMunitionCount
        /// (re-clamped every refresh, so switching to a smaller bay after dialing this
        /// up doesn't leave a stale, now-invalid value sitting in the field).
        /// </summary>
        private VisualElement BuildAmmoCountSliderRow(PlayerProgress progress)
        {
            int maxAmmo = Mathf.Max(1, _selectedDroneWeaponBay != null ? _selectedDroneWeaponBay.maxMunitionCount : 1);
            droneAmmoCount = Mathf.Clamp(droneAmmoCount, 1, maxAmmo);

            var row = new VisualElement();
            row.AddToClassList("slider-row");

            var label = new Label($"Ammo: {droneAmmoCount} / {maxAmmo}");
            label.AddToClassList("slider-label");
            row.Add(label);

            var slider = new SliderInt(1, maxAmmo) { value = droneAmmoCount };
            slider.RegisterValueChangedCallback(evt =>
            {
                droneAmmoCount = evt.newValue;
                label.text = $"Ammo: {droneAmmoCount} / {maxAmmo}";
                RefreshDesignPreview(progress);
            });
            row.Add(slider);
            return row;
        }

        /// <summary>
        /// The missile fuel-fill slider used to be a static always-visible row in the
        /// preview panel; it's now built dynamically alongside the Missile tab's other
        /// rows (Phase 3B follow-up) so it only shows up when actually editing the
        /// missile, and disappears cleanly with the rest of that tab's content.
        /// </summary>
        private VisualElement BuildFuelFillSliderRow()
        {
            var row = new VisualElement();
            row.AddToClassList("slider-row");

            var label = new Label($"Fuel fill: {missileFuelFill * 100f:F0}%");
            label.AddToClassList("slider-label");
            row.Add(label);

            var slider = new Slider(0f, 1f) { value = missileFuelFill };
            slider.RegisterValueChangedCallback(evt =>
            {
                missileFuelFill = evt.newValue;
                label.text = $"Fuel fill: {missileFuelFill * 100f:F0}%";
                RefreshDesignPreview(PlayerProgress.Instance);
            });
            row.Add(slider);
            return row;
        }

        /// <summary>
        /// Fixed-wing flight-model rework: a two-way "Airframe Type" segmented toggle
        /// (Multirotor / Fixed-Wing) reusing the same designer-mode-tab visual style as
        /// the Craft/Missile/Research tabs above it, rather than introducing new USS.
        /// Selecting a side filters the Propulsion/Engine dropdowns below to only that
        /// FlightConfiguration's compatible parts (via DroneCompatibility) — Hull
        /// Material, Fuel, Weapon Bay, and Countermeasure stay unfiltered since
        /// they're flight-model-agnostic by design. Planform-preset pass: Multirotor
        /// still shows two independent Airframe/Wing-or-Rotor dropdowns, but
        /// Fixed-Wing now shows one merged "Planform" dropdown instead (see
        /// BuildPlanformSlotDropdown/DronePlanformDefinition) — a real aircraft's
        /// fuselage and wing are one integrated design, not a free cross-product.
        /// This is purely a Workshop UI concern: it doesn't add a field to
        /// DroneLoadout, and DesignStatsCalculator/VehicleFactory are unaware any of
        /// this exists — the tech tree and physics pipeline are unchanged, exactly as
        /// scoped.
        /// </summary>
        private VisualElement BuildAirframeTypeToggleRow(PlayerProgress progress)
        {
            var row = new VisualElement();
            row.AddToClassList("designer-mode-tabs");

            var multirotorButton = new Button(() => OnAirframeTypeSelected(FlightConfiguration.Multirotor, progress)) { text = "Multirotor" };
            multirotorButton.AddToClassList("designer-mode-tab");
            multirotorButton.EnableInClassList("designer-mode-tab-active", _selectedFlightConfiguration == FlightConfiguration.Multirotor);

            var fixedWingButton = new Button(() => OnAirframeTypeSelected(FlightConfiguration.FixedWing, progress)) { text = "Fixed-Wing" };
            fixedWingButton.AddToClassList("designer-mode-tab");
            fixedWingButton.EnableInClassList("designer-mode-tab-active", _selectedFlightConfiguration == FlightConfiguration.FixedWing);

            row.Add(multirotorButton);
            row.Add(fixedWingButton);
            return row;
        }

        private void OnAirframeTypeSelected(FlightConfiguration flightConfiguration, PlayerProgress progress)
        {
            if (_selectedFlightConfiguration == flightConfiguration)
                return;
            _selectedFlightConfiguration = flightConfiguration;
            RefreshPartPicker(progress);
            RefreshDesignPreview(progress);
        }

        private static DroneAirframeDefinition[] FilterByFlightConfig(DroneAirframeDefinition[] options, FlightConfiguration config)
        {
            var matches = new List<DroneAirframeDefinition>();
            if (options != null)
            {
                foreach (var option in options)
                    if (option != null && DroneCompatibility.GetFlightConfiguration(option) == config)
                        matches.Add(option);
            }
            return matches.ToArray();
        }

        private static WingOrPropellerDefinition[] FilterByFlightConfig(WingOrPropellerDefinition[] options, FlightConfiguration config)
        {
            var matches = new List<WingOrPropellerDefinition>();
            if (options != null)
            {
                foreach (var option in options)
                    if (option != null && DroneCompatibility.GetFlightConfiguration(option) == config)
                        matches.Add(option);
            }
            return matches.ToArray();
        }

        private static PropulsionDefinition[] FilterByFlightConfig(PropulsionDefinition[] options, FlightConfiguration config)
        {
            var matches = new List<PropulsionDefinition>();
            if (options != null)
            {
                foreach (var option in options)
                    if (option != null && DroneCompatibility.GetFlightConfiguration(option) == config)
                        matches.Add(option);
            }
            return matches.ToArray();
        }

        private static DroneEngineDefinition[] FilterByFlightConfig(DroneEngineDefinition[] options, FlightConfiguration config)
        {
            var matches = new List<DroneEngineDefinition>();
            if (options != null)
            {
                foreach (var option in options)
                    if (option != null && DroneCompatibility.GetFlightConfiguration(option) == config)
                        matches.Add(option);
            }
            return matches.ToArray();
        }

        /// <summary>
        /// Depth pass: the merged "Propulsion" dropdown shown instead of separate
        /// Propulsion/Engine dropdowns — see DronePropulsionPackageDefinition's own
        /// doc comment for why. Same shape as BuildPlanformSlotDropdown (can't reuse
        /// BuildPartSlotDropdown directly since a package isn't a PartDefinition).
        /// </summary>
        private VisualElement BuildPropulsionPackageSlotDropdown(PlayerProgress progress, DronePropulsionPackageDefinition[] options)
        {
            var row = new VisualElement();
            row.AddToClassList("part-slot-row");

            var slotNameLabel = new Label("Propulsion");
            slotNameLabel.AddToClassList("part-slot-label");
            row.Add(slotNameLabel);

            var unlockedOptions = new List<DronePropulsionPackageDefinition>();
            if (options != null && progress != null)
            {
                foreach (var option in options)
                {
                    if (IsPropulsionPackageUnlocked(progress, option))
                        unlockedOptions.Add(option);
                }
            }

            if (unlockedOptions.Count == 0)
            {
                var emptyLabel = new Label("Unlock more tech");
                emptyLabel.AddToClassList("part-slot-empty-label");
                row.Add(emptyLabel);
                return row;
            }

            var choices = new List<string>();
            foreach (var option in unlockedOptions)
                choices.Add(option.displayName);

            int selectedIndex = _selectedDronePropulsionPackage != null ? unlockedOptions.IndexOf(_selectedDronePropulsionPackage) : 0;

            var dropdown = new DropdownField(choices, Mathf.Clamp(selectedIndex, 0, choices.Count - 1));
            dropdown.AddToClassList("part-option-dropdown");
            dropdown.RegisterValueChangedCallback(evt =>
            {
                int index = choices.IndexOf(evt.newValue);
                _selectedDronePropulsionPackage = index >= 0 && index < unlockedOptions.Count ? unlockedOptions[index] : null;
                _selectedDronePropulsion = _selectedDronePropulsionPackage?.propulsion;
                _selectedDroneEngine = _selectedDronePropulsionPackage?.engine;
                RefreshDesignPreview(progress);
            });
            row.Add(dropdown);
            return row;
        }

        private bool IsPropulsionPackageUnlocked(PlayerProgress progress, DronePropulsionPackageDefinition package)
        {
            return package != null && package.propulsion != null && package.engine != null &&
                   progress.IsPartUnlocked(package.propulsion, techTree) && progress.IsPartUnlocked(package.engine, techTree);
        }

        private DronePropulsionPackageDefinition ResolvePropulsionPackageSelection(PlayerProgress progress,
            DronePropulsionPackageDefinition[] options, DronePropulsionPackageDefinition current)
        {
            if (current != null && progress != null && IsPropulsionPackageUnlocked(progress, current) && ArrayContains(options, current))
                return current;

            if (options == null || progress == null)
                return null;

            foreach (var option in options)
            {
                if (IsPropulsionPackageUnlocked(progress, option))
                    return option;
            }
            return null;
        }

        private static DronePropulsionPackageDefinition[] FilterPropulsionPackagesByFlightConfig(
            DronePropulsionPackageDefinition[] options, FlightConfiguration config)
        {
            var matches = new List<DronePropulsionPackageDefinition>();
            if (options != null)
            {
                foreach (var option in options)
                    if (option?.propulsion != null && DroneCompatibility.GetFlightConfiguration(option.propulsion) == config)
                        matches.Add(option);
            }
            return matches.ToArray();
        }

        /// <summary>
        /// Planform-preset pass: the merged "Planform" dropdown shown instead of
        /// separate Airframe/Wing-or-Rotor dropdowns whenever the Airframe Type
        /// toggle is set to Fixed-Wing. Mirrors BuildPartSlotDropdown's shape (same
        /// USS classes, same "Unlock more tech" empty state) but can't reuse it
        /// directly since DronePlanformDefinition isn't a PartDefinition — a preset
        /// has no cost/tier of its own, "unlocked" means both its airframe and wing
        /// are (see ResolvePlanformSelection).
        /// </summary>
        private VisualElement BuildPlanformSlotDropdown(PlayerProgress progress)
        {
            var row = new VisualElement();
            row.AddToClassList("part-slot-row");

            var slotNameLabel = new Label("Planform");
            slotNameLabel.AddToClassList("part-slot-label");
            row.Add(slotNameLabel);

            var unlockedOptions = new List<DronePlanformDefinition>();
            if (dronePlanformOptions != null && progress != null)
            {
                foreach (var option in dronePlanformOptions)
                {
                    if (IsPlanformUnlocked(progress, option))
                        unlockedOptions.Add(option);
                }
            }

            if (unlockedOptions.Count == 0)
            {
                var emptyLabel = new Label("Unlock more tech");
                emptyLabel.AddToClassList("part-slot-empty-label");
                row.Add(emptyLabel);
                return row;
            }

            var choices = new List<string>();
            foreach (var option in unlockedOptions)
                choices.Add(option.displayName);

            int selectedIndex = _selectedDronePlanform != null ? unlockedOptions.IndexOf(_selectedDronePlanform) : 0;

            var dropdown = new DropdownField(choices, Mathf.Clamp(selectedIndex, 0, choices.Count - 1));
            dropdown.AddToClassList("part-option-dropdown");
            dropdown.RegisterValueChangedCallback(evt =>
            {
                int index = choices.IndexOf(evt.newValue);
                _selectedDronePlanform = index >= 0 && index < unlockedOptions.Count ? unlockedOptions[index] : null;
                _selectedDroneAirframe = _selectedDronePlanform?.airframe;
                _selectedDroneWing = _selectedDronePlanform?.wing;
                RefreshDesignPreview(progress);
            });
            row.Add(dropdown);
            return row;
        }

        private bool IsPlanformUnlocked(PlayerProgress progress, DronePlanformDefinition planform)
        {
            return planform != null && planform.airframe != null && planform.wing != null &&
                   progress.IsPartUnlocked(planform.airframe, techTree) && progress.IsPartUnlocked(planform.wing, techTree);
        }

        /// <summary>
        /// Same "keep current if still valid, else fall back to first unlocked"
        /// pattern as ResolveSelection, adapted for DronePlanformDefinition (not a
        /// PartDefinition — see its own class doc comment for why).
        /// </summary>
        private DronePlanformDefinition ResolvePlanformSelection(PlayerProgress progress, DronePlanformDefinition[] options,
            DronePlanformDefinition current)
        {
            if (current != null && progress != null && IsPlanformUnlocked(progress, current) && ArrayContains(options, current))
                return current;

            if (options == null || progress == null)
                return null;

            foreach (var option in options)
            {
                if (IsPlanformUnlocked(progress, option))
                    return option;
            }
            return null;
        }

        /// <summary>
        /// One labeled DropdownField per slot — Phase 3B follow-up, replacing the
        /// original row-of-buttons-per-slot picker (BuildPartSlotRow) that predated
        /// the live 3D preview and didn't scale well once every slot had many
        /// unlocked options. "None" (for optional slots) is always the dropdown's
        /// first entry when present, so index 0 reliably means "no part selected"
        /// below.
        /// </summary>
        private VisualElement BuildPartSlotDropdown<T>(string slotLabel, T[] options, PlayerProgress progress, T selected,
            bool allowNone, System.Action<T> onSelect) where T : PartDefinition
        {
            var row = new VisualElement();
            row.AddToClassList("part-slot-row");

            var slotNameLabel = new Label(slotLabel);
            slotNameLabel.AddToClassList("part-slot-label");
            row.Add(slotNameLabel);

            var unlockedOptions = new List<T>();
            if (options != null && progress != null)
            {
                foreach (T option in options)
                {
                    if (option != null && progress.IsPartUnlocked(option, techTree))
                        unlockedOptions.Add(option);
                }
            }

            if (unlockedOptions.Count == 0 && !allowNone)
            {
                var emptyLabel = new Label("Unlock more tech");
                emptyLabel.AddToClassList("part-slot-empty-label");
                row.Add(emptyLabel);
                return row;
            }

            var choices = new List<string>();
            if (allowNone)
                choices.Add("None");
            foreach (T option in unlockedOptions)
                choices.Add(option.displayName);

            int selectedIndex = 0;
            if (selected != null)
            {
                int optionIndex = unlockedOptions.IndexOf(selected);
                if (optionIndex >= 0)
                    selectedIndex = allowNone ? optionIndex + 1 : optionIndex;
            }

            var dropdown = new DropdownField(choices, Mathf.Clamp(selectedIndex, 0, choices.Count - 1));
            dropdown.AddToClassList("part-option-dropdown");
            dropdown.RegisterValueChangedCallback(evt =>
            {
                int newIndex = choices.IndexOf(evt.newValue);
                int optionIndex = allowNone ? newIndex - 1 : newIndex;
                onSelect(optionIndex >= 0 && optionIndex < unlockedOptions.Count ? unlockedOptions[optionIndex] : null);
            });
            row.Add(dropdown);
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
            // The membership check (not just "still unlocked") matters specifically for
            // the four Airframe-Type-filtered slots (Airframe/Wing-or-Rotor/Propulsion/
            // Engine — see _selectedFlightConfiguration): a previously-selected part
            // stays fully unlocked when the player flips the toggle, but is no longer
            // present in the freshly-filtered `options` passed in for that refresh, so
            // it must not be kept just because IsPartUnlocked still says yes.
            if (current != null && progress != null && progress.IsPartUnlocked(current, techTree) && ArrayContains(options, current))
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

        private static bool ArrayContains<T>(T[] array, T value) where T : class
        {
            if (array == null)
                return false;
            foreach (T item in array)
            {
                if (item == value)
                    return true;
            }
            return false;
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

        /// <summary>
        /// Phase 3B follow-up: this used to dump ~11 lines of stats into a plain
        /// (non-scrolling) VisualElement sharing space with a fixed-size viewport,
        /// which visibly overflowed and overlapped the scenario picker/deploy
        /// buttons below it once the panel got tall enough — see the compact
        /// `design-stat-card` overlay this now renders into instead (one short line
        /// per craft), directly fixing that overlap rather than just growing the
        /// panel. The live 3D preview (always the strike drone, with its missile
        /// loadout mounted on its hardpoints) is still rebuilt here regardless of
        /// which designer tab is active, so switching to the Missile tab and
        /// changing a part still visibly updates the mounted missiles on the craft.
        /// </summary>
        private void RefreshDesignPreview(PlayerProgress progress)
        {
            if (_designStatCard != null)
                _designStatCard.Clear();

            bool missileWithinMtow = true;
            if (TryBuildMissileLoadout(progress, out MissileLoadout missileLoadout))
            {
                var stats = DesignStatsCalculator.Calculate(missileLoadout);
                missileWithinMtow = stats.isWithinMtow;
                AddDesignLine($"Missile: {(missileWithinMtow ? "READY" : "OVER MTOW")}  " +
                    $"Mass {stats.massKg:F0}kg  Dmg {stats.directDamage:F0}(+{stats.splashDamage:F0})  Range {stats.seekerRangeMeters:F0}m  " +
                    $"Burn {stats.effectiveBurnTimeSeconds:F0}s  RCS {stats.radarCrossSection:F2}",
                    ready: missileWithinMtow);
            }
            else
            {
                AddDesignLine("Missile: incomplete — unlock more tech", ready: false);
            }

            // Phase 3B follow-up: while the Missile tab is active, the live 3D
            // preview swaps to a close-up of just the missile (auto-rotating, zoomed
            // in — see WorkshopPreviewStage) instead of the full strike drone, so its
            // seeker nose/fin detail is actually readable rather than a tiny speck
            // mounted on a much bigger aircraft.
            if (_designerMode == DesignerMode.Missile)
                previewStage?.SetMissileLoadout(missileLoadout.IsComplete ? missileLoadout : null, Team.Player);

            bool strikeDroneWithinMtow = true;
            bool strikeDroneFlightConfigOk = true;
            bool strikeDroneFuelOk = true;
            if (TryBuildDroneLoadout(progress, missileLoadout, includeWeapon: true, out DroneLoadout droneLoadout))
            {
                var stats = DesignStatsCalculator.Calculate(droneLoadout);
                strikeDroneWithinMtow = stats.isWithinMtow;
                strikeDroneFlightConfigOk = stats.isFlightConfigurationCompatible;
                strikeDroneFuelOk = stats.isFuelCompatible;

                // Craft/Research tabs show the full strike drone (the design that
                // actually flies in combat, with its missile loadout mounted on its
                // hardpoints per current ammoCount).
                if (_designerMode != DesignerMode.Missile)
                    previewStage?.SetDroneLoadout(droneLoadout, Team.Player);

                string flightModel = stats.requiresForwardFlight ? "Fixed-wing/jet" : "Multirotor";
                bool strikeDroneReady = strikeDroneWithinMtow && strikeDroneFlightConfigOk && strikeDroneFuelOk;
                string statusText = !strikeDroneFlightConfigOk || !strikeDroneFuelOk ? "MISMATCHED PARTS" : (strikeDroneWithinMtow ? "READY" : "OVER MTOW");
                AddDesignLine($"Strike Drone: {statusText}  " +
                    $"Mass {stats.massKg:F0}kg  Health {stats.maxHealth:F0}  Sensor {stats.sensorRangeMeters:F0}m  " +
                    $"RCS {stats.radarCrossSection:F2}  ({flightModel})",
                    ready: strikeDroneReady, header: true);
                if (!strikeDroneFlightConfigOk)
                    AddDesignLine($"  {stats.flightConfigurationMismatchReason}", ready: false);
                if (!strikeDroneFuelOk)
                    AddDesignLine($"  {stats.fuelMismatchReason}", ready: false);
            }
            else
            {
                AddDesignLine("Strike Drone: incomplete — unlock more tech", ready: false, header: true);
                if (_designerMode != DesignerMode.Missile)
                    previewStage?.SetDroneLoadout(null, Team.Player);
            }

            if (TryBuildDroneLoadout(progress, null, includeWeapon: false, out DroneLoadout scoutLoadout, useScoutSensor: true))
            {
                var stats = DesignStatsCalculator.Calculate(scoutLoadout);
                AddDesignLine($"Scout Drone: READY  Sensor {stats.sensorRangeMeters:F0}m  RCS {stats.radarCrossSection:F2} (shares contacts)", ready: true, header: true);
            }
            else
            {
                AddDesignLine("Scout Drone: incomplete — unlock more tech", ready: false, header: true);
            }

            bool combatReady = missileLoadout.IsComplete && missileWithinMtow && strikeDroneWithinMtow &&
                                strikeDroneFlightConfigOk && strikeDroneFuelOk &&
                                TryBuildDroneLoadout(progress, null, true, out _) &&
                                TryBuildDroneLoadout(progress, null, false, out _, useScoutSensor: true);

            _enterCombatButton.SetEnabled(combatReady);
            _enterCombatButton.text = combatReady
                ? "Enter Combat"
                : (!missileWithinMtow ? "Missile over MTOW — reduce fuel/parts"
                    : (!strikeDroneFlightConfigOk ? "Fix mismatched airframe/wing/propulsion/engine parts"
                        : (!strikeDroneFuelOk ? "Fix mismatched fuel/propulsion parts"
                            : (!strikeDroneWithinMtow ? "Strike drone over MTOW — reduce fuel/parts" : "Unlock more tech to proceed"))));
        }

        private void AddDesignLine(string text, bool? ready = null, bool header = false)
        {
            if (_designStatCard == null)
                return;
            var label = new Label(text);
            label.AddToClassList("design-line");
            if (header)
                label.AddToClassList("design-header-line");
            if (ready.HasValue)
                label.AddToClassList(ready.Value ? "design-line-ready" : "design-line-incomplete");
            _designStatCard.Add(label);
        }

        private void OnEnterCombatClicked()
        {
            if (!_enterCombatButton.enabledSelf)
                return;

            PlayerProgress progress = StashCurrentLoadouts();

            // Phase 2E/3A: load whichever scenario was picked via the in-UI scenario
            // picker above, if any, falling back to the single hardcoded default scene
            // so Combat_Arena01 stays reachable even when no picker was ever shown
            // (e.g. headless batch regression tests that never touch the Workshop
            // scene at all). GameFlowController.ResolveCombatScene is the pure,
            // headlessly-testable version of this same fallback logic.
            GameFlowController.LoadCombat(progress?.PendingScenario, combatSceneName);
        }

        /// <summary>
        /// Phase 2G/3A: Test Range entry point — same design-readiness gate and the
        /// same PlayerProgress stashing as Enter Combat (so the Test Range shows the
        /// player's actual current design, not a placeholder), but loads
        /// testRangeSceneName instead and never touches PendingScenario, since Test
        /// Range isn't one of the scenario picker's choices. Public so it can also be
        /// called directly (e.g. from tests); wired to the in-UI "Test Range" button
        /// in OnEnable.
        /// </summary>
        public void EnterTestRange()
        {
            if (!_enterCombatButton.enabledSelf)
                return;

            StashCurrentLoadouts();
            GameFlowController.LoadTestRange(testRangeSceneName);
        }

        /// <summary>
        /// Stashes the actual configured designs on PlayerProgress (a DontDestroyOnLoad
        /// singleton that survives the scene load) so the destination scene's
        /// CombatPlayerLoadoutApplier can spawn the player's real chosen loadout
        /// instead of that scene's editor-time-baked Tier-0 default. Without this,
        /// every part-picker selection in this UI would be purely cosmetic — it would
        /// compute preview stats here and then have zero effect on the actual battle
        /// (or Test Range run). Shared by OnEnterCombatClicked and EnterTestRange so
        /// both destinations see identical loadout-building logic.
        /// </summary>
        private PlayerProgress StashCurrentLoadouts()
        {
            PlayerProgress progress = PlayerProgress.Instance;
            if (progress == null)
                return null;

            TryBuildMissileLoadout(progress, out MissileLoadout missileLoadout);
            if (TryBuildDroneLoadout(progress, missileLoadout, includeWeapon: true, out DroneLoadout strikeLoadout))
                progress.PendingStrikeDroneLoadout = strikeLoadout;
            if (TryBuildDroneLoadout(progress, null, includeWeapon: false, out DroneLoadout scoutLoadout, useScoutSensor: true))
                progress.PendingScoutDroneLoadout = scoutLoadout;

            return progress;
        }

        private bool TryBuildMissileLoadout(PlayerProgress progress, out MissileLoadout loadout)
        {
            loadout = new MissileLoadout { designName = "Basic Missile" };
            if (progress == null)
                return false;

            loadout.airframe = IsUnlocked(progress, _selectedMissileAirframe) ? _selectedMissileAirframe : null;
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

            // Depth pass: the scout drone still always uses the dedicated Scout sensor
            // by design (a scout's whole purpose is the long-range shared-contact
            // sensor); the strike drone now uses whatever the player actually
            // selected instead of being hardcoded to droneSensorBasic regardless of
            // what's unlocked — see droneSensorBasic's own tooltip.
            SensorSuiteDefinition sensor = useScoutSensor ? droneSensorScout : _selectedDroneSensor;
            loadout.sensorSuite = IsUnlocked(progress, sensor) ? sensor : null;

            if (includeWeapon)
            {
                loadout.missileLoadout = missileLoadout;
                loadout.ammoCount = droneAmmoCount;
            }

            return loadout.IsComplete;
        }

        private bool IsUnlocked(PlayerProgress progress, PartDefinition part)
        {
            return progress != null && progress.IsPartUnlocked(part, techTree);
        }
    }
}
