using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
        private const int Columns = 9;
        private const int Rows = 7;
        private const float HexRadius = 1f;

        // Tall enough relative to HexRadius to read as a solid block/tile with a
        // flat lid you can see things resting on, not a thin flat disc (which, at a
        // low camera angle, visually reads as a shallow dish instead of a tile).
        private const float HexHeight = 0.6f;
        private const float MountainHeight = 2f;

        private readonly Dictionary<HexCoordinate, HexTileView> _tileViews = new Dictionary<HexCoordinate, HexTileView>();
        private readonly List<SiteMarkerView> _siteViews = new List<SiteMarkerView>();

        public TheatreWorldState World { get; private set; }
        public TheatreTurnController Controller { get; private set; }
        public HexTile SelectedTile { get; private set; }

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

            Controller = new TheatreTurnController(World, new ITheatreVictoryCondition[]
            {
                new EconomicCollapseCondition(),
                new TerritorialControlCondition(requiredFraction: 0.75f, requiredConsecutiveTurns: 3),
            });

            BuildCamera();
        }

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

        private void CreateSite(SiteType type, TheatreFaction owner, HexCoordinate coord)
        {
            Site site = Site.BeginConstruction(type, owner, coord, turnsToBuild: 1);
            site.Tick(); // instantly Operational for this POC's starting state
            World.Sites.Add(site);

            HexTile tile = World.Grid.GetTile(coord);
            float tileHeight = tile != null && tile.Terrain == TerrainType.Mountain ? MountainHeight : HexHeight;
            float markerHeight = SiteMarkerView.HeightForType(type);

            Vector3 worldPos = HexMeshFactory.AxialToWorld(coord, HexRadius);
            worldPos.y = tileHeight + markerHeight * 0.5f;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Site_{type}_{owner}";
            go.transform.SetParent(transform, worldPositionStays: true);
            go.transform.position = worldPos;
            go.transform.localScale = new Vector3(0.55f, markerHeight, 0.55f);
            Object.Destroy(go.GetComponent<Collider>()); // purely a visual marker, not clickable itself

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
            cameraGo.transform.position = gridCenter + new Vector3(0f, 16f, -10f);
            cameraGo.transform.LookAt(gridCenter);

            var cameraController = cameraGo.AddComponent<TheatreMapCameraController>();
            cameraController.focusPoint = gridCenter;
        }

        // Left mouse button now also drag-pans the camera (TheatreMapCameraController)
        // — so a hex is only selected on mouse-up if the press-to-release movement
        // stayed under this threshold, otherwise it was a drag, not a click.
        private const float ClickDragThresholdPixels = 6f;
        private Vector3 _mouseDownScreenPosition;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                _mouseDownScreenPosition = Input.mousePosition;

            if (Input.GetMouseButtonUp(0) && Camera.main != null)
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

        public void AdvanceTurn()
        {
            Controller.AdvanceTurn();
            RefreshAllViews();
        }

        private void RefreshAllViews()
        {
            foreach (HexTileView view in _tileViews.Values)
                view.Refresh();
            foreach (SiteMarkerView marker in _siteViews)
                marker.Refresh();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 380, 260), GUI.skin.box);
            GUILayout.Label("Vanquish — Phase 2 Theatre Map (POC)", Bold());
            GUILayout.Space(6);

            GUILayout.Label($"Turn: {Controller.CurrentTurn}    Result: {Controller.Result}");
            GUILayout.Label($"Resources — Player: {Controller.ResourcePool[TheatreFaction.Player]}    Enemy: {Controller.ResourcePool[TheatreFaction.Enemy]}");
            GUILayout.Label($"Hex ownership — Player: {World.Grid.OwnershipFraction(TheatreFaction.Player):P0}    Enemy: {World.Grid.OwnershipFraction(TheatreFaction.Enemy):P0}");

            GUILayout.Space(8);
            if (SelectedTile != null)
            {
                GUILayout.Label($"Selected hex {SelectedTile.Coordinate}: {SelectedTile.Terrain}, owned by {SelectedTile.Owner}", Bold());
                Site siteHere = World.Sites.FirstOrDefault(s => s.Location.Equals(SelectedTile.Coordinate) && s.State != SiteState.Destroyed);
                if (siteHere != null)
                    GUILayout.Label($"  Site: {siteHere.Owner} {siteHere.Type} — {siteHere.State}");

                GUI.enabled = CanCaptureSelectedTile() && Controller.Result == TheatreResult.InProgress;
                if (GUILayout.Button("Capture this hex for Player"))
                    TryCaptureSelectedTile();
                GUI.enabled = true;
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

        private static GUIStyle Bold() => new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        private static GUIStyle Italic() => new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic, wordWrap = true };
    }
}
