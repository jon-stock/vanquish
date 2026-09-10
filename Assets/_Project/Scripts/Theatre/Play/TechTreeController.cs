using System.Collections.Generic;
using UnityEngine;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// Modal "Tech Tree" browser opened from an Operational, owned Lab's bottom-bar
    /// card (see <see cref="TheatreMapHarness.DrawLabCards"/>). Renders
    /// <see cref="TheatreTechCatalog"/> as an actual node-and-line tree — one column
    /// per <see cref="TechBranch"/> (simple techs at the top, more complicated ones
    /// deeper down each column, per the branch's linear prerequisite chain), plus a
    /// rightmost "Airframe Evolution" column whose tiers cross-link back to nodes in
    /// the other columns — rather than the old flat scrolling list. Nodes have an
    /// Unlock button gated on prerequisites and the Player's current research point
    /// pool (<see cref="TheatreTurnController.ResearchPool"/>, fed by Operational
    /// Labs each turn). Follows the same shell pattern as <see cref="GameMenuController"/>:
    /// a separate MonoBehaviour with a <c>harness</c> reference, toggled open/closed,
    /// and registered against <see cref="TheatreMapHarness.IsPointerOverUI"/> so
    /// clicks on it never reach through to the 3D scene underneath.
    /// </summary>
    public class TechTreeController : MonoBehaviour
    {
        public TheatreMapHarness harness;

        private bool _isOpen;
        private Vector2 _scroll;

        // ---- Tree layout constants ------------------------------------------

        private const float NodeWidth = 190f;
        private const float NodeHeight = 96f;
        private const float ColumnGap = 60f;
        private const float RowGap = 34f;
        private const float CanvasPadding = 40f;

        private static readonly TechBranch[] ColumnOrder =
        {
            TechBranch.Propeller, TechBranch.Battery, TechBranch.Avionics, TechBranch.Signature, TechBranch.Airframe,
        };

        private const float PanelWidth = 900f;
        private const float PanelMargin = 16f;

        /// <summary>Centered within the space above the bottom action bar (not the whole screen) so it never overlaps <see cref="TheatreMapHarness.DrawBottomBar"/> underneath it — clamped so it still fits on very short/narrow windows.</summary>
        private Rect PanelRect
        {
            get
            {
                float availableHeight = Mathf.Max(0f, Screen.height - TheatreMapHarness.BottomBarHeight);
                float height = Mathf.Max(0f, availableHeight - PanelMargin * 2f);
                float width = Mathf.Min(PanelWidth, Screen.width - PanelMargin * 2f);
                float x = (Screen.width - width) / 2f;
                float y = (availableHeight - height) / 2f;
                return new Rect(x, y, width, height);
            }
        }

        /// <summary>Included in TheatreMapHarness.IsPointerOverUI's coverage so the panel itself blocks click-through while open.</summary>
        public bool IsPointerOverPanel() => _isOpen && PanelRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));

        public void Open() => _isOpen = true;

        private void OnGUI()
        {
            if (!_isOpen)
                return;

            Rect panelRect = PanelRect;
            GUILayout.BeginArea(panelRect, GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Tech Tree", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 });
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Research: {harness.Controller.ResearchPool[TheatreFaction.Player]}", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "Simple sub-component upgrades sit at the top of each column; unlocking one opens up the next. Airframe tiers on the right cross-link to techs from multiple columns. Unlocking a node tracks research progress — it doesn't yet grant a real unit bonus (POC).",
                new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic, wordWrap = true });

            // Reserving the scroll view's rect via GUILayoutUtility (rather than a
            // hardcoded header/footer height) means it always exactly fills
            // whatever vertical space is actually left after the header text above
            // and the Close button below, however many lines the header wraps to.
            Rect viewRect = GUILayoutUtility.GetRect(panelRect.width, 100f, GUILayout.ExpandHeight(true));
            Rect contentRect = ComputeContentRect();

            _scroll = GUI.BeginScrollView(viewRect, _scroll, contentRect);
            DrawColumnHeaders();
            DrawConnections();
            foreach (TheatreTechNode node in TheatreTechCatalog.Nodes)
                DrawNode(node);
            GUI.EndScrollView();

            if (GUILayout.Button("Close", GUILayout.Height(28)))
                _isOpen = false;

            GUILayout.EndArea();
        }

        // ---- Layout helpers ---------------------------------------------------

        private static int ColumnIndex(TechBranch branch)
        {
            for (int i = 0; i < ColumnOrder.Length; i++)
            {
                if (ColumnOrder[i] == branch)
                    return i;
            }
            return 0;
        }

        private static Vector2 NodeTopLeft(TheatreTechNode node)
        {
            float x = CanvasPadding + ColumnIndex(node.Branch) * (NodeWidth + ColumnGap);
            float y = CanvasPadding + 24f + (node.Tier - 1) * (NodeHeight + RowGap);
            return new Vector2(x, y);
        }

        private static Rect NodeRect(TheatreTechNode node)
        {
            Vector2 topLeft = NodeTopLeft(node);
            return new Rect(topLeft.x, topLeft.y, NodeWidth, NodeHeight);
        }

        private static Rect ComputeContentRect()
        {
            int maxTier = 1;
            foreach (TheatreTechNode node in TheatreTechCatalog.Nodes)
                maxTier = Mathf.Max(maxTier, node.Tier);

            float width = CanvasPadding * 2f + ColumnOrder.Length * NodeWidth + (ColumnOrder.Length - 1) * ColumnGap;
            float height = CanvasPadding * 2f + 24f + maxTier * NodeHeight + (maxTier - 1) * RowGap;
            return new Rect(0f, 0f, width, height);
        }

        private void DrawColumnHeaders()
        {
            var style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            for (int i = 0; i < ColumnOrder.Length; i++)
            {
                float x = CanvasPadding + i * (NodeWidth + ColumnGap);
                GUI.Label(new Rect(x, CanvasPadding, NodeWidth, 22f), TheatreTechCatalog.DisplayName(ColumnOrder[i]).ToUpperInvariant(), style);
            }
        }

        private void DrawConnections()
        {
            foreach (TheatreTechNode node in TheatreTechCatalog.Nodes)
            {
                Rect nodeRect = NodeRect(node);
                Vector2 nodeTop = new Vector2(nodeRect.x + nodeRect.width / 2f, nodeRect.y);

                foreach (string prerequisiteId in node.PrerequisiteIds)
                {
                    TheatreTechNode? prerequisite = TheatreTechCatalog.Find(prerequisiteId);
                    if (prerequisite == null)
                        continue;

                    Rect prereqRect = NodeRect(prerequisite.Value);
                    Vector2 prereqBottom = new Vector2(prereqRect.x + prereqRect.width / 2f, prereqRect.y + prereqRect.height);

                    bool sameColumn = prerequisite.Value.Branch == node.Branch;
                    Color lineColor = harness.IsTechUnlocked(node.Id)
                        ? new Color(0.4f, 0.85f, 0.4f)
                        : (sameColumn ? new Color(0.7f, 0.7f, 0.75f) : new Color(0.9f, 0.75f, 0.3f));

                    DrawLine(prereqBottom, nodeTop, lineColor, sameColumn ? 2f : 2.5f);
                }
            }
        }

        private void DrawNode(TheatreTechNode node)
        {
            Rect rect = NodeRect(node);
            bool unlocked = harness.IsTechUnlocked(node.Id);
            bool prereqsMet = ArePrerequisitesMet(node);

            Color boxColor = unlocked ? new Color(0.35f, 0.55f, 0.35f) : (prereqsMet ? new Color(0.3f, 0.35f, 0.45f) : new Color(0.22f, 0.22f, 0.26f));
            Color previousColor = GUI.color;
            GUI.color = boxColor;
            GUI.Box(rect, GUIContent.none);
            GUI.color = previousColor;

            var titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = true, fontSize = 11 };
            var descStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 9 };
            var statusStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic, fontSize = 9 };

            GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 30f), node.DisplayName, titleStyle);
            GUI.Label(new Rect(rect.x + 6f, rect.y + 32f, rect.width - 12f, 34f), node.Description, descStyle);

            string status = unlocked ? "Unlocked" : prereqsMet ? $"Cost: {node.ResearchCost}" : "Locked";
            GUI.Label(new Rect(rect.x + 6f, rect.y + rect.height - 24f, rect.width - 70f, 20f), status, statusStyle);

            GUI.enabled = !unlocked && prereqsMet;
            if (GUI.Button(new Rect(rect.x + rect.width - 60f, rect.y + rect.height - 26f, 54f, 22f), unlocked ? "Done" : "Unlock"))
                harness.TryUnlockTech(node, out _);
            GUI.enabled = true;
        }

        private bool ArePrerequisitesMet(TheatreTechNode node)
        {
            foreach (string prerequisiteId in node.PrerequisiteIds)
            {
                if (!harness.IsTechUnlocked(prerequisiteId))
                    return false;
            }
            return true;
        }

        /// <summary>Draws a straight line between two points in the current OnGUI coordinate space, via the standard "rotate a stretched white texture" runtime trick (no UnityEditor.Handles dependency, so it works in real builds too).</summary>
        private static void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float thickness)
        {
            Vector2 diff = pointB - pointA;
            float length = diff.magnitude;
            if (length < 0.01f)
                return;

            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            Color previousColor = GUI.color;
            Matrix4x4 previousMatrix = GUI.matrix;

            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, pointA);
            GUI.DrawTexture(new Rect(pointA.x, pointA.y - thickness / 2f, length, thickness), Texture2D.whiteTexture);

            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }
    }
}
