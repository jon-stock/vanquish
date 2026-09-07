using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vanquish.Theatre.DebugTools
{
    // Namespace deliberately not "Vanquish.Theatre.Debug" — that would shadow
    // UnityEngine.Debug for any code written inside it.

    /// <summary>
    /// Press-Play-and-click test harness for the Phase 2 theatre-map logic
    /// (PLAN.md). Builds a small 5-hex front line, two factories, and both Phase 2
    /// victory conditions entirely in code at Start(), then draws an OnGUI panel so
    /// you can step turns and trigger either victory condition yourself.
    ///
    /// To use: open Assets/_Project/Scenes/Phase2_TheatreDebugHarness.unity and press Play.
    /// </summary>
    public class TheatreDebugHarness : MonoBehaviour
    {
        private static readonly HexCoordinate[] Line =
        {
            new HexCoordinate(0, 0), new HexCoordinate(1, 0), new HexCoordinate(2, 0),
            new HexCoordinate(3, 0), new HexCoordinate(4, 0),
        };

        private HexGrid _grid;
        private TheatreWorldState _world;
        private TheatreTurnController _controller;
        private Site _playerFactory;
        private Site _enemyFactory;

        private void Start()
        {
            BuildTheatre();
        }

        private void BuildTheatre()
        {
            _grid = new HexGrid();

            // A 5-hex front: Player holds a road-connected rear (0,1) and one
            // contested forward hex (2); Enemy holds the other two (3,4).
            _grid.GetOrAddTile(Line[0], TerrainType.Road, TheatreFaction.Player);
            _grid.GetOrAddTile(Line[1], TerrainType.Road, TheatreFaction.Player);
            _grid.GetOrAddTile(Line[2], TerrainType.Open, TheatreFaction.Player);
            _grid.GetOrAddTile(Line[3], TerrainType.Open, TheatreFaction.Enemy);
            _grid.GetOrAddTile(Line[4], TerrainType.Open, TheatreFaction.Enemy);

            _world = new TheatreWorldState(_grid);

            _playerFactory = Site.BeginConstruction(SiteType.Factory, TheatreFaction.Player, Line[0], turnsToBuild: 1);
            _playerFactory.Tick(); // instantly operational for the demo baseline
            _world.Sites.Add(_playerFactory);

            _enemyFactory = Site.BeginConstruction(SiteType.Factory, TheatreFaction.Enemy, Line[4], turnsToBuild: 1);
            _enemyFactory.Tick();
            _world.Sites.Add(_enemyFactory);

            _controller = new TheatreTurnController(_world, new ITheatreVictoryCondition[]
            {
                new EconomicCollapseCondition(),
                new TerritorialControlCondition(requiredFraction: 0.75f, requiredConsecutiveTurns: 2),
            });
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 480, 560), GUI.skin.box);

            GUILayout.Label("Vanquish — Phase 2 Theatre Debug Harness", Bold());
            GUILayout.Space(8);

            GUILayout.Label($"Turn: {_controller.CurrentTurn}    Result: {_controller.Result}");
            GUILayout.Label($"Resources — Player: {_controller.ResourcePool[TheatreFaction.Player]}    Enemy: {_controller.ResourcePool[TheatreFaction.Enemy]}");

            GUILayout.Space(8);
            float playerFrac = _grid.OwnershipFraction(TheatreFaction.Player);
            float enemyFrac = _grid.OwnershipFraction(TheatreFaction.Enemy);
            GUILayout.Label($"Hex ownership — Player: {playerFrac:P0}    Enemy: {enemyFrac:P0}    (win threshold: 75%, sustained 2 turns)");

            int frontLineCount = _grid.FrontLineTiles().Count();
            GUILayout.Label($"Front-line hexes right now: {frontLineCount}");

            GUILayout.Space(8);
            GUILayout.Label("Sites:", Bold());
            foreach (Site site in _world.Sites)
            {
                GUILayout.Label($"  {site.Owner} {site.Type} @ {site.Location} — {site.State}" +
                                 (site.State == SiteState.UnderConstruction || site.State == SiteState.Repairing || site.State == SiteState.Relocating
                                     ? $" ({site.TurnsRemaining} turn(s) left)"
                                     : $" — health {site.HealthFraction01:P0}"));
            }

            GUILayout.Space(12);

            GUI.enabled = _controller.Result == TheatreResult.InProgress;

            if (GUILayout.Button("Advance Turn", GUILayout.Height(30)))
                _controller.AdvanceTurn();

            GUILayout.Space(4);
            if (GUILayout.Button("Destroy Enemy Factory (trigger economic collapse)", GUILayout.Height(26)))
                _enemyFactory.ApplyDamage(1f);

            if (GUILayout.Button("Player captures hex 3 (push toward territorial control)", GUILayout.Height(26)))
            {
                HexTile tile = _grid.GetTile(Line[3]);
                if (tile != null)
                    tile.Owner = TheatreFaction.Player;
            }

            GUI.enabled = true;

            GUILayout.Space(8);
            if (GUILayout.Button("Reset Theatre", GUILayout.Height(24)))
                BuildTheatre();

            GUILayout.Space(8);
            GUILayout.Label(
                "Try: click 'Destroy Enemy Factory' then 'Advance Turn' to see economic-" +
                "collapse victory. Or reset, click 'capture hex 3' then 'Advance Turn' " +
                "twice in a row (75%+ ownership sustained 2 turns) to see territorial " +
                "control victory instead.",
                Wrap());

            GUILayout.EndArea();
        }

        private static GUIStyle Bold() => new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        private static GUIStyle Wrap() => new GUIStyle(GUI.skin.label) { wordWrap = true, fontStyle = FontStyle.Italic };
    }
}
