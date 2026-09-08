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

        private GameObject _showcaseAnchor;
        private string _planNameInput = "";
        private string _planFeedback;

        public TheatreWorldState World { get; private set; }
        public TheatreTurnController Controller { get; private set; }
        public HexTile SelectedTile { get; private set; }

        private TerritorialControlCondition _territorialCondition;

        /// <summary>Every Plan the player has designed at any Lab so far (POC scope: Player only, one shared catalog rather than per-Lab).</summary>
        public List<DronePlan> PlayerPlans { get; } = new List<DronePlan>();

        /// <summary>Completed production, keyed by which Plan — a stand-in for a real per-instance stockpile until the combat-instance feedback loop exists.</summary>
        public Dictionary<DronePlan, int> PlayerInventory { get; } = new Dictionary<DronePlan, int>();

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
        }

        /// <summary>The corner info/build panel's screen rect — used to stop world-space clicks/drags/scrolls "reaching through" the UI (see IsPointerOverUI).</summary>
        private readonly Rect _hudPanelRect = new Rect(20, 20, 400, 720);

        /// <summary>
        /// True if the mouse cursor is currently over the OnGUI panel. Checked by
        /// both this harness's own click-to-select handling and the camera
        /// controller's pan/orbit/zoom, so clicking a "Build"/"Advance Turn" button
        /// etc. can never also select/deselect a hex or move the camera underneath it
        /// (the original "focus goes when I click build" bug).
        /// </summary>
        public bool IsPointerOverUI()
        {
            Vector2 guiPoint = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (_hudPanelRect.Contains(guiPoint))
                return true;

            return _menu != null && _menu.IsPointerOverMenu();
        }

        private GameMenuController _menu;

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
            float markerHeight = SiteMarkerView.HeightForType(site.Type);

            Vector3 worldPos = HexMeshFactory.AxialToWorld(site.Location, HexRadius);
            worldPos.y = tileHeight + markerHeight * 0.5f;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Site_{site.Type}_{site.Owner}";
            go.transform.SetParent(transform, worldPositionStays: true);
            go.transform.position = worldPos;
            go.transform.localScale = new Vector3(0.55f, markerHeight, 0.55f);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>()); // purely a visual marker, not clickable itself

            var marker = go.AddComponent<SiteMarkerView>();
            marker.Initialize(site);
            _siteViews.Add(marker);
        }

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

            var cameraController = cameraGo.AddComponent<TheatreMapCameraController>();
            cameraController.focusPoint = gridCenter;
            cameraController.isPointerOverUI = IsPointerOverUI;
        }

        private void BuildMenu()
        {
            var menuGo = new GameObject("GameMenu");
            menuGo.transform.SetParent(transform, worldPositionStays: true);
            var menu = menuGo.AddComponent<GameMenuController>();
            menu.harness = this;
            _menu = menu;
        }

        // Left mouse button now also drag-pans the camera (TheatreMapCameraController)
        // — so a hex is only selected on mouse-up if the press-to-release movement
        // stayed under this threshold, otherwise it was a drag, not a click.
        private const float ClickDragThresholdPixels = 6f;
        private Vector3 _mouseDownScreenPosition;
        private bool _mouseDownWasOverUI;

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
                    {
                        var view = hit.collider.GetComponent<HexTileView>();
                        if (view != null)
                            SelectTile(view.Tile);
                    }
                }
            }
        }

        /// <summary>
        /// Public (not just Update-private) so this can be driven directly from a
        /// headless test without needing to simulate real mouse input/raycasts.
        /// </summary>
        public void SelectTile(HexTile tile)
        {
            SelectedTile = tile;
            RefreshShowcaseForSelection();
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
            RefreshShowcaseForSelection();

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

        /// <summary>Queues one unit of <paramref name="plan"/> for production at the selected Factory. False (no-op) if there isn't an eligible Factory selected.</summary>
        public bool TryQueueProduction(DronePlan plan)
        {
            Site factory = SelectedFactory();
            if (factory == null)
                return false;

            if (!_productionQueues.TryGetValue(factory, out List<ProductionOrder> queue))
            {
                queue = new List<ProductionOrder>();
                _productionQueues[factory] = queue;
            }

            queue.Add(new ProductionOrder(plan, TurnsToProduce(plan.Category)));
            return true;
        }

        /// <summary>All in-progress production orders at the given Factory (for UI display).</summary>
        public IReadOnlyList<ProductionOrder> ProductionQueueAt(Site factory) =>
            _productionQueues.TryGetValue(factory, out List<ProductionOrder> queue) ? queue : System.Array.Empty<ProductionOrder>();

        private void TickProduction()
        {
            foreach (List<ProductionOrder> queue in _productionQueues.Values)
            {
                for (int i = queue.Count - 1; i >= 0; i--)
                {
                    queue[i].TurnsRemaining--;
                    if (queue[i].TurnsRemaining > 0)
                        continue;

                    DronePlan plan = queue[i].Plan;
                    PlayerInventory.TryGetValue(plan, out int count);
                    PlayerInventory[plan] = count + 1;
                    queue.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Rebuilds the small 3D showcase of every designed Plan next to whichever
        /// Lab/Factory is currently selected — "shown what they look like" both where
        /// they're designed and where they're built, without needing a persistent
        /// showcase at every site simultaneously.
        /// </summary>
        private void RefreshShowcaseForSelection()
        {
            if (_showcaseAnchor != null)
            {
                Destroy(_showcaseAnchor);
                _showcaseAnchor = null;
            }

            if (SelectedTile == null || PlayerPlans.Count == 0)
                return;

            Site site = SiteAt(SelectedTile.Coordinate);
            if (site == null || site.Owner != TheatreFaction.Player)
                return;
            if (site.Type != SiteType.Lab && site.Type != SiteType.Factory)
                return;

            HexTile tile = World.Grid.GetTile(site.Location);
            float tileHeight = tile != null && tile.Terrain == TerrainType.Mountain ? MountainHeight : HexHeight;
            float markerHeight = SiteMarkerView.HeightForType(site.Type);
            Vector3 anchorPos = HexMeshFactory.AxialToWorld(site.Location, HexRadius) + new Vector3(1.4f, tileHeight + markerHeight * 0.5f, 0f);

            _showcaseAnchor = new GameObject("PlanShowcase");
            _showcaseAnchor.transform.SetParent(transform, worldPositionStays: true);
            _showcaseAnchor.transform.position = anchorPos;

            for (int i = 0; i < PlayerPlans.Count; i++)
            {
                var slot = new GameObject($"Slot_{i}");
                slot.transform.SetParent(_showcaseAnchor.transform, false);
                slot.transform.localPosition = new Vector3(i * 0.7f, 0f, 0f);
                PlanPreviewBuilder.Build(slot.transform, PlayerPlans[i]);
            }
        }

        public void AdvanceTurn()
        {
            Controller.AdvanceTurn();
            TickProduction();
            RefreshAllViews();
        }

        private void RefreshAllViews()
        {
            foreach (HexTileView view in _tileViews.Values)
                view.Refresh();
            foreach (SiteMarkerView marker in _siteViews)
                marker.Refresh();
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
                playerTerritorialSustainTurns = _territorialCondition.ConsecutiveTurnsMet(TheatreFaction.Player),
                enemyTerritorialSustainTurns = _territorialCondition.ConsecutiveTurnsMet(TheatreFaction.Enemy),
            };

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

            foreach (KeyValuePair<DronePlan, int> entry in PlayerInventory)
                data.inventory.Add(new SavedInventoryEntry { planName = entry.Key.Name, count = entry.Value });

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
                    });
                }
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

            PlayerInventory.Clear();
            foreach (SavedInventoryEntry saved in data.inventory)
            {
                DronePlan plan = PlayerPlans.FirstOrDefault(p => p.Name == saved.planName);
                if (plan != null)
                    PlayerInventory[plan] = saved.count;
            }

            foreach (SavedProductionOrder saved in data.productionOrders)
            {
                Site factory = World.Sites.FirstOrDefault(s => s.Type == SiteType.Factory && s.Location.Equals(new HexCoordinate(saved.factoryQ, saved.factoryR)));
                DronePlan plan = PlayerPlans.FirstOrDefault(p => p.Name == saved.planName);
                if (factory == null || plan == null)
                    continue;

                if (!_productionQueues.TryGetValue(factory, out List<ProductionOrder> queue))
                {
                    queue = new List<ProductionOrder>();
                    _productionQueues[factory] = queue;
                }
                queue.Add(new ProductionOrder(plan, saved.turnsRemaining));
            }

            Enum.TryParse(data.result, out TheatreResult result);
            Controller.RestoreProgress(data.currentTurn, result);
            Controller.ResourcePool[TheatreFaction.Player] = data.playerResource;
            Controller.ResourcePool[TheatreFaction.Enemy] = data.enemyResource;
            _territorialCondition.RestoreConsecutiveTurns(TheatreFaction.Player, data.playerTerritorialSustainTurns);
            _territorialCondition.RestoreConsecutiveTurns(TheatreFaction.Enemy, data.enemyTerritorialSustainTurns);

            SelectedTile = null;
            RefreshShowcaseForSelection();
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
            PlayerPlans.Clear();
            PlayerInventory.Clear();
            SelectedTile = null;
            _showcaseAnchor = null;

            Build();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 400, 720), GUI.skin.box);
            GUILayout.Label("Vanquish — Phase 2 Theatre Map (POC)", Bold());
            GUILayout.Space(6);

            GUILayout.Label($"Turn: {Controller.CurrentTurn}    Result: {Controller.Result}");
            GUILayout.Label($"Resources — Player: {Controller.ResourcePool[TheatreFaction.Player]}    Enemy: {Controller.ResourcePool[TheatreFaction.Enemy]}");
            GUILayout.Label($"Hex ownership — Player: {World.Grid.OwnershipFraction(TheatreFaction.Player):P0}    Enemy: {World.Grid.OwnershipFraction(TheatreFaction.Enemy):P0}");

            if (PlayerInventory.Count > 0)
                GUILayout.Label("Inventory: " + string.Join(", ", PlayerInventory.Select(kv => $"{kv.Key.Name} x{kv.Value}")));

            GUILayout.Space(8);
            if (SelectedTile != null)
            {
                GUILayout.Label($"Selected hex {SelectedTile.Coordinate}: {SelectedTile.Terrain}, owned by {SelectedTile.Owner}", Bold());
                Site siteHere = SiteAt(SelectedTile.Coordinate);
                if (siteHere != null)
                {
                    string progress = siteHere.State == SiteState.Operational
                        ? "Operational"
                        : $"{siteHere.State} — {siteHere.TurnsRemaining} turn(s) remaining";
                    GUILayout.Label($"  Site: {siteHere.Owner} {siteHere.Type} — {progress}");
                }

                GUI.enabled = CanCaptureSelectedTile() && Controller.Result == TheatreResult.InProgress;
                if (GUILayout.Button("Capture this hex for Player"))
                    TryCaptureSelectedTile();
                GUI.enabled = true;

                DrawBuildMenu(siteHere);

                if (siteHere != null && siteHere.Owner == TheatreFaction.Player && siteHere.IsOperational)
                {
                    if (siteHere.Type == SiteType.Lab)
                        DrawLabPanel();
                    else if (siteHere.Type == SiteType.Factory)
                        DrawFactoryPanel(siteHere);
                }
            }
            else
            {
                GUILayout.Label("Click a hex to select it.");
            }

            GUILayout.Space(8);
            GUI.enabled = Controller.Result == TheatreResult.InProgress;
            if (GUILayout.Button("Advance Turn", GUILayout.Height(28)))
                AdvanceTurn();
            GUI.enabled = true;

            GUILayout.Space(6);
            GUILayout.Label("WASD or left-drag pans, right-drag rotates, scroll zooms, click (without dragging) selects a hex.", Italic());
            GUILayout.EndArea();
        }

        private void DrawLabPanel()
        {
            GUILayout.Space(8);
            GUILayout.Label("Research Lab — design a new plan:", Bold());

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", GUILayout.Width(50));
            _planNameInput = GUILayout.TextField(_planNameInput, GUILayout.Width(180));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Design Quadcopter"))
                SubmitPlan(UnitCategory.Quadcopter);
            if (GUILayout.Button("Design Hexacopter"))
                SubmitPlan(UnitCategory.Hexacopter);
            if (GUILayout.Button("Design Missile"))
                SubmitPlan(UnitCategory.Missile);
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_planFeedback))
                GUILayout.Label(_planFeedback, Italic());

            if (PlayerPlans.Count > 0)
            {
                GUILayout.Label("Designed plans (previewed beside this lab in-world):");
                foreach (DronePlan plan in PlayerPlans)
                    GUILayout.Label($"  {plan.Name} — {plan.Category}");
            }

            GUILayout.Label("Tech research: not implemented yet in this POC.", Italic());
        }

        private void SubmitPlan(UnitCategory category)
        {
            bool ok = TryCreatePlan(_planNameInput, category, out string error);
            _planFeedback = ok ? $"Saved '{_planNameInput}' ({category})." : error;
            if (ok)
                _planNameInput = "";
        }

        private void DrawFactoryPanel(Site factory)
        {
            GUILayout.Space(8);
            GUILayout.Label("Factory Production", Bold());

            if (PlayerPlans.Count == 0)
            {
                GUILayout.Label("No plans designed yet — build a Lab and design one there first.", Italic());
                return;
            }

            foreach (DronePlan plan in PlayerPlans)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{plan.Name} ({plan.Category})", GUILayout.Width(220));
                if (GUILayout.Button($"Build ({TurnsToProduce(plan.Category)}t)"))
                    TryQueueProduction(plan);
                GUILayout.EndHorizontal();
            }

            IReadOnlyList<ProductionOrder> queue = ProductionQueueAt(factory);
            if (queue.Count > 0)
            {
                GUILayout.Label("In production:");
                foreach (ProductionOrder order in queue)
                    GUILayout.Label($"  {order.Plan.Name} — {order.TurnsRemaining} turn(s) left");
            }
        }

        private void DrawBuildMenu(Site siteHere)
        {
            if (SelectedTile.Owner != TheatreFaction.Player)
                return;

            GUILayout.Space(8);

            if (siteHere != null)
            {
                GUILayout.Label("Already occupied — nothing else can be built here.", Italic());
                return;
            }

            if (!IsTerrainBuildable(SelectedTile.Terrain))
            {
                GUILayout.Label($"Cannot build on {SelectedTile.Terrain} terrain.", Italic());
                return;
            }

            GUILayout.Label("Build here:", Bold());
            bool canStartConstruction = Controller.Result == TheatreResult.InProgress;
            foreach (SiteBuildOption option in SiteBuildCatalog.Options)
            {
                GUI.enabled = canStartConstruction;
                if (GUILayout.Button($"{option.DisplayName} ({option.TurnsToBuild} turn(s))"))
                    TryBeginConstructionOnSelectedTile(option.Type, option.TurnsToBuild);
                GUI.enabled = true;
            }
        }

        private static GUIStyle Bold() => new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        private static GUIStyle Italic() => new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic, wordWrap = true };
    }
}
