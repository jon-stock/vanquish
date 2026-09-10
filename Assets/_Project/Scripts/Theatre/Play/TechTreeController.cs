using UnityEngine;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// Modal "Tech Tree" browser opened from an Operational, owned Lab's bottom-bar
    /// card (see <see cref="TheatreMapHarness.DrawLabCards"/>). Lists every node in
    /// <see cref="TheatreTechCatalog"/> as a flat scrolling list (no graph view —
    /// matches how the pre-pivot project's own Workshop research tab worked, since a
    /// real node-graph UI was a perpetually-deferred TODO there too), each with an
    /// Unlock button gated on prerequisites and the Player's current research point
    /// pool (<see cref="TheatreTurnController.ResearchPool"/>, fed by Operational
    /// Labs each turn — see <see cref="TheatreTurnController.ResearchPerOperationalLabPerTurn"/>).
    /// Follows the same shell pattern as <see cref="GameMenuController"/>: a separate
    /// MonoBehaviour with a <c>harness</c> reference, toggled open/closed, and
    /// registered against <see cref="TheatreMapHarness.IsPointerOverUI"/> so clicks
    /// on it never reach through to the 3D scene underneath.
    /// </summary>
    public class TechTreeController : MonoBehaviour
    {
        public TheatreMapHarness harness;

        private bool _isOpen;
        private Vector2 _scroll;

        private const float PanelWidth = 440f;
        private const float PanelMargin = 16f;

        /// <summary>
        /// Centered within the space above the bottom action bar (not the whole
        /// screen) so it never overlaps <see cref="TheatreMapHarness.DrawBottomBar"/>
        /// underneath it — clamped so it still fits on very short windows.
        /// </summary>
        private Rect PanelRect
        {
            get
            {
                float availableHeight = Mathf.Max(0f, Screen.height - TheatreMapHarness.BottomBarHeight);
                float height = Mathf.Min(440f, Mathf.Max(0f, availableHeight - PanelMargin * 2f));
                float x = (Screen.width - PanelWidth) / 2f;
                float y = (availableHeight - height) / 2f;
                return new Rect(x, y, PanelWidth, height);
            }
        }

        /// <summary>Included in TheatreMapHarness.IsPointerOverUI's coverage so the panel itself blocks click-through while open.</summary>
        public bool IsPointerOverPanel() => _isOpen && PanelRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));

        public void Open() => _isOpen = true;

        private void OnGUI()
        {
            if (!_isOpen)
                return;

            GUILayout.BeginArea(PanelRect, GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Tech Tree", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Research: {harness.Controller.ResearchPool[TheatreFaction.Player]}", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "Unlocking a node tracks research progress — it doesn't yet grant a real unit bonus (POC).",
                new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic, wordWrap = true });
            GUILayout.Space(6f);

            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (TheatreTechNode node in TheatreTechCatalog.Nodes)
                DrawRow(node);
            GUILayout.EndScrollView();

            GUILayout.Space(6f);
            if (GUILayout.Button("Close", GUILayout.Height(28)))
                _isOpen = false;

            GUILayout.EndArea();
        }

        private void DrawRow(TheatreTechNode node)
        {
            bool unlocked = harness.IsTechUnlocked(node.Id);
            bool prereqsMet = ArePrerequisitesMet(node);

            GUILayout.BeginHorizontal(GUI.skin.box);

            GUILayout.BeginVertical();
            GUILayout.Label(node.DisplayName, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label(node.Description, new GUIStyle(GUI.skin.label) { wordWrap = true });
            string status = unlocked ? "Unlocked" : prereqsMet ? $"Cost: {node.ResearchCost}" : "Locked — missing prerequisites";
            GUILayout.Label(status, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic });
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUI.enabled = !unlocked && prereqsMet;
            if (GUILayout.Button(unlocked ? "Done" : "Unlock", GUILayout.Width(70), GUILayout.Height(36)))
                harness.TryUnlockTech(node, out _);
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }

        private bool ArePrerequisitesMet(TheatreTechNode node)
        {
            foreach (string prereqId in node.PrerequisiteIds)
            {
                if (!harness.IsTechUnlocked(prereqId))
                    return false;
            }
            return true;
        }
    }
}
