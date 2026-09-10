using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vanquish.Core;
using Vanquish.Theatre;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// A basic, proof-of-concept **visible and playable** theatre map (PLAN.md
    /// Phase 2) — builds a real hex grid you can see and click on, not just OnGUI
    /// buttons (see the earlier <c>TheatreDebugHarness</c>). A rectangular patch of
    /// hexes with mixed terrain (open/road/mountain) and a front line down the
    /// middle, two factories and two bases (one pair per side), all driven by the
    /// same <see cref="Vanquish.Theatre.HexGrid"/>/<see cref="Site"/>/
    /// <see cref="TheatreTurnController"/> logic already proven in SmokeTest. Click a
    /// hex to select it; if it's adjacent to Player territory you can capture it
    /// (a simple stand-in for "won a combat instance here," until the real
    /// combat-instance-to-theatre feedback loop exists); Advance Turn ticks
    /// construction/production and checks both victory conditions.
    ///
    /// To use: open Assets/_Project/Scenes/Phase2_TheatreMap.unity and press Play.
    /// WASD or left-click-drag pans the camera, right-click-drag orbits it, scroll
    /// zooms (smoothly), and clicking a hex without dragging selects it.
    /// </summary>
    public class TheatreMapHarness : MonoBehaviour
    {
        // 10x10 = 100 hexes, split 5 columns (50 hexes) per side.
        private const int Columns = 10;
        private const int Rows = 10;
        private const float HexRadius = 1f;

        // Tall enough relative to HexRadius to read as a solid block/tile with a
        // flat lid you can see things resting on, not a thin flat disc (which, at a
        // low camera angle, visually reads as a shallow dish instead of a tile).
        private const float HexHeight = 0.6f;
        private const float MountainHeight = 2f;

        private static readonly Color[] PlanAccentPalette =
        {
            new Color(0.2f, 0.7f, 0.9f), new Color(0.9f, 0.55f, 0.15f),
            new Color(0.55f, 0.85f, 0.3f), new Color(0.8f, 0.3f, 0.75f),
        };

        private readonly Dictionary<HexCoordinate, HexTileView> _tileViews = new Dictionary<HexCoordinate, HexTileView>();
        private readonly List<SiteMarkerView> _siteViews = new List<SiteMarkerView>();
        private readonly Dictionary<Site, List<ProductionOrder>> _productionQueues = new Dictionary<Site, List<ProductionOrder>>();

        /// <summary>Each Airfield's/Warehouse's stored drone/missile counts by Plan — replaces the old global player-wide inventory pool now that storage is capacity-limited and site-scoped.</summary>
        private readonly Dictionary<Site, Dictionary<DronePlan, int>> _siteStorage = new Dictionary<Site, Dictionary<DronePlan, int>>();

        /// <summary>Every in-progress inter-site logistics shipment — see <see cref="TryBeginTransfer"/>.</summary>
        private readonly List<TransferOrder> _transferOrders = new List<TransferOrder>();

        /// <summary>Every transfer always takes exactly this many turns, regardless of how far apart the two sites are (no travel-time-by-distance modeling in this POC).</summary>
        private const int TransferTurns = 1;

        /// <summary>Non-null while the player is in "pick a destination" mode after clicking Move on a storage card — see <see cref="BeginTransferPicking"/>/<see cref="HandleWorldClick"/>.</summary>
        private Site _transferSourceSite;
        private DronePlan _transferPlan;

        private readonly List<Army> _armies = new List<Army>();
        private readonly List<ArmyMarkerView> _armyViews = new List<ArmyMarkerView>();

        /// <summary>Counts staged (but not yet submitted) for the "Deploy Army" panel at the currently selected Airfield — cleared whenever the selected hex changes.</summary>
        private readonly Dictionary<DronePlan, int> _pendingDeployment = new Dictionary<DronePlan, int>();

        private string _planNameInput = "";
        private string _planFeedback;
        private string _combatFeedback;

        /// <summary>IDs of every <see cref="TheatreTechNode"/> the player has unlocked at the Lab's Tech Tree panel — see <see cref="TryUnlockTech"/>.</summary>
        private readonly HashSet<string> _unlockedTechIds = new HashSet<string>();

        public TheatreWorldState World { get; private set; }
        public TheatreTurnController Controller { get; private set; }
        public HexTile SelectedTile { get; private set; }
        public Army SelectedArmy { get; private set; }

        private TerritorialControlCondition _territorialCondition;

        /// <summary>Every Plan the player has designed at any Lab so far (POC scope: Player only, one shared catalog rather than per-Lab).</summary>
        public List<DronePlan> PlayerPlans { get; } = new List<DronePlan>();

        /// <summary>Every field army currently on the map, for either faction.</summary>
        public IReadOnlyList<Army> Armies => _armies;

        private void Start()
        {
            Build();
        }

        /// <summary>
        /// The actual scene-assembly logic, factored out of <see cref="Start"/> so it
        /// can be driven directly from a headless smoke test (same reasoning as
        /// FlightTestHarness.Build / EngagementController.Initialize).
        /// </summary>
        public void Build()
        {
            BuildLight();

            var grid = new HexGrid();
            World = new TheatreWorldState(grid);

            BuildTiles(grid);
            BuildSites();

            _territorialCondition = new TerritorialControlCondition(requiredFraction: 0.75f, requiredConsecutiveTurns: 3);
            Controller = new TheatreTurnController(World, new ITheatreVictoryCondition[]
            {
                new EconomicCollapseCondition(),
                _territorialCondition,
            });

            BuildCamera();
            BuildMenu();

            // The base Quadcopter airframe is always available — no research needed.
            _unlockedTechIds.Add(TheatreTechCatalog.DefaultUnlockedId);
        }

        /// <summary>Height of the bottom action bar (see <see cref="DrawBottomBar"/>) — used to stop world-space clicks/drags/scrolls "reaching through" the UI (see IsPointerOverUI), and by <see cref="TechTreeController"/> to keep its modal panel from overlapping the bar.</summary>
        public const float BottomBarHeight = 270f;

        /// <summary>
        /// True if the mouse cursor is currently over the bottom bar (or the pause
        /// menu). Checked by both this harness's own click-to-select handling and
        /// the camera controller's pan/orbit/zoom, so clicking a button in the bar
        /// etc. can never also select/deselect a hex or move the camera underneath
        /// it (the original "focus goes when I click build" bug).
        /// </summary>
        public bool IsPointerOverUI()
        {
            if (Input.mousePosition.y <= BottomBarHeight)
                return true;

            if (_menu != null && _menu.IsPointerOverMenu())
                return true;

            return _techTree != null && _techTree.IsPointerOverPanel();
        }

        private GameMenuController _menu;
        private TechTreeController _techTree;

        private void BuildLight()
        {
            var lightGo = new GameObject("Directional Light");
            lightGo.transform.SetParent(transform, worldPositionStays: true);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
        }

        /// <summary>
        /// Standard "odd-r" offset-to-axial conversion (redblobgames) — lets the map
        /// be authored as a plain rectangular grid of columns/rows while storing and
        /// simulating tiles as proper axial <see cref="HexCoordinate"/>s underneath.
        /// </summary>
        private static HexCoordinate OffsetToAxial(int col, int row)
        {
            int q = col - (row - (row & 1)) / 2;
            return new HexCoordinate(q, row);
        }

        private void BuildTiles(HexGrid grid)
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    HexCoordinate coord = OffsetToAxial(col, row);

                    TerrainType terrain;
                    if ((col == 4 && row == 0) || (col == 4 && row == Rows - 1))
                        terrain = TerrainType.Mountain; // a couple of impassable flanks, for visual/terrain variety
                    else if (row == Rows / 2)
                        terrain = TerrainType.Road; // a highway cutting across, rear to front
                    else
                        terrain = TerrainType.Open;

                    TheatreFaction owner = col <= Columns / 2 - 1 ? TheatreFaction.Player : TheatreFaction.Enemy;

                    HexTile tile = grid.GetOrAddTile(coord, terrain, owner);
                    CreateTileView(tile);
                }
            }
        }

        private void CreateTileView(HexTile tile)
        {
            float height = tile.Terrain == TerrainType.Mountain ? MountainHeight : HexHeight;
            Vector3 worldPos = HexMeshFactory.AxialToWorld(tile.Coordinate, HexRadius);
            worldPos.y = height * 0.5f;

            var go = new GameObject($"Hex_{tile.Coordinate}");
            go.transform.SetParent(transform, worldPositionStays: true);
            go.transform.position = worldPos;

            Mesh mesh = HexMeshFactory.CreateHexPrism(HexRadius * 0.98f, height); // slight gap between tiles reads as grid lines
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshCollider>().sharedMesh = mesh;

            var view = go.AddComponent<HexTileView>();
            view.Initialize(tile);
            _tileViews[tile.Coordinate] = view;
        }

        private void BuildSites()
        {
            CreateSite(SiteType.Factory, TheatreFaction.Player, OffsetToAxial(1, Rows / 2));
            CreateSite(SiteType.Base, TheatreFaction.Player, OffsetToAxial(0, 1));
            CreateSite(SiteType.Factory, TheatreFaction.Enemy, OffsetToAxial(Columns - 2, Rows / 2));
            CreateSite(SiteType.Base, TheatreFaction.Enemy, OffsetToAxial(Columns - 1, Rows - 2));
        }

        /// <summary>Seeds a pre-built site (map setup only) — instantly Operational, skipping the normal construction wait.</summary>
        private void CreateSite(SiteType type, TheatreFaction owner, HexCoordinate coord)
        {
            Site site = Site.BeginConstruction(type, owner, coord, turnsToBuild: 1);
            site.Tick(); // instantly Operational for this POC's starting state
            World.Sites.Add(site);
            CreateSiteMarker(site);
        }

        private void CreateSiteMarker(Site site)
        {
            HexTile tile = World.Grid.GetTile(site.Location);
            float tileHeight = tile != null && tile.Terrain == TerrainType.Mountain ? MountainHeight : HexHeight;

            // SiteVisualBuilder's shapes are authored resting on y=0 of their
            // parent, so the root just needs to sit at the hex's surface height —
            // no per-type vertical offset needed here.
            Vector3 worldPos = HexMeshFactory.AxialToWorld(site.Location, HexRadius);
            worldPos.y = tileHeight;

            GameObject go = new GameObject($"Site_{site.Type}_{site.Owner}");
            go.transform.SetParent(transform, worldPositionStays: true);
            go.transform.position = worldPos;

            Renderer[] renderers = SiteVisualBuilder.Build(go.transform, site.Type, Color.white); // Refresh() below applies the real owner/state color immediately

            var marker = go.AddComponent<SiteMarkerView>();
            marker.Initialize(site, renderers);
            _siteViews.Add(marker);
        }

        private TheatreMapCameraController _cameraController;

        private void BuildCamera()
        {
            Vector3 gridCenter = HexMeshFactory.AxialToWorld(OffsetToAxial(Columns / 2, Rows / 2), HexRadius);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.transform.SetParent(transform, worldPositionStays: true);
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.position = gridCenter + new Vector3(0f, 26f, -16f);
            cameraGo.transform.LookAt(gridCenter);

            _cameraController = cameraGo.AddComponent<TheatreMapCameraController>();
            _cameraController.focusPoint = gridCenter;
            _cameraController.isPointerOverUI = IsPointerOverUI;
        }

        private void BuildMenu()
        {
            var menuGo = new GameObject("GameMenu");
            menuGo.transform.SetParent(transform, worldPositionStays: true);
            var menu = menuGo.AddComponent<GameMenuController>();
            menu.harness = this;
            _menu = menu;

            var techTreeGo = new GameObject("TechTree");
            techTreeGo.transform.SetParent(transform, worldPositionStays: true);
            var techTree = techTreeGo.AddComponent<TechTreeController>();
            techTree.harness = this;
            _techTree = techTree;
        }

        // ---- Tech tree -------------------------------------------------------

        /// <summary>True if the player has already unlocked the given <see cref="TheatreTechNode"/>.</summary>
        public bool IsTechUnlocked(string techId) => _unlockedTechIds.Contains(techId);

        /// <summary>
        /// Spends research points (see TheatreTurnController.ResearchPool) to unlock a
        /// tech node, if it isn't already unlocked, every prerequisite is already
        /// unlocked, and the Player can afford its cost.
        /// </summary>
        public bool TryUnlockTech(TheatreTechNode node, out string error)
        {
            if (_unlockedTechIds.Contains(node.Id))
            {
                error = "Already unlocked.";
                return false;
            }

            foreach (string prerequisiteId in node.PrerequisiteIds)
            {
                if (!_unlockedTechIds.Contains(prerequisiteId))
                {
                    error = "Missing prerequisites.";
                    return false;
                }
            }

            if (Controller.ResearchPool[TheatreFaction.Player] < node.ResearchCost)
            {
                error = "Not enough research points.";
                return false;
            }

            Controller.ResearchPool[TheatreFaction.Player] -= node.ResearchCost;
            _unlockedTechIds.Add(node.Id);
            error = null;
            return true;
        }

        // Left mouse button now also drag-pans the camera (TheatreMapCameraController)
        // — so a hex is only selected on mouse-up if the press-to-release movement
        // stayed under this threshold, otherwise it was a drag, not a click.
        private const float ClickDragThresholdPixels = 6f;
        private Vector3 _mouseDownScreenPosition;
        private bool _mouseDownWasOverUI;

        /// <summary>What's currently under the mouse cursor (not clicked, just hovered) — see <see cref="DrawHoverTooltip"/>. Null/null when the pointer is over the bottom bar or nothing 3D at all.</summary>
        private Army _hoveredArmy;
        private HexTile _hoveredTile;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _mouseDownScreenPosition = Input.mousePosition;
                _mouseDownWasOverUI = IsPointerOverUI();
            }

            // If the press started on a GUI button/panel (e.g. "Build", "Advance
            // Turn"), never treat the eventual release as a world-space hex click —
            // this was the actual cause of the reported "focus goes when I click
            // build" bug (the click was reaching through to the 3D scene underneath).
            if (Input.GetMouseButtonUp(0) && Camera.main != null && !_mouseDownWasOverUI)
            {
                float dragDistance = Vector3.Distance(Input.mousePosition, _mouseDownScreenPosition);
                if (dragDistance <= ClickDragThresholdPixels)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                        HandleWorldClick(hit);
                }
            }

            UpdateHover();
        }

        /// <summary>Refreshes <see cref="_hoveredArmy"/>/<see cref="_hoveredTile"/> every frame from whatever's currently under the mouse — used only for the lightweight hover tooltip, never for click handling.</summary>
        private void UpdateHover()
        {
            _hoveredArmy = null;
            _hoveredTile = null;

            if (Camera.main == null || IsPointerOverUI())
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit))
                return;

            ArmyMarkerView armyView = hit.collider.GetComponentInParent<ArmyMarkerView>();
            if (armyView != null)
            {
                _hoveredArmy = armyView.Army;
                return;
            }

            HexTileView tileView = hit.collider.GetComponent<HexTileView>();
            if (tileView != null)
                _hoveredTile = tileView.Tile;
        }

        /// <summary>
        /// Resolves one world-space click, whether it landed on an army marker or a
        /// hex tile: if an army is already selected, movable, and the click landed
        /// on one of its adjacent hexes (highlighted — see
        /// <see cref="UpdateMoveHighlights"/>), it's treated as a move/attack order
        /// there directly (an empty hex, a hostile army's marker, or a hostile
        /// site's hex all resolve the same way via <see cref="TryMoveArmy"/>) —
        /// no separate "select destination then confirm" step. Otherwise, clicking
        /// an army marker selects that army, and clicking a hex tile selects it
        /// (matching the previous select-a-hex behavior).
        /// </summary>
        private void HandleWorldClick(RaycastHit hit)
        {
            ArmyMarkerView armyHit = hit.collider.GetComponentInParent<ArmyMarkerView>();
            HexTileView tileHit = armyHit == null ? hit.collider.GetComponent<HexTileView>() : null;

            HexCoordinate? clickedCoordinate = armyHit != null ? armyHit.Army.Location
                : tileHit != null ? tileHit.Tile.Coordinate
                : (HexCoordinate?)null;

            // Transfer-picking mode (see BeginTransferPicking) takes priority over
            // everything else while active: any click either completes the
            // transfer (clicked a valid highlighted destination) or cancels
            // picking mode and falls through to a normal select below.
            if (_transferSourceSite != null)
            {
                Site clickedSite = clickedCoordinate.HasValue ? SiteAt(clickedCoordinate.Value) : null;
                string transferError = "Click a highlighted hex to choose a destination.";
                bool completed = clickedSite != null && TryBeginTransfer(_transferSourceSite, clickedSite, _transferPlan, out transferError);
                _combatFeedback = completed ? $"Moving {_transferPlan.Name} to {DescribeSite(clickedSite)} (1t)." : transferError;
                CancelTransferPicking();
                if (completed)
                    return;
                // Not a valid destination — cancel picking and treat this click as a normal select instead of doing nothing.
            }

            if (clickedCoordinate.HasValue && SelectedArmy != null && SelectedArmy.Owner == TheatreFaction.Player &&
                !SelectedArmy.HasMovedThisTurn && HexCoordinate.Distance(SelectedArmy.Location, clickedCoordinate.Value) == 1)
            {
                TryMoveArmy(SelectedArmy, clickedCoordinate.Value);
                return;
            }

            if (armyHit != null)
                SelectArmyDirect(armyHit.Army);
            else if (tileHit != null)
                SelectTile(tileHit.Tile);
        }

        /// <summary>
        /// Public (not just Update-private) so this can be driven directly from a
        /// headless test without needing to simulate real mouse input/raycasts.
        /// </summary>
        public void SelectTile(HexTile tile)
        {
            SelectedTile = tile;
            SelectedArmy = null;
            _pendingDeployment.Clear();
            _combatFeedback = null;
            CancelTransferPicking();
            UpdateMoveHighlights();
        }

        /// <summary>Every army (either faction) currently sitting on the given hex — multiple same-owner armies can stack (no auto-merge).</summary>
        public List<Army> ArmiesAt(HexCoordinate coordinate) => _armies.Where(a => a.Location.Equals(coordinate)).ToList();

        /// <summary>
        /// Selects an army directly (e.g. clicking its marker on the map — see
        /// <see cref="HandleWorldClick"/>) — also updates <see cref="SelectedTile"/>
        /// to the hex it's standing on so the rest of the info panel stays
        /// consistent, without the old "select the tile, then find the army in the
        /// 'Armies here' list" detour (still available via <see cref="ArmiesAt"/>
        /// for disambiguating multiple stacked armies on one hex).
        /// </summary>
        public void SelectArmyDirect(Army army)
        {
            SelectedTile = World.Grid.GetTile(army.Location);
            SelectedArmy = army;
            _pendingDeployment.Clear();
            _combatFeedback = null;
            CancelTransferPicking();
            UpdateMoveHighlights();
        }

        /// <summary>
        /// Enters "pick a destination" mode for moving <paramref name="source"/>'s
        /// entire current stock of <paramref name="plan"/> — every eligible
        /// destination hex lights up (see <see cref="UpdateMoveHighlights"/>);
        /// the next click either completes the transfer (see
        /// <see cref="HandleWorldClick"/>/<see cref="TryBeginTransfer"/>) or, if it
        /// wasn't a valid destination, cancels picking mode.
        /// </summary>
        public void BeginTransferPicking(Site source, DronePlan plan)
        {
            _transferSourceSite = source;
            _transferPlan = plan;
            _combatFeedback = null;
            UpdateMoveHighlights();
        }

        /// <summary>Leaves transfer-picking mode (e.g. the player clicked an invalid hex, or a different card) without moving anything.</summary>
        public void CancelTransferPicking()
        {
            if (_transferSourceSite == null)
                return;

            _transferSourceSite = null;
            _transferPlan = null;
            UpdateMoveHighlights();
        }

        /// <summary>True while <see cref="BeginTransferPicking"/> is active — the bottom bar shows a "click a highlighted hex" prompt while this is true.</summary>
        public bool IsPickingTransferDestination => _transferSourceSite != null;

        /// <summary>
        /// Drives two independent hex tints (see <see cref="HexTileView"/>): a
        /// strong "selected" glow on whichever hex a selected site/army/tile is on
        /// (so the player can always see what they've got open, matching the
        /// reference UI's highlighted-factory look), and a "reachable" tint on
        /// every hex that's currently a legal click target — either the selected
        /// army's 6 neighbors (move/attack) or, while <see cref="IsPickingTransferDestination"/>,
        /// every eligible transfer destination. Both clear whenever nothing
        /// applicable is selected/movable/picking.
        /// </summary>
        private void UpdateMoveHighlights()
        {
            HexCoordinate? selectedCoordinate = SelectedArmy != null ? SelectedArmy.Location : SelectedTile?.Coordinate;

            HashSet<HexCoordinate> reachable = null;
            if (_transferSourceSite != null)
            {
                reachable = new HashSet<HexCoordinate>(EligibleTransferDestinations(_transferSourceSite, _transferPlan).Select(s => s.Location));
            }
            else if (SelectedArmy != null && SelectedArmy.Owner == TheatreFaction.Player && !SelectedArmy.HasMovedThisTurn &&
                Controller.Result == TheatreResult.InProgress)
            {
                reachable = new HashSet<HexCoordinate>(SelectedArmy.Location.Neighbors().Where(n => World.Grid.GetTile(n) != null));
            }

            foreach (KeyValuePair<HexCoordinate, HexTileView> kv in _tileViews)
            {
                kv.Value.SetSelected(selectedCoordinate.HasValue && kv.Key.Equals(selectedCoordinate.Value));
                kv.Value.SetHighlighted(reachable != null && reachable.Contains(kv.Key));
            }

            foreach (ArmyMarkerView view in _armyViews)
            {
                if (view != null)
                    view.SetSelected(view.Army == SelectedArmy);
            }
        }

        /// <summary>True if the selected hex is a non-Player hex adjacent to at least one Player-owned hex.</summary>
        public bool CanCaptureSelectedTile()
        {
            if (SelectedTile == null || SelectedTile.Owner == TheatreFaction.Player)
                return false;

            return World.Grid.NeighborsOf(SelectedTile.Coordinate).Any(neighbor => neighbor.Owner == TheatreFaction.Player);
        }

        /// <summary>
        /// A simple stand-in for "the player won a combat instance targeting this
        /// hex" — sets ownership directly and refreshes the visuals. Returns false
        /// (no-op) if <see cref="CanCaptureSelectedTile"/> would be false.
        /// </summary>
        public bool TryCaptureSelectedTile()
        {
            if (!CanCaptureSelectedTile())
                return false;

            SelectedTile.Owner = TheatreFaction.Player;
            RefreshAllViews();
            return true;
        }

        /// <summary>Terrain a site can actually be constructed on — blocks Mountain (impassable) and Road (a route, not a building plot).</summary>
        public static bool IsTerrainBuildable(TerrainType terrain) => terrain == TerrainType.Open;

        /// <summary>True if any non-destroyed site already occupies this hex.</summary>
        public bool HasActiveSite(HexCoordinate coordinate) =>
            World.Sites.Any(s => s.Location.Equals(coordinate) && s.State != SiteState.Destroyed);

        /// <summary>True if the player could start construction of something on the currently selected hex.</summary>
        public bool CanBuildOnSelectedTile()
        {
            if (SelectedTile == null || SelectedTile.Owner != TheatreFaction.Player)
                return false;

            return IsTerrainBuildable(SelectedTile.Terrain) && !HasActiveSite(SelectedTile.Coordinate);
        }

        /// <summary>
        /// Starts construction of <paramref name="type"/> on the selected hex — the
        /// site becomes Operational automatically after <paramref name="turnsToBuild"/>
        /// calls to <see cref="AdvanceTurn"/> (Site's own existing turn-based
        /// lifecycle; no new construction logic needed here). Returns false (no-op)
        /// if <see cref="CanBuildOnSelectedTile"/> would be false.
        /// </summary>
        public bool TryBeginConstructionOnSelectedTile(SiteType type, int turnsToBuild)
        {
            if (!CanBuildOnSelectedTile())
                return false;

            Site site = Site.BeginConstruction(type, TheatreFaction.Player, SelectedTile.Coordinate, turnsToBuild);
            World.Sites.Add(site);
            CreateSiteMarker(site); // Initialize() -> Refresh() shows it dimmed/under-construction immediately
            return true;
        }

        /// <summary>The non-destroyed site (if any) sitting on the given hex.</summary>
        private Site SiteAt(HexCoordinate coordinate) =>
            World.Sites.FirstOrDefault(s => s.Location.Equals(coordinate) && s.State != SiteState.Destroyed);

        /// <summary>
        /// True if there's an unbroken chain of hexes owned by <paramref name="site"/>'s
        /// faction connecting it back to one of that faction's Base sites — a supply
        /// line requirement for <see cref="Site.BeginRepair"/> (see the site card's
        /// Repair action): a site cut off behind enemy-held territory can't be
        /// resupplied/repaired even if it's still standing. Only gates repair for
        /// now — a natural extension point if other logistics-dependent actions
        /// (e.g. production) ever need the same rule.
        /// </summary>
        public bool HasSupplyLineToBase(Site site)
        {
            if (site == null)
                return false;

            HexTile startTile = World.Grid.GetTile(site.Location);
            if (startTile == null || startTile.Owner != site.Owner)
                return false;

            List<Site> bases = World.Sites.Where(s => s.Owner == site.Owner && s.Type == SiteType.Base && s.State != SiteState.Destroyed).ToList();
            if (bases.Count == 0)
                return false;
            if (bases.Any(b => b.Location.Equals(site.Location)))
                return true;

            var visited = new HashSet<HexCoordinate> { site.Location };
            var frontier = new Queue<HexCoordinate>();
            frontier.Enqueue(site.Location);

            while (frontier.Count > 0)
            {
                HexCoordinate current = frontier.Dequeue();
                foreach (HexTile neighborTile in World.Grid.NeighborsOf(current))
                {
                    if (neighborTile.Owner != site.Owner || visited.Contains(neighborTile.Coordinate))
                        continue;

                    if (bases.Any(b => b.Location.Equals(neighborTile.Coordinate)))
                        return true;

                    visited.Add(neighborTile.Coordinate);
                    frontier.Enqueue(neighborTile.Coordinate);
                }
            }

            return false;
        }

        /// <summary>True if the selected hex has an Operational, Player-owned Lab — where Plans can be designed.</summary>
        public bool CanDesignPlanAtSelectedTile()
        {
            if (SelectedTile == null)
                return false;

            Site site = SiteAt(SelectedTile.Coordinate);
            return site != null && site.Owner == TheatreFaction.Player && site.Type == SiteType.Lab && site.IsOperational;
        }

        /// <summary>
        /// Saves a new named Plan designed at the selected Lab. Fails (returns false,
        /// with a human-readable <paramref name="error"/>) if there's no eligible Lab
        /// selected, the name is blank, or a plan with that name already exists —
        /// names must be unique since they're how the player tells plans apart in
        /// both the Lab and Factory panels.
        /// </summary>
        public bool TryCreatePlan(string name, UnitCategory category, out string error)
        {
            if (!CanDesignPlanAtSelectedTile())
            {
                error = "Select an operational Lab you own first.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Plans need a name.";
                return false;
            }

            if (PlayerPlans.Any(p => p.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase)))
            {
                error = $"A plan named '{name}' already exists.";
                return false;
            }

            Color accent = PlanAccentPalette[PlayerPlans.Count % PlanAccentPalette.Length];
            PlayerPlans.Add(new DronePlan(name, category, accent));

            error = null;
            return true;
        }

        /// <summary>Placeholder turn-cost per unit category — open tuning numbers, same as everywhere else in this POC.</summary>
        public static int TurnsToProduce(UnitCategory category) => category switch
        {
            UnitCategory.Missile => 1,
            UnitCategory.Quadcopter => 2,
            UnitCategory.Hexacopter => 3,
            _ => 2,
        };

        /// <summary>The selected hex's site, if it's an Operational, Player-owned Factory — where Plans can be queued for production.</summary>
        public Site SelectedFactory()
        {
            if (SelectedTile == null)
                return null;

            Site site = SiteAt(SelectedTile.Coordinate);
            return site != null && site.Owner == TheatreFaction.Player && site.Type == SiteType.Factory && site.IsOperational ? site : null;
        }

        /// <summary>True if a Plan's category counts as missile storage (vs. drone storage) for capacity purposes.</summary>
        private static bool IsMissile(DronePlan plan) => plan.Category == UnitCategory.Missile;

        /// <summary>How many units of the given category (drone or missile) are currently stored at <paramref name="site"/>.</summary>
        public int StorageUsed(Site site, bool missiles) =>
            _siteStorage.TryGetValue(site, out Dictionary<DronePlan, int> storage)
                ? storage.Where(kv => IsMissile(kv.Key) == missiles).Sum(kv => kv.Value)
                : 0;

        /// <summary>Read-only view of everything currently stored at <paramref name="site"/>, by Plan.</summary>
        public IReadOnlyDictionary<DronePlan, int> StorageAt(Site site) =>
            _siteStorage.TryGetValue(site, out Dictionary<DronePlan, int> storage) ? storage : EmptyStorage;

        private static readonly Dictionary<DronePlan, int> EmptyStorage = new Dictionary<DronePlan, int>();

        /// <summary>Total of a Plan currently stored across every Airfield/Warehouse (for display only — gameplay always reasons about a specific site's storage).</summary>
        public int TotalStored(DronePlan plan) => _siteStorage.Values.Sum(storage => storage.TryGetValue(plan, out int c) ? c : 0);

        /// <summary>Player-owned, Operational sites of a given type with room for at least one more unit of <paramref name="plan"/>'s category, optionally excluding one site (its own storage doesn't count as a destination for its own transfer).</summary>
        private List<Site> EligibleStorageSitesOfType(DronePlan plan, SiteType type, Site exclude = null)
        {
            bool missile = IsMissile(plan);
            return World.Sites.Where(s => s != exclude && s.Owner == TheatreFaction.Player && s.IsOperational && s.Type == type)
                .Where(s => StorageUsed(s, missile) < (missile ? SiteStorageCatalog.MissileCapacity(s.Type) : SiteStorageCatalog.DroneCapacity(s.Type)))
                .ToList();
        }

        /// <summary>
        /// Every Player-owned, Operational Warehouse with room for at least one
        /// more unit of <paramref name="plan"/>'s category — manufactured output
        /// always lands in a Warehouse first (never straight into an Airfield);
        /// see <see cref="TryBeginTransfer"/> for moving stock on from there.
        /// </summary>
        public List<Site> EligibleProductionDestinations(DronePlan plan) => EligibleStorageSitesOfType(plan, SiteType.Warehouse);

        /// <summary>Every other Player-owned, Operational, storage-capable site (Warehouse or Airfield) with room for at least one more unit of <paramref name="plan"/>'s category — valid targets for a transfer out of <paramref name="source"/>.</summary>
        public List<Site> EligibleTransferDestinations(Site source, DronePlan plan)
        {
            bool missile = IsMissile(plan);
            return World.Sites.Where(s => s != source && s.Owner == TheatreFaction.Player && s.IsOperational && SiteStorageCatalog.CanStore(s.Type))
                .Where(s => StorageUsed(s, missile) < (missile ? SiteStorageCatalog.MissileCapacity(s.Type) : SiteStorageCatalog.DroneCapacity(s.Type)))
                .ToList();
        }

        private bool TryAddToStorage(Site site, DronePlan plan, int amount)
        {
            bool missile = IsMissile(plan);
            int capacity = missile ? SiteStorageCatalog.MissileCapacity(site.Type) : SiteStorageCatalog.DroneCapacity(site.Type);
            if (StorageUsed(site, missile) + amount > capacity)
                return false;

            if (!_siteStorage.TryGetValue(site, out Dictionary<DronePlan, int> storage))
            {
                storage = new Dictionary<DronePlan, int>();
                _siteStorage[site] = storage;
            }

            storage.TryGetValue(plan, out int count);
            storage[plan] = count + amount;
            return true;
        }

        private bool TryRemoveFromStorage(Site site, DronePlan plan, int amount)
        {
            if (amount <= 0 || !_siteStorage.TryGetValue(site, out Dictionary<DronePlan, int> storage))
                return false;
            if (!storage.TryGetValue(plan, out int count) || count < amount)
                return false;

            int remaining = count - amount;
            if (remaining <= 0)
                storage.Remove(plan);
            else
                storage[plan] = remaining;
            return true;
        }

        /// <summary>Queues one unit of <paramref name="plan"/> for production at the selected Factory, to be delivered into <paramref name="destination"/>'s storage once complete. False (no-op) if there isn't an eligible Factory selected, or <paramref name="destination"/> isn't a Player-owned, Operational Warehouse with room right now (manufactured output always goes to a Warehouse first — see <see cref="TryBeginTransfer"/> to move it on from there).</summary>
        public bool TryQueueProduction(DronePlan plan, Site destination)
        {
            Site factory = SelectedFactory();
            if (factory == null)
                return false;

            if (destination == null || destination.Owner != TheatreFaction.Player || !destination.IsOperational || destination.Type != SiteType.Warehouse)
                return false;

            bool missile = IsMissile(plan);
            int capacity = missile ? SiteStorageCatalog.MissileCapacity(destination.Type) : SiteStorageCatalog.DroneCapacity(destination.Type);
            if (StorageUsed(destination, missile) >= capacity)
                return false;

            if (!_productionQueues.TryGetValue(factory, out List<ProductionOrder> queue))
            {
                queue = new List<ProductionOrder>();
                _productionQueues[factory] = queue;
            }

            queue.Add(new ProductionOrder(plan, TurnsToProduce(plan.Category), destination));
            return true;
        }

        /// <summary>Convenience overload: auto-picks the first Warehouse with room. False (no-op) if none currently has room.</summary>
        public bool TryQueueProduction(DronePlan plan)
        {
            List<Site> eligible = EligibleProductionDestinations(plan);
            return eligible.Count > 0 && TryQueueProduction(plan, eligible[0]);
        }

        /// <summary>All in-progress production orders at the given Factory (for UI display).</summary>
        public IReadOnlyList<ProductionOrder> ProductionQueueAt(Site factory) =>
            _productionQueues.TryGetValue(factory, out List<ProductionOrder> queue) ? queue : System.Array.Empty<ProductionOrder>();

        /// <summary>
        /// Ticks every in-progress order down by one turn; on reaching zero, tries
        /// to deliver it into its destination's storage. If the destination has no
        /// room right now (e.g. it filled up from other orders/deployments since
        /// this one was queued), the order stays in the queue at zero turns
        /// remaining and is retried again next turn rather than silently
        /// discarding the finished unit.
        /// </summary>
        private void TickProduction()
        {
            foreach (List<ProductionOrder> queue in _productionQueues.Values)
            {
                for (int i = queue.Count - 1; i >= 0; i--)
                {
                    ProductionOrder order = queue[i];
                    if (order.TurnsRemaining > 0)
                        order.TurnsRemaining--;

                    if (order.TurnsRemaining > 0)
                        continue;

                    if (TryAddToStorage(order.Destination, order.Plan, 1))
                        queue.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Begins moving the entirety of <paramref name="source"/>'s current stock
        /// of <paramref name="plan"/> to <paramref name="destination"/> — e.g.
        /// relocating a Warehouse's manufactured output out to an Airfield so it
        /// can be deployed into an army. The full amount is removed from
        /// <paramref name="source"/>'s storage immediately (so the player sees it
        /// leave right away); it only lands in <paramref name="destination"/>'s
        /// storage once the transfer completes (always exactly 1 turn later,
        /// regardless of distance — see <see cref="TransferOrder"/>). Fails
        /// (returns false, with a human-readable <paramref name="error"/>) if
        /// there's nothing to move, or the two sites aren't both Player-owned,
        /// Operational, storage-capable sites (with <paramref name="destination"/>
        /// having room right now).
        /// </summary>
        public bool TryBeginTransfer(Site source, Site destination, DronePlan plan, out string error)
        {
            if (source == null || destination == null || source == destination ||
                source.Owner != TheatreFaction.Player || destination.Owner != TheatreFaction.Player ||
                !source.IsOperational || !destination.IsOperational ||
                !SiteStorageCatalog.CanStore(source.Type) || !SiteStorageCatalog.CanStore(destination.Type))
            {
                error = "Select two different, operational, owned storage sites.";
                return false;
            }

            int amount = StorageAt(source).TryGetValue(plan, out int stored) ? stored : 0;
            if (amount <= 0)
            {
                error = $"Nothing to move — {source.Location} has no {plan.Name} stored.";
                return false;
            }

            bool missile = IsMissile(plan);
            int capacity = missile ? SiteStorageCatalog.MissileCapacity(destination.Type) : SiteStorageCatalog.DroneCapacity(destination.Type);
            if (StorageUsed(destination, missile) >= capacity)
            {
                error = $"{DescribeSite(destination)} has no room for {plan.Name}.";
                return false;
            }

            if (!TryRemoveFromStorage(source, plan, amount))
            {
                error = "Failed to remove stock from the source site.";
                return false;
            }

            _transferOrders.Add(new TransferOrder(source, destination, plan, amount, TransferTurns));
            error = null;
            return true;
        }

        /// <summary>Every in-progress transfer, regardless of source/destination — for UI display (e.g. "35 Missile in transit" cards).</summary>
        public IReadOnlyList<TransferOrder> TransferOrders => _transferOrders;

        /// <summary>
        /// Ticks every in-progress transfer down by one turn; on reaching zero,
        /// tries to deliver it into its destination's storage. If the destination
        /// has no room right now, the transfer stays in progress at zero turns
        /// remaining and is retried again next turn (same "awaiting storage space"
        /// pattern as <see cref="TickProduction"/>) rather than the shipment being
        /// silently lost.
        /// </summary>
        private void TickTransfers()
        {
            for (int i = _transferOrders.Count - 1; i >= 0; i--)
            {
                TransferOrder order = _transferOrders[i];
                if (order.TurnsRemaining > 0)
                    order.TurnsRemaining--;

                if (order.TurnsRemaining > 0)
                    continue;

                if (TryAddToStorage(order.Destination, order.Plan, order.Amount))
                    _transferOrders.RemoveAt(i);
            }
        }

        /// <summary>True if the selected hex has an Operational, Player-owned Airfield — where drones/missiles can be deployed into a new army.</summary>
        public bool CanDeployArmyAtSelectedTile()
        {
            if (SelectedTile == null)
                return false;

            Site site = SiteAt(SelectedTile.Coordinate);
            return site != null && site.Owner == TheatreFaction.Player && site.IsOperational && SiteStorageCatalog.CanDeployArmies(site.Type);
        }

        /// <summary>
        /// Deploys <paramref name="selection"/> (drone/missile counts by Plan, drawn
        /// from <paramref name="airfield"/>'s current storage) into a brand-new
        /// <see cref="Army"/> at that Airfield's hex, removing the deployed amounts
        /// from storage. Fails (returns false, with a human-readable
        /// <paramref name="error"/>) if the site isn't an eligible Airfield, nothing
        /// was selected, or the selection exceeds what's actually stored there.
        /// </summary>
        public bool TryDeployArmy(Site airfield, IReadOnlyDictionary<DronePlan, int> selection, out string error)
        {
            if (airfield == null || airfield.Owner != TheatreFaction.Player || !airfield.IsOperational || !SiteStorageCatalog.CanDeployArmies(airfield.Type))
            {
                error = "Select an operational, owned Airfield first.";
                return false;
            }

            if (selection == null || selection.Count == 0 || selection.Values.All(v => v <= 0))
            {
                error = "Select at least one drone or missile to deploy.";
                return false;
            }

            _siteStorage.TryGetValue(airfield, out Dictionary<DronePlan, int> storage);
            foreach (KeyValuePair<DronePlan, int> entry in selection)
            {
                if (entry.Value <= 0)
                    continue;

                int available = storage != null && storage.TryGetValue(entry.Key, out int a) ? a : 0;
                if (entry.Value > available)
                {
                    error = $"Not enough {entry.Key.Name} stored ({available} available).";
                    return false;
                }
            }

            var army = new Army(TheatreFaction.Player, airfield.Location);
            foreach (KeyValuePair<DronePlan, int> entry in selection)
            {
                if (entry.Value <= 0)
                    continue;

                storage[entry.Key] -= entry.Value;
                if (storage[entry.Key] <= 0)
                    storage.Remove(entry.Key);

                army.Composition[entry.Key] = entry.Value;
            }

            _armies.Add(army);
            CreateArmyMarker(army);
            error = null;
            return true;
        }

        /// <summary>
        /// Folds every unit (and accrued experience) from <paramref name="source"/>
        /// into <paramref name="target"/> and removes <paramref name="source"/> from
        /// the map — both must belong to the same faction and be standing on the
        /// same hex.
        /// </summary>
        public bool TryMergeArmies(Army source, Army target, out string error)
        {
            if (source == null || target == null || source == target)
            {
                error = "Select two different armies to merge.";
                return false;
            }
            if (source.Owner != target.Owner)
            {
                error = "Can only merge armies of the same faction.";
                return false;
            }
            if (!source.Location.Equals(target.Location))
            {
                error = "Armies must be on the same hex to merge.";
                return false;
            }

            foreach (KeyValuePair<DronePlan, int> unit in source.Composition.ToList())
                target.AddUnits(unit.Key, unit.Value);
            target.AddExperience(source.Experience);

            bool wasSelected = SelectedArmy == source;
            RemoveArmy(source);
            if (wasSelected)
                SelectArmyDirect(target);

            error = null;
            return true;
        }

        /// <summary>
        /// Moves <paramref name="amount"/> of <paramref name="plan"/> from
        /// <paramref name="from"/> to <paramref name="to"/> — both must belong to
        /// the same faction and be standing on the same hex, and <paramref name="from"/>
        /// must hold at least that many.
        /// </summary>
        public bool TryTransferUnits(Army from, Army to, DronePlan plan, int amount, out string error)
        {
            if (from == null || to == null || from == to)
            {
                error = "Select two different armies to transfer between.";
                return false;
            }
            if (from.Owner != to.Owner)
            {
                error = "Can only transfer units between armies of the same faction.";
                return false;
            }
            if (!from.Location.Equals(to.Location))
            {
                error = "Armies must be on the same hex to transfer units.";
                return false;
            }
            if (!from.TryRemoveUnits(plan, amount))
            {
                error = $"{from.Name} doesn't have {amount} {plan.Name} to send.";
                return false;
            }

            to.AddUnits(plan, amount);
            error = null;
            return true;
        }

        /// <summary>
        /// Draws units from an Operational, owned Airfield's storage directly into
        /// <paramref name="army"/> — restocking an existing army (e.g. after a
        /// fight burned through its missiles) without needing to disband and
        /// redeploy it. <paramref name="army"/> must be standing on that Airfield's
        /// hex.
        /// </summary>
        public bool TryRestockArmy(Army army, Site airfield, IReadOnlyDictionary<DronePlan, int> selection, out string error)
        {
            if (army == null || airfield == null || army.Owner != airfield.Owner || !army.Location.Equals(airfield.Location) ||
                !airfield.IsOperational || !SiteStorageCatalog.CanDeployArmies(airfield.Type))
            {
                error = "The army must be standing on an operational, owned Airfield to restock.";
                return false;
            }

            if (selection == null || selection.Count == 0 || selection.Values.All(v => v <= 0))
            {
                error = "Select at least one drone or missile to draw from storage.";
                return false;
            }

            _siteStorage.TryGetValue(airfield, out Dictionary<DronePlan, int> storage);
            foreach (KeyValuePair<DronePlan, int> entry in selection)
            {
                if (entry.Value <= 0)
                    continue;

                int available = storage != null && storage.TryGetValue(entry.Key, out int a) ? a : 0;
                if (entry.Value > available)
                {
                    error = $"Not enough {entry.Key.Name} stored ({available} available).";
                    return false;
                }
            }

            foreach (KeyValuePair<DronePlan, int> entry in selection)
            {
                if (entry.Value <= 0)
                    continue;

                storage[entry.Key] -= entry.Value;
                if (storage[entry.Key] <= 0)
                    storage.Remove(entry.Key);

                army.AddUnits(entry.Key, entry.Value);
            }

            error = null;
            return true;
        }

        /// <summary>
        /// The reverse of <see cref="TryRestockArmy"/>: returns units from an army
        /// back into storage at the Operational, owned Airfield it's standing on
        /// (respecting that Airfield's remaining capacity).
        /// </summary>
        public bool TryUnloadArmy(Army army, Site airfield, DronePlan plan, int amount, out string error)
        {
            if (army == null || airfield == null || army.Owner != airfield.Owner || !army.Location.Equals(airfield.Location) ||
                !airfield.IsOperational || !SiteStorageCatalog.CanDeployArmies(airfield.Type))
            {
                error = "The army must be standing on an operational, owned Airfield to unload.";
                return false;
            }

            if (!army.TryRemoveUnits(plan, amount))
            {
                error = $"{army.Name} doesn't have {amount} {plan.Name} to unload.";
                return false;
            }

            if (!TryAddToStorage(airfield, plan, amount))
            {
                army.AddUnits(plan, amount); // roll back — no room at the Airfield
                error = "The Airfield has no room for that many.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Moves <paramref name="army"/> one hex from its current location to the
        /// adjacent <paramref name="destination"/>. Resolves combat via
        /// <see cref="TheatreCombatResolver"/> if the destination holds a hostile
        /// army or site; otherwise moves freely and, if the hex wasn't already
        /// owned by this army's faction, captures it (same stand-in rule as
        /// <see cref="TryCaptureSelectedTile"/>). False (no-op) if the army has
        /// already moved this turn, the game isn't in progress, or the destination
        /// isn't actually adjacent/on the grid. Only Player armies can be moved in
        /// this POC (no enemy-side theatre AI exists yet).
        ///
        /// A losing attack never destroys the attacker outright: since neither a
        /// site nor a defending army has any active defensive fire of its own yet
        /// (see <see cref="TheatreCombatResolver"/>'s documented simplification),
        /// nothing actually shoots the attacker down on a loss — it simply fails to
        /// break through, holds its original position having expended its missiles,
        /// rather than vanishing from the map. Only a full win moves it onto the
        /// contested hex.
        /// </summary>
        public bool TryMoveArmy(Army army, HexCoordinate destination)
        {
            if (army == null || army.Owner != TheatreFaction.Player)
                return false;
            if (army.HasMovedThisTurn || Controller.Result != TheatreResult.InProgress)
                return false;
            if (HexCoordinate.Distance(army.Location, destination) != 1)
                return false;

            HexTile destTile = World.Grid.GetTile(destination);
            if (destTile == null)
                return false;

            army.HasMovedThisTurn = true;

            Army opposingArmy = _armies.FirstOrDefault(a => a != army && a.Owner != army.Owner && a.Location.Equals(destination));
            Site siteHere = SiteAt(destination);
            Site opposingSite = siteHere != null && siteHere.Owner != army.Owner ? siteHere : null;

            if (opposingArmy != null)
            {
                TheatreCombatResult result = TheatreCombatResolver.ResolveArmyVsArmy(army, opposingArmy);
                army.ConsumeMissiles(result.AttackerMissilesUsed);
                _combatFeedback = DescribeArmyVsArmyResult(army, result);

                if (opposingArmy.IsEmpty)
                    RemoveArmy(opposingArmy);

                if (result.Outcome == TheatreCombatOutcome.AttackerWin)
                    army.MoveTo(destination);
                // DefenderWin: the attacker failed to break through but wasn't shot
                // down either — it holds its original position (see method summary).
            }
            else if (opposingSite != null)
            {
                TheatreCombatResult result = TheatreCombatResolver.ResolveArmyVsSite(army, opposingSite);
                army.ConsumeMissiles(result.AttackerMissilesUsed);
                _combatFeedback = DescribeArmyVsSiteResult(army, opposingSite, result);

                if (result.Outcome == TheatreCombatOutcome.AttackerWin)
                    army.MoveTo(destination);
                // DefenderWin: the site absorbed the attack but has no active
                // defenses to destroy the attacker — it holds its original position.
            }
            else
            {
                army.MoveTo(destination);
                if (destTile.Owner != army.Owner)
                    destTile.Owner = army.Owner;
            }

            // A pure-missile army (no drones) that expends its last missiles has
            // nothing left at all and disappears — the one case an army can still
            // vanish, since there's genuinely nothing left to represent.
            if (_armies.Contains(army) && army.IsEmpty)
                RemoveArmy(army);

            RefreshAllViews();
            return true;
        }

        private static string DescribeArmyVsArmyResult(Army attacker, TheatreCombatResult result) =>
            result.Outcome == TheatreCombatOutcome.AttackerWin
                ? $"{attacker.Name} destroyed the opposing army (expended {result.AttackerMissilesUsed} missile(s))."
                : $"{attacker.Name} failed to break through and holds position (expended {result.AttackerMissilesUsed} missile(s)).";

        private static string DescribeArmyVsSiteResult(Army attacker, Site site, TheatreCombatResult result) =>
            result.Outcome == TheatreCombatOutcome.AttackerWin
                ? $"{attacker.Name} damaged/destroyed the {site.Owner} {SiteBuildCatalog.DisplayName(site.Type)} (expended {result.AttackerMissilesUsed} missile(s))."
                : $"{attacker.Name} ran out of missiles attacking the {site.Owner} {SiteBuildCatalog.DisplayName(site.Type)} and pulled back (expended {result.AttackerMissilesUsed} missile(s)); the site took some damage.";

        private void RemoveArmy(Army army)
        {
            _armies.Remove(army);
            ArmyMarkerView view = _armyViews.FirstOrDefault(v => v != null && v.Army == army);
            if (view != null)
            {
                _armyViews.Remove(view);
                DestroyImmediate(view.gameObject);
            }

            if (SelectedArmy == army)
                SelectedArmy = null;
        }

        // Hovers above any site marker (tallest is the Factory at 0.9) so an army
        // always reads as a distinct, always-visible flying unit rather than
        // sitting on/inside a building.
        private const float ArmyHoverHeight = 1.1f;

        private Vector3 ArmyWorldPosition(Army army)
        {
            HexTile tile = World.Grid.GetTile(army.Location);
            float tileHeight = tile != null && tile.Terrain == TerrainType.Mountain ? MountainHeight : HexHeight;
            Vector3 worldPos = HexMeshFactory.AxialToWorld(army.Location, HexRadius);
            worldPos.y = tileHeight + ArmyHoverHeight;
            return worldPos;
        }

        /// <summary>Which drone silhouette best represents an army's composition — whichever category (quad/hex) it holds more of, defaulting to quadcopter (e.g. for a missile-only army, an edge case this POC allows).</summary>
        private static Vanquish.Combat.Play.DroneRotorConfiguration RotorConfigurationFor(Army army)
        {
            int quad = army.Composition.Where(kv => kv.Key.Category == UnitCategory.Quadcopter).Sum(kv => kv.Value);
            int hex = army.Composition.Where(kv => kv.Key.Category == UnitCategory.Hexacopter).Sum(kv => kv.Value);
            return hex > quad ? Vanquish.Combat.Play.DroneRotorConfiguration.Hexacopter : Vanquish.Combat.Play.DroneRotorConfiguration.Quadcopter;
        }

        /// <summary>
        /// Builds the always-visible marker for a newly deployed/loaded army: a
        /// small procedural flying drone (<see cref="Vanquish.Combat.Play.DroneVisualBuilder"/>
        /// — the same builder combat-instance drones and Lab/Factory Plan previews
        /// use) rather than an abstract token, plus a thin status disc underneath
        /// that's the one piece re-tinted at runtime (owner color, dimmed when not
        /// combat effective — see <see cref="ArmyMarkerView"/>), plus a
        /// <see cref="SphereCollider"/> so the whole marker is directly clickable
        /// (see <see cref="HandleWorldClick"/>) since the drone visual's own child
        /// pieces are deliberately collider-free.
        /// </summary>
        private void CreateArmyMarker(Army army)
        {
            Color ownerColor = army.Owner switch
            {
                TheatreFaction.Player => new Color(0.35f, 0.65f, 1f),
                TheatreFaction.Enemy => new Color(1f, 0.35f, 0.35f),
                _ => Color.gray,
            };

            GameObject go = new GameObject($"Army_{army.Owner}_{army.Id}_{army.Name}");
            go.transform.SetParent(transform, worldPositionStays: true);
            go.transform.position = ArmyWorldPosition(army);

            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "StatusDisc";
            disc.transform.SetParent(go.transform, false);
            disc.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            disc.transform.localScale = new Vector3(0.55f, 0.015f, 0.55f);
            UnityEngine.Object.Destroy(disc.GetComponent<Collider>());
            Renderer discRenderer = disc.GetComponent<Renderer>();

            // Larger, bright ring beneath the status disc — hidden by default,
            // shown only while this army is selected (see ArmyMarkerView.SetSelected)
            // as a clear "this is the one you've got open" cue on the marker itself.
            GameObject selectionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            selectionRing.name = "SelectionRing";
            selectionRing.transform.SetParent(go.transform, false);
            selectionRing.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            selectionRing.transform.localScale = new Vector3(0.85f, 0.008f, 0.85f);
            UnityEngine.Object.Destroy(selectionRing.GetComponent<Collider>());
            Renderer selectionRingRenderer = selectionRing.GetComponent<Renderer>();
            if (selectionRingRenderer != null)
                selectionRingRenderer.material.color = new Color(1f, 0.95f, 0.4f);

            Vanquish.Combat.Play.DroneVisualBuilder.Build(go.transform, ownerColor, RotorConfigurationFor(army), out _, armLength: 0.3f, hardpointCount: 0);

            var collider = go.AddComponent<SphereCollider>();
            collider.radius = 0.5f;

            var marker = go.AddComponent<ArmyMarkerView>();
            marker.Initialize(army, discRenderer, selectionRing);
            _armyViews.Add(marker);
        }

        private void RefreshArmyViews()
        {
            foreach (ArmyMarkerView view in _armyViews)
            {
                if (view == null)
                    continue;

                view.transform.position = ArmyWorldPosition(view.Army);
                view.Refresh();
            }
        }

        public void AdvanceTurn()
        {
            Controller.AdvanceTurn();
            TickProduction();
            TickTransfers();
            foreach (Army army in _armies)
                army.HasMovedThisTurn = false;
            RefreshAllViews();
        }

        private void RefreshAllViews()
        {
            foreach (HexTileView view in _tileViews.Values)
                view.Refresh();
            foreach (SiteMarkerView marker in _siteViews)
                marker.Refresh();
            RefreshArmyViews();
            UpdateMoveHighlights();
        }

        /// <summary>
        /// Captures and writes the full theatre-map state: tile ownership, which
        /// buildings are on which tile, weapon (Plan) designs, weapon (Plan)
        /// inventory, the turn counter and result, the per-faction resource pool,
        /// in-progress Factory production orders, and the territorial-control
        /// sustain counters — every piece of persistent player-facing state this
        /// harness currently tracks. Camera position/selection are deliberately not
        /// saved (transient UI state, not player progress).
        /// </summary>
        public void SaveGame()
        {
            var data = new SaveData
            {
                currentTurn = Controller.CurrentTurn,
                result = Controller.Result.ToString(),
                playerResource = Controller.ResourcePool[TheatreFaction.Player],
                enemyResource = Controller.ResourcePool[TheatreFaction.Enemy],
                playerResearch = Controller.ResearchPool[TheatreFaction.Player],
                enemyResearch = Controller.ResearchPool[TheatreFaction.Enemy],
                playerTerritorialSustainTurns = _territorialCondition.ConsecutiveTurnsMet(TheatreFaction.Player),
                enemyTerritorialSustainTurns = _territorialCondition.ConsecutiveTurnsMet(TheatreFaction.Enemy),
            };

            foreach (string techId in _unlockedTechIds)
                data.unlockedTechNodeIds.Add(techId);

            foreach (HexTile tile in World.Grid.Tiles)
            {
                data.hexOwnership.Add(new SavedHexOwnership
                {
                    q = tile.Coordinate.Q,
                    r = tile.Coordinate.R,
                    owner = tile.Owner.ToString(),
                });
            }

            foreach (Site site in World.Sites)
            {
                if (site.State == SiteState.Destroyed)
                    continue;

                data.sites.Add(new SavedSite
                {
                    type = site.Type.ToString(),
                    owner = site.Owner.ToString(),
                    q = site.Location.Q,
                    r = site.Location.R,
                    state = site.State.ToString(),
                    turnsRemaining = site.TurnsRemaining,
                    healthFraction01 = site.HealthFraction01,
                });
            }

            foreach (DronePlan plan in PlayerPlans)
                data.plans.Add(new SavedPlan { name = plan.Name, category = plan.Category.ToString() });

            foreach (KeyValuePair<Site, List<ProductionOrder>> queueEntry in _productionQueues)
            {
                foreach (ProductionOrder order in queueEntry.Value)
                {
                    data.productionOrders.Add(new SavedProductionOrder
                    {
                        factoryQ = queueEntry.Key.Location.Q,
                        factoryR = queueEntry.Key.Location.R,
                        planName = order.Plan.Name,
                        turnsRemaining = order.TurnsRemaining,
                        destinationQ = order.Destination.Location.Q,
                        destinationR = order.Destination.Location.R,
                    });
                }
            }

            foreach (KeyValuePair<Site, Dictionary<DronePlan, int>> storageEntry in _siteStorage)
            {
                if (storageEntry.Key.State == SiteState.Destroyed)
                    continue;

                foreach (KeyValuePair<DronePlan, int> unit in storageEntry.Value)
                {
                    data.siteStorage.Add(new SavedSiteStorage
                    {
                        siteQ = storageEntry.Key.Location.Q,
                        siteR = storageEntry.Key.Location.R,
                        planName = unit.Key.Name,
                        count = unit.Value,
                    });
                }
            }

            foreach (Army army in _armies)
            {
                var savedArmy = new SavedArmy
                {
                    id = army.Id,
                    owner = army.Owner.ToString(),
                    q = army.Location.Q,
                    r = army.Location.R,
                    hasMovedThisTurn = army.HasMovedThisTurn,
                    name = army.Name,
                    experience = army.Experience,
                };

                foreach (KeyValuePair<DronePlan, int> unit in army.Composition)
                    savedArmy.units.Add(new SavedArmyUnit { planName = unit.Key.Name, count = unit.Value });

                data.armies.Add(savedArmy);
            }

            foreach (TransferOrder order in _transferOrders)
            {
                data.transferOrders.Add(new SavedTransferOrder
                {
                    sourceQ = order.Source.Location.Q,
                    sourceR = order.Source.Location.R,
                    destinationQ = order.Destination.Location.Q,
                    destinationR = order.Destination.Location.R,
                    planName = order.Plan.Name,
                    amount = order.Amount,
                    turnsRemaining = order.TurnsRemaining,
                });
            }

            SaveSystem.Save(data);
        }

        /// <summary>Loads and applies a save, if one exists. Returns false (no-op) if there's no save file.</summary>
        public bool LoadGame()
        {
            if (!SaveSystem.HasSave())
                return false;

            ApplySaveData(SaveSystem.Load());
            return true;
        }

        private void ApplySaveData(SaveData data)
        {
            foreach (SavedHexOwnership saved in data.hexOwnership)
            {
                HexTile tile = World.Grid.GetTile(new HexCoordinate(saved.q, saved.r));
                if (tile != null && Enum.TryParse(saved.owner, out TheatreFaction owner))
                    tile.Owner = owner;
            }

            foreach (SiteMarkerView marker in _siteViews)
            {
                if (marker != null)
                    DestroyImmediate(marker.gameObject);
            }
            _siteViews.Clear();
            World.Sites.Clear();
            _productionQueues.Clear();
            _siteStorage.Clear();
            _transferOrders.Clear();
            CancelTransferPicking();

            foreach (ArmyMarkerView armyView in _armyViews)
            {
                if (armyView != null)
                    DestroyImmediate(armyView.gameObject);
            }
            _armyViews.Clear();
            _armies.Clear();
            SelectedArmy = null;
            _pendingDeployment.Clear();
            _combatFeedback = null;

            foreach (SavedSite saved in data.sites)
            {
                if (!Enum.TryParse(saved.type, out SiteType type) || !Enum.TryParse(saved.owner, out TheatreFaction owner) || !Enum.TryParse(saved.state, out SiteState state))
                    continue;

                Site site = Site.Restore(type, owner, new HexCoordinate(saved.q, saved.r), state, saved.turnsRemaining, saved.healthFraction01);
                World.Sites.Add(site);
                CreateSiteMarker(site);
            }

            PlayerPlans.Clear();
            foreach (SavedPlan saved in data.plans)
            {
                if (!Enum.TryParse(saved.category, out UnitCategory category))
                    continue;

                Color accent = PlanAccentPalette[PlayerPlans.Count % PlanAccentPalette.Length];
                PlayerPlans.Add(new DronePlan(saved.name, category, accent));
            }

            foreach (SavedSiteStorage saved in data.siteStorage)
            {
                Site site = World.Sites.FirstOrDefault(s => s.Location.Equals(new HexCoordinate(saved.siteQ, saved.siteR)));
                DronePlan plan = PlayerPlans.FirstOrDefault(p => p.Name == saved.planName);
                if (site == null || plan == null)
                    continue;

                if (!_siteStorage.TryGetValue(site, out Dictionary<DronePlan, int> storage))
                {
                    storage = new Dictionary<DronePlan, int>();
                    _siteStorage[site] = storage;
                }
                storage[plan] = saved.count;
            }

            foreach (SavedProductionOrder saved in data.productionOrders)
            {
                Site factory = World.Sites.FirstOrDefault(s => s.Type == SiteType.Factory && s.Location.Equals(new HexCoordinate(saved.factoryQ, saved.factoryR)));
                Site destination = World.Sites.FirstOrDefault(s => s.Location.Equals(new HexCoordinate(saved.destinationQ, saved.destinationR)));
                DronePlan plan = PlayerPlans.FirstOrDefault(p => p.Name == saved.planName);
                if (factory == null || destination == null || plan == null)
                    continue;

                if (!_productionQueues.TryGetValue(factory, out List<ProductionOrder> queue))
                {
                    queue = new List<ProductionOrder>();
                    _productionQueues[factory] = queue;
                }
                queue.Add(new ProductionOrder(plan, saved.turnsRemaining, destination));
            }

            foreach (SavedTransferOrder saved in data.transferOrders)
            {
                Site source = World.Sites.FirstOrDefault(s => s.Location.Equals(new HexCoordinate(saved.sourceQ, saved.sourceR)));
                Site destination = World.Sites.FirstOrDefault(s => s.Location.Equals(new HexCoordinate(saved.destinationQ, saved.destinationR)));
                DronePlan plan = PlayerPlans.FirstOrDefault(p => p.Name == saved.planName);
                if (source == null || destination == null || plan == null)
                    continue;

                _transferOrders.Add(new TransferOrder(source, destination, plan, saved.amount, saved.turnsRemaining));
            }

            foreach (SavedArmy saved in data.armies)
            {
                if (!Enum.TryParse(saved.owner, out TheatreFaction owner))
                    continue;

                var army = new Army(saved.id, owner, new HexCoordinate(saved.q, saved.r), saved.name) { HasMovedThisTurn = saved.hasMovedThisTurn };
                army.SetExperienceForRestore(saved.experience);
                foreach (SavedArmyUnit unit in saved.units)
                {
                    DronePlan plan = PlayerPlans.FirstOrDefault(p => p.Name == unit.planName);
                    if (plan != null)
                        army.Composition[plan] = unit.count;
                }

                _armies.Add(army);
                CreateArmyMarker(army);
            }

            Enum.TryParse(data.result, out TheatreResult result);
            Controller.RestoreProgress(data.currentTurn, result);
            Controller.ResourcePool[TheatreFaction.Player] = data.playerResource;
            Controller.ResourcePool[TheatreFaction.Enemy] = data.enemyResource;
            Controller.ResearchPool[TheatreFaction.Player] = data.playerResearch;
            Controller.ResearchPool[TheatreFaction.Enemy] = data.enemyResearch;

            _unlockedTechIds.Clear();
            foreach (string techId in data.unlockedTechNodeIds)
                _unlockedTechIds.Add(techId);
            // Defensive: older saves predating the tech tree won't have this in their
            // unlockedTechNodeIds list, but the base Quadcopter airframe should always
            // be available regardless of when the save was made.
            _unlockedTechIds.Add(TheatreTechCatalog.DefaultUnlockedId);

            _territorialCondition.RestoreConsecutiveTurns(TheatreFaction.Player, data.playerTerritorialSustainTurns);
            _territorialCondition.RestoreConsecutiveTurns(TheatreFaction.Enemy, data.enemyTerritorialSustainTurns);

            SelectedTile = null;
            RefreshAllViews();
        }

        /// <summary>Tears down the whole map and rebuilds it fresh — the "New Game" menu action.</summary>
        public void NewGame()
        {
            var children = new List<GameObject>();
            foreach (Transform child in transform)
                children.Add(child.gameObject);
            foreach (GameObject child in children)
                DestroyImmediate(child);

            _tileViews.Clear();
            _siteViews.Clear();
            _productionQueues.Clear();
            _siteStorage.Clear();
            _transferOrders.Clear();
            _transferSourceSite = null;
            _transferPlan = null;
            _armies.Clear();
            _armyViews.Clear();
            _pendingDeployment.Clear();
            SelectedArmy = null;
            _combatFeedback = null;
            Army.ResetIdCounterForNewGame();
            PlanIconRenderer.ClearCache();
            PlayerPlans.Clear();
            _unlockedTechIds.Clear();
            SelectedTile = null;

            Build();
        }

        /// <summary>
        /// Two-part UI, per design feedback: a lightweight hover tooltip for
        /// "what is this" at a glance (no click needed), and a bottom action bar
        /// that only shows relevant info/actions for whatever is currently
        /// selected — replacing the old always-open, everything-at-once side panel.
        /// </summary>
        private void OnGUI()
        {
            DrawArmyNameTags();
            DrawCompass();
            DrawHoverTooltip();
            DrawBottomBar();
        }

        // ---- Hover tooltip -------------------------------------------------

        /// <summary>
        /// A small tooltip that follows the mouse, showing only the basics for
        /// whatever's currently hovered (see <see cref="UpdateHover"/>) — no click
        /// required. Sites hint at what clicking them opens; armies just show
        /// name/rank, matching the reference mockup's plain hover text.
        /// </summary>
        private void DrawHoverTooltip()
        {
            string text;
            if (_hoveredArmy != null)
            {
                text = $"{_hoveredArmy.Name} ({_hoveredArmy.Rank})";
            }
            else if (_hoveredTile != null)
            {
                Site site = SiteAt(_hoveredTile.Coordinate);
                if (site != null)
                {
                    text = $"{SiteBuildCatalog.DisplayName(site.Type).ToUpperInvariant()} ({site.Owner})";
                    if (site.Owner == TheatreFaction.Player && site.IsOperational)
                    {
                        if (site.Type == SiteType.Factory)
                            text += "\nClick to manage production";
                        else if (site.Type == SiteType.Lab)
                            text += "\nClick to design plans";
                        else if (SiteStorageCatalog.CanDeployArmies(site.Type))
                            text += "\nClick to deploy/manage armies";
                        else if (SiteStorageCatalog.CanStore(site.Type))
                            text += "\nClick to view storage";
                    }
                }
                else
                {
                    text = $"{_hoveredTile.Terrain}, {_hoveredTile.Owner}";
                }
            }
            else
            {
                return;
            }

            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                padding = new RectOffset(8, 8, 6, 6),
            };

            const float width = 190f;
            Vector2 mouse = Event.current.mousePosition;
            float height = style.CalcHeight(new GUIContent(text), width);
            GUI.Box(new Rect(mouse.x + 18f, mouse.y + 18f, width, height), text, style);
        }

        // ---- Bottom action bar ---------------------------------------------

        private const float TurnPanelWidth = 210f;
        private Vector2 _bottomBarScroll;

        /// <summary>
        /// Fixed-height bar spanning the bottom of the screen: turn/resources/End
        /// Turn always live on the right (see <see cref="DrawTurnPanel"/>); the
        /// rest is whatever's relevant to the current selection (see
        /// <see cref="DrawSelectionContent"/>).
        /// </summary>
        private void DrawBottomBar()
        {
            var barRect = new Rect(0f, Screen.height - BottomBarHeight, Screen.width, BottomBarHeight);
            GUI.Box(barRect, GUIContent.none);

            GUILayout.BeginArea(barRect);
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(Mathf.Max(100f, Screen.width - TurnPanelWidth - 24f)));
            DrawSelectionContent();
            GUILayout.EndVertical();

            GUILayout.Space(8f);

            GUILayout.BeginVertical(GUILayout.Width(TurnPanelWidth));
            DrawTurnPanel();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawTurnPanel()
        {
            GUILayout.Label($"Turn {Controller.CurrentTurn} — {Controller.Result}", Bold());
            GUILayout.Label($"Resources — P:{Controller.ResourcePool[TheatreFaction.Player]} E:{Controller.ResourcePool[TheatreFaction.Enemy]}");
            GUILayout.Label($"Research — P:{Controller.ResearchPool[TheatreFaction.Player]} E:{Controller.ResearchPool[TheatreFaction.Enemy]}");
            GUILayout.Label($"Territory — P:{World.Grid.OwnershipFraction(TheatreFaction.Player):P0} E:{World.Grid.OwnershipFraction(TheatreFaction.Enemy):P0}");

            GUILayout.FlexibleSpace();
            GUI.enabled = Controller.Result == TheatreResult.InProgress;
            if (GUILayout.Button("END TURN >>", GUILayout.Height(44)))
                AdvanceTurn();
            GUI.enabled = true;
        }

        /// <summary>Dispatches to whichever context view applies: a selected army takes priority, then a selected hex (its site's cards, or the build menu if empty).</summary>
        private void DrawSelectionContent()
        {
            if (SelectedArmy != null)
            {
                DrawArmyBar(SelectedArmy);
                return;
            }

            if (SelectedTile == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Hover or click a hex, building, or army for details.", Italic());
                GUILayout.FlexibleSpace();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{SelectedTile.Coordinate} — {SelectedTile.Terrain}, {SelectedTile.Owner}", Bold());
            GUI.enabled = CanCaptureSelectedTile() && Controller.Result == TheatreResult.InProgress;
            if (GUILayout.Button("Capture", GUILayout.Width(80)))
                TryCaptureSelectedTile();
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            if (IsPickingTransferDestination)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Moving {_transferPlan.Name} — click a highlighted hex to send it there.", Italic());
                if (GUILayout.Button("Cancel", GUILayout.Width(60)))
                    CancelTransferPicking();
                GUILayout.EndHorizontal();
            }

            Site site = SiteAt(SelectedTile.Coordinate);

            _bottomBarScroll = GUILayout.BeginScrollView(_bottomBarScroll, GUILayout.ExpandHeight(true));
            GUILayout.BeginHorizontal();

            if (site != null)
                DrawSiteCards(site);
            else
                DrawBuildCards();

            foreach (Army army in ArmiesAt(SelectedTile.Coordinate))
                DrawArmyHereCard(army);

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        // ---- Card helpers ---------------------------------------------------

        private const float CardHeight = 190f;

        private static void BeginCard(float width)
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(width), GUILayout.Height(CardHeight));
        }

        private static void EndCard() => GUILayout.EndVertical();

        private const float PlanIconSize = 100f;

        /// <summary>A small rendered preview icon for a Plan (see <see cref="PlanIconRenderer"/>) — shown directly inside its card instead of a separate 3D model floating next to the building in world space.</summary>
        private static void DrawPlanIcon(DronePlan plan)
        {
            Texture2D icon = PlanIconRenderer.GetOrCreateIcon(plan);
            GUILayout.Label(icon, GUILayout.Width(PlanIconSize), GUILayout.Height(PlanIconSize));
        }

        /// <summary>A row of chevrons (one per rank tier — Private=1, Corporal=2, etc.) plus the rank name, standing in for a real insignia sprite.</summary>
        private static void DrawRankBadge(ArmyRank rank)
        {
            int chevronCount = (int)rank + 1;
            var style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            style.normal.textColor = new Color(1f, 0.82f, 0.25f);
            GUILayout.Label(new string('^', chevronCount) + " " + rank, style, GUILayout.ExpandWidth(false));
        }

        // ---- Empty-hex build cards ------------------------------------------

        private void DrawBuildCards()
        {
            if (SelectedTile.Owner != TheatreFaction.Player)
            {
                BeginCard(170f);
                GUILayout.Label("HEX", Bold());
                GUILayout.Label($"{SelectedTile.Owner} territory", Italic());
                EndCard();
                return;
            }

            if (!IsTerrainBuildable(SelectedTile.Terrain))
            {
                BeginCard(170f);
                GUILayout.Label("HEX", Bold());
                GUILayout.Label($"{SelectedTile.Terrain} — not buildable", Italic());
                EndCard();
                return;
            }

            bool canStartConstruction = Controller.Result == TheatreResult.InProgress;
            foreach (SiteBuildOption option in SiteBuildCatalog.Options)
            {
                BeginCard(140f);
                GUILayout.Label(option.DisplayName.ToUpperInvariant(), Bold());
                GUILayout.Label($"{option.TurnsToBuild} turn(s)");
                GUILayout.FlexibleSpace();
                GUI.enabled = canStartConstruction;
                if (GUILayout.Button("Build"))
                    TryBeginConstructionOnSelectedTile(option.Type, option.TurnsToBuild);
                GUI.enabled = true;
                EndCard();
            }
        }

        // ---- Site cards ------------------------------------------------------

        private const int RepairTurns = 2;

        private void DrawSiteCards(Site site)
        {
            BeginCard(170f);
            GUILayout.Label(SiteBuildCatalog.DisplayName(site.Type).ToUpperInvariant(), Bold());
            GUILayout.Label($"{site.Owner}");
            if (site.State != SiteState.Operational)
            {
                GUILayout.Label($"{site.State} — {site.TurnsRemaining}t", Italic());
            }
            else
            {
                GUILayout.Label($"Health {site.HealthFraction01:P0}");
                if (site.Owner == TheatreFaction.Player && site.HealthFraction01 < 1f)
                {
                    if (HasSupplyLineToBase(site))
                    {
                        if (GUILayout.Button($"Repair ({RepairTurns}t)") && site.BeginRepair(RepairTurns))
                            RefreshAllViews();
                    }
                    else
                    {
                        GUILayout.Label("Repair needs a supply line to a Base", Italic());
                    }
                }
            }
            EndCard();

            if (site.Owner != TheatreFaction.Player || !site.IsOperational)
                return;

            if (site.Type == SiteType.Lab)
                DrawLabCards();
            else if (site.Type == SiteType.Factory)
                DrawFactoryCards(site);
            else if (SiteStorageCatalog.CanDeployArmies(site.Type))
                DrawAirfieldCards(site);
            else if (SiteStorageCatalog.CanStore(site.Type))
                DrawWarehouseCards(site);
        }

        private void DrawLabCards()
        {
            BeginCard(190f);
            GUILayout.Label("DESIGN PLAN", Bold());
            _planNameInput = GUILayout.TextField(_planNameInput);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Quad", GUILayout.Width(48)))
                SubmitPlan(UnitCategory.Quadcopter);
            if (GUILayout.Button("Hex", GUILayout.Width(42)))
                SubmitPlan(UnitCategory.Hexacopter);
            if (GUILayout.Button("Msl", GUILayout.Width(42)))
                SubmitPlan(UnitCategory.Missile);
            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(_planFeedback))
                GUILayout.Label(_planFeedback, Italic());
            EndCard();

            BeginCard(150f);
            GUILayout.Label("TECH TREE", Bold());
            GUILayout.Label($"Research: {Controller.ResearchPool[TheatreFaction.Player]}");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open"))
                _techTree.Open();
            EndCard();

            foreach (DronePlan plan in PlayerPlans)
            {
                BeginCard(150f);
                GUILayout.Label(plan.Name, Bold());
                GUILayout.Label(plan.Category.ToString());
                DrawPlanIcon(plan);
                EndCard();
            }
        }

        private void SubmitPlan(UnitCategory category)
        {
            bool ok = TryCreatePlan(_planNameInput, category, out string error);
            _planFeedback = ok ? $"Saved '{_planNameInput}' ({category})." : error;
            if (ok)
                _planNameInput = "";
        }

        private static string DescribeSite(Site site) => $"{SiteBuildCatalog.DisplayName(site.Type)} {site.Location}";

        private void DrawFactoryCards(Site factory)
        {
            if (PlayerPlans.Count == 0)
            {
                BeginCard(210f);
                GUILayout.Label("FACTORY", Bold());
                GUILayout.Label("No plans designed — build a Lab first.", Italic());
                EndCard();
                return;
            }

            foreach (DronePlan plan in PlayerPlans)
            {
                BeginCard(160f);
                GUILayout.Label(plan.Name, Bold());
                GUILayout.Label($"{plan.Category} — {TurnsToProduce(plan.Category)}t");
                DrawPlanIcon(plan);

                List<Site> destinations = EligibleProductionDestinations(plan);
                if (destinations.Count == 0)
                {
                    GUILayout.Label("No Warehouse room", Italic());
                }
                else
                {
                    GUILayout.Label($"-> {DescribeSite(destinations[0])}", Italic());
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Build"))
                        TryQueueProduction(plan, destinations[0]);
                }
                EndCard();
            }

            // Stack same Plan+destination orders into one card (e.g. queuing 6
            // Missiles used to draw 6 near-identical cards) — shows the count and
            // how long until the next one in the stack completes, rather than one
            // card per individual order.
            var queueGroups = ProductionQueueAt(factory).GroupBy(o => (o.Plan, o.Destination));
            foreach (var group in queueGroups)
            {
                List<ProductionOrder> orders = group.ToList();
                int awaitingCount = orders.Count(o => o.TurnsRemaining <= 0);
                int minTurnsRemaining = orders.Where(o => o.TurnsRemaining > 0).Select(o => o.TurnsRemaining).DefaultIfEmpty(0).Min();

                BeginCard(150f);
                GUILayout.Label($"{group.Key.Plan.Name} x{orders.Count}", Bold());
                if (awaitingCount == orders.Count)
                    GUILayout.Label("awaiting storage", Italic());
                else if (awaitingCount > 0)
                    GUILayout.Label($"next in {minTurnsRemaining}t ({awaitingCount} awaiting storage)", Italic());
                else
                    GUILayout.Label($"next in {minTurnsRemaining}t", Italic());
                GUILayout.Label($"-> {DescribeSite(group.Key.Destination)}");
                EndCard();
            }
        }

        private void DrawWarehouseCards(Site warehouse)
        {
            BeginCard(170f);
            GUILayout.Label("WAREHOUSE", Bold());
            GUILayout.Label($"Drones {StorageUsed(warehouse, false)}/{SiteStorageCatalog.DroneCapacity(warehouse.Type)}");
            GUILayout.Label($"Missiles {StorageUsed(warehouse, true)}/{SiteStorageCatalog.MissileCapacity(warehouse.Type)}");
            EndCard();

            foreach (KeyValuePair<DronePlan, int> kv in StorageAt(warehouse).ToList())
            {
                BeginCard(150f);
                GUILayout.Label(kv.Key.Name, Bold());
                GUILayout.Label($"{kv.Key.Category} x{kv.Value}");
                DrawPlanIcon(kv.Key);
                GUILayout.FlexibleSpace();
                DrawMoveButton(warehouse, kv.Key);
                EndCard();
            }

            DrawTransferCardsFor(warehouse);
        }

        private void DrawAirfieldCards(Site airfield)
        {
            BeginCard(170f);
            GUILayout.Label("AIRFIELD", Bold());
            GUILayout.Label($"Drones {StorageUsed(airfield, false)}/{SiteStorageCatalog.DroneCapacity(airfield.Type)}");
            GUILayout.Label($"Missiles {StorageUsed(airfield, true)}/{SiteStorageCatalog.MissileCapacity(airfield.Type)}");
            EndCard();

            IReadOnlyDictionary<DronePlan, int> storage = StorageAt(airfield);
            foreach (KeyValuePair<DronePlan, int> kv in storage.ToList())
            {
                BeginCard(160f);
                GUILayout.Label(kv.Key.Name, Bold());
                GUILayout.Label($"{kv.Key.Category} — {kv.Value} stored");
                DrawPlanIcon(kv.Key);

                _pendingDeployment.TryGetValue(kv.Key, out int pending);
                GUILayout.Label(pending > 0 ? $"Staged to deploy: {pending}" : "Staged to deploy: none", Italic());

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("-", GUILayout.Width(22)))
                    _pendingDeployment[kv.Key] = Mathf.Max(0, pending - 1);
                GUILayout.Label(pending.ToString(), GUILayout.Width(22));
                if (GUILayout.Button("+", GUILayout.Width(22)))
                    _pendingDeployment[kv.Key] = Mathf.Min(kv.Value, pending + 1);
                if (GUILayout.Button("All", GUILayout.Width(32)))
                    _pendingDeployment[kv.Key] = kv.Value;
                GUILayout.EndHorizontal();
                DrawMoveButton(airfield, kv.Key);
                EndCard();
            }

            if (storage.Count > 0)
            {
                BeginCard(170f);
                GUILayout.Label("DEPLOY ARMY", Bold());

                List<KeyValuePair<DronePlan, int>> staged = _pendingDeployment.Where(kv => kv.Value > 0).ToList();
                if (staged.Count == 0)
                {
                    GUILayout.Label("Use +/-/All on a stock card above to stage drones/missiles, then Deploy.", Italic());
                }
                else
                {
                    foreach (KeyValuePair<DronePlan, int> kv in staged)
                        GUILayout.Label($"{kv.Key.Name} x{kv.Value}");
                }

                GUILayout.FlexibleSpace();
                GUI.enabled = staged.Count > 0;
                if (GUILayout.Button("Deploy"))
                {
                    bool ok = TryDeployArmy(airfield, _pendingDeployment, out string error);
                    _combatFeedback = ok ? "New army deployed." : error;
                    if (ok)
                        _pendingDeployment.Clear();
                }
                GUI.enabled = true;
                EndCard();
            }

            DrawTransferCardsFor(airfield);
        }

        /// <summary>The "Move" button on a stock card — enters transfer-picking mode for that site/Plan (see <see cref="BeginTransferPicking"/>) rather than moving anything immediately.</summary>
        private void DrawMoveButton(Site site, DronePlan plan)
        {
            bool picking = _transferSourceSite == site && _transferPlan == plan;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(picking ? "Picking..." : "Move"))
            {
                if (picking)
                    CancelTransferPicking();
                else
                    BeginTransferPicking(site, plan);
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>Cards for every transfer currently in progress to/from a site — shown on both the sending and receiving Warehouse/Airfield's own card row.</summary>
        private void DrawTransferCardsFor(Site site)
        {
            foreach (TransferOrder order in _transferOrders.Where(o => o.Source == site))
            {
                BeginCard(150f);
                GUILayout.Label($"{order.Plan.Name} (out)", Bold());
                GUILayout.Label($"{order.Amount} -> {DescribeSite(order.Destination)}");
                GUILayout.Label(order.TurnsRemaining > 0 ? $"{order.TurnsRemaining}t left" : "arriving", Italic());
                EndCard();
            }

            foreach (TransferOrder order in _transferOrders.Where(o => o.Destination == site))
            {
                BeginCard(150f);
                GUILayout.Label($"{order.Plan.Name} (in)", Bold());
                GUILayout.Label($"{order.Amount} from {DescribeSite(order.Source)}");
                GUILayout.Label(order.TurnsRemaining > 0 ? $"{order.TurnsRemaining}t left" : "arriving", Italic());
                EndCard();
            }
        }

        /// <summary>A compact card for an army sitting on the currently selected hex — lets the player pick which one to inspect/command without needing to click its 3D marker directly.</summary>
        private void DrawArmyHereCard(Army army)
        {
            BeginCard(140f);
            GUILayout.Label(army.Name, Bold());
            DrawRankBadge(army.Rank);
            GUILayout.Label($"D:{army.DroneCount} M:{army.MissileCount}");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(SelectedArmy == army ? "Selected" : "Select"))
                SelectArmyDirect(army);
            EndCard();
        }

        // ---- Selected-army bar ----------------------------------------------

        /// <summary>
        /// The selected army's detail view: name + rank badge header, then one
        /// card per Plan currently held (count + Resupply/Unload when standing on
        /// an owned Airfield), plus a merge card for any other same-faction army
        /// sharing this hex.
        /// </summary>
        private void DrawArmyBar(Army army)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Army: {army.Name}", Bold());
            DrawRankBadge(army.Rank);
            GUILayout.FlexibleSpace();
            string effective = army.IsCombatEffective ? "Combat effective" : "Not combat effective — needs drones and missiles";
            GUILayout.Label($"{army.Owner} — {effective}", Italic());
            GUILayout.EndHorizontal();

            Site standingSite = SiteAt(army.Location);
            bool onOwnAirfield = army.Owner == TheatreFaction.Player && standingSite != null && standingSite.Owner == army.Owner &&
                SiteStorageCatalog.CanDeployArmies(standingSite.Type) && standingSite.IsOperational;

            _bottomBarScroll = GUILayout.BeginScrollView(_bottomBarScroll, GUILayout.ExpandHeight(true));
            GUILayout.BeginHorizontal();

            foreach (KeyValuePair<DronePlan, int> kv in army.Composition.ToList())
                DrawUnitCard(army, kv.Key, kv.Value, standingSite, onOwnAirfield);

            if (army.Owner == TheatreFaction.Player)
            {
                Army sibling = _armies.FirstOrDefault(a => a != army && a.Owner == army.Owner && a.Location.Equals(army.Location));
                if (sibling != null)
                    DrawSiblingArmyCard(army, sibling);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

            if (army.Owner == TheatreFaction.Player)
            {
                if (army.HasMovedThisTurn)
                    GUILayout.Label("Already moved this turn.", Italic());
                else if (Controller.Result == TheatreResult.InProgress)
                    GUILayout.Label("Click a highlighted hex to move/attack there.", Italic());
            }

            if (!string.IsNullOrEmpty(_combatFeedback))
                GUILayout.Label(_combatFeedback, Italic());
        }

        /// <summary>One card per Plan held by an army — the closest analogue to "a card per unit" this pooled-composition model supports (see Army.Composition), with Resupply/Unload against the Airfield it's currently standing on, if any.</summary>
        private void DrawUnitCard(Army army, DronePlan plan, int count, Site standingSite, bool onOwnAirfield)
        {
            BeginCard(150f);
            GUILayout.Label(plan.Name, Bold());
            GUILayout.Label($"{plan.Category} x{count}");
            DrawPlanIcon(plan);
            GUILayout.FlexibleSpace();

            if (onOwnAirfield)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Resupply", GUILayout.Width(70)))
                {
                    var selection = new Dictionary<DronePlan, int> { { plan, 1 } };
                    bool ok = TryRestockArmy(army, standingSite, selection, out string error);
                    _combatFeedback = ok ? $"Drew 1 {plan.Name}." : error;
                }
                if (GUILayout.Button("Unload", GUILayout.Width(60)))
                {
                    bool ok = TryUnloadArmy(army, standingSite, plan, 1, out string error);
                    _combatFeedback = ok ? $"Returned 1 {plan.Name}." : error;
                }
                GUILayout.EndHorizontal();
            }

            EndCard();
        }

        private void DrawSiblingArmyCard(Army selected, Army other)
        {
            BeginCard(150f);
            GUILayout.Label(other.Name, Bold());
            DrawRankBadge(other.Rank);
            GUILayout.Label($"D:{other.DroneCount} M:{other.MissileCount}");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Merge in"))
            {
                bool ok = TryMergeArmies(other, selected, out string error);
                _combatFeedback = ok ? $"Merged {other.Name} in." : error;
            }
            EndCard();
        }

        /// <summary>
        /// Small always-on-screen name/rank tag floating above each army's 3D
        /// marker (world-to-screen projected), drawn before the bottom bar so the
        /// bar visually sits on top of any tag it happens to overlap. Purely
        /// informational — clicking still goes through <see cref="HandleWorldClick"/>
        /// against the 3D marker underneath, not these labels.
        /// </summary>
        private void DrawArmyNameTags()
        {
            if (Camera.main == null)
                return;

            // A background box (not a bare label) plus generous padding and
            // screen-clamping — a bare label at exactly text-height clipped at the
            // top/bottom of the screen when an army was near the viewport edge.
            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(6, 6, 3, 3),
                wordWrap = false,
            };

            foreach (Army army in _armies)
            {
                Vector3 worldPos = ArmyWorldPosition(army) + Vector3.up * 0.6f;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                if (screenPos.z <= 0f)
                    continue;

                style.normal.textColor = army.Owner == TheatreFaction.Player ? new Color(0.75f, 0.9f, 1f) : new Color(1f, 0.75f, 0.75f);

                string label = $"{army.Name} ({ArmyRankNames.Abbreviation(army.Rank)})";
                Vector2 textSize = style.CalcSize(new GUIContent(label));
                float width = textSize.x;
                float height = textSize.y;

                Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
                float x = Mathf.Clamp(guiPos.x - width * 0.5f, 0f, Mathf.Max(0f, Screen.width - width));
                float y = Mathf.Clamp(guiPos.y - height, 0f, Mathf.Max(0f, Screen.height - height));

                GUI.Box(new Rect(x, y, width, height), label, style);
            }
        }

        /// <summary>
        /// A small compass in the top-right corner that counter-rotates against the
        /// camera's current orbit yaw, so "N" always points to true world north
        /// (marked with a red needle) regardless of how the map has been rotated.
        /// </summary>
        private void DrawCompass()
        {
            if (_cameraController == null)
                return;

            var compassRect = new Rect(Screen.width - 90f, 30f, 70f, 70f);
            GUI.Box(compassRect, GUIContent.none);

            Matrix4x4 matrixBackup = GUI.matrix;
            GUIUtility.RotateAroundPivot(-_cameraController.Yaw, compassRect.center);

            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            const float pad = 4f;
            GUI.Label(new Rect(compassRect.x, compassRect.y + pad + 10f, compassRect.width, 16f), "N", style);
            GUI.Label(new Rect(compassRect.x, compassRect.y + compassRect.height - 16f - pad, compassRect.width, 16f), "S", style);
            GUI.Label(new Rect(compassRect.x + pad, compassRect.y + compassRect.height * 0.5f - 8f, 16f, 16f), "W", style);
            GUI.Label(new Rect(compassRect.x + compassRect.width - 16f - pad, compassRect.y + compassRect.height * 0.5f - 8f, 16f, 16f), "E", style);

            // Red needle pointing at true north — drawn last (on top) inside the
            // same rotated block so it always points at the "N" label above.
            var needleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20 };
            needleStyle.normal.textColor = Color.red;
            GUI.Label(new Rect(compassRect.x, compassRect.y - 16f, compassRect.width, 24f), "\u25B2", needleStyle);

            GUI.matrix = matrixBackup;
        }

        private static GUIStyle Bold() => new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        private static GUIStyle Italic() => new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic, wordWrap = true };
    }
}
