using System.Collections.Generic;
using UnityEngine;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// Modal "Tech Tree" browser opened from an Operational, owned Lab's bottom-bar
    /// card (see <see cref="TheatreMapHarness.DrawLabCards"/>). Renders
    /// <see cref="TheatreTechCatalog"/> as an actual node-and-line tree — one column
    /// per sub-component branch (simple techs at the top, more complicated ones
    /// deeper down each column, via a linear prerequisite chain), plus a rightmost
    /// evolution column whose tiers cross-link back to nodes in the other columns —
    /// rather than a flat scrolling list. Two tabs (<see cref="TechGroup.Airframe"/>/
    /// <see cref="TechGroup.Missile"/>) switch between the UAV and missile trees.
    /// Everything here is laid out with plain absolute <see cref="Rect"/> math
    /// (not <c>GUILayout</c>) precisely so header/description text of varying
    /// wrapped height can never overlap the node grid below it — <c>GUILayout</c>'s
    /// two-pass Layout/Repaint sizing is unreliable for wrapped labels sized just
    /// before an explicitly-positioned sibling.
    /// Follows the same shell pattern as <see cref="GameMenuController"/>: a
    /// separate MonoBehaviour with a <c>harness</c> reference, toggled open/closed,
    /// and registered against <see cref="TheatreMapHarness.IsPointerOverUI"/> so
    /// clicks on it never reach through to the 3D scene underneath.
    /// </summary>
    public class TechTreeController : MonoBehaviour
    {
        public TheatreMapHarness harness;

        private bool _isOpen;
        private Vector2 _scroll;
        private TechGroup _group = TechGroup.Airframe;

        // ---- Tree layout constants ------------------------------------------

        private const float NodeWidth = 190f;
        private const float NodeHeight = 100f;
        private const float ColumnGap = 70f;
        private const float RowGap = 40f;
        private const float CanvasPadding = 24f;
        private const float ColumnHeaderHeight = 36f;

        private const float PanelWidth = 940f;
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

        // GUIStyle instances must not be constructed from a field initializer/
        // constructor (Unity throws "set_fontStyle is not allowed to be called
        // from a MonoBehaviour constructor" and — worse — a failed static
        // initializer then re-throws on every subsequent access to this type,
        // including from unrelated code like the camera's pointer-over-UI check,
        // which is what caused the reported input lag). Built fresh in OnGUI
        // instead, matching how the rest of this project's OnGUI code creates
        // GUIStyles inline (see TheatreMapHarness.Bold()/Italic()).
        private GUIStyle _titleStyle;
        private GUIStyle _researchStyle;
        private GUIStyle _descriptionStyle;
        private GUIStyle _columnHeaderStyle;

        private const string DescriptionText =
            "Simple sub-component upgrades sit at the top of each column; unlocking one opens up the next. " +
            "The evolution column on the right cross-links to techs from multiple columns. Unlocking a node tracks " +
            "research progress — it doesn't yet grant a real unit bonus (POC).";

        private void OnGUI()
        {
            if (!_isOpen)
                return;

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle { fontStyle = FontStyle.Bold, fontSize = 14, normal = { textColor = Color.white } };
                _researchStyle = new GUIStyle { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = Color.white } };
                _descriptionStyle = new GUIStyle { fontStyle = FontStyle.Italic, wordWrap = true, normal = { textColor = new Color(0.85f, 0.85f, 0.85f) } };
                _columnHeaderStyle = new GUIStyle { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter, wordWrap = true, normal = { textColor = Color.white } };
            }

            Rect panelRect = PanelRect;
            Color previousBoxColor = GUI.color;
            GUI.color = new Color(0.102f, 0.114f, 0.141f, 0.97f); // dark slate (~#1A1D24), evoking the blueprint/HUD look without a full UI Toolkit rebuild
            GUI.Box(panelRect, GUIContent.none);
            GUI.color = previousBoxColor;

            const float pad = 12f;
            const float closeButtonSize = 26f;

            var closeButtonRect = new Rect(panelRect.xMax - pad - closeButtonSize, panelRect.y + pad - 4f, closeButtonSize, closeButtonSize);
            var titleRect = new Rect(panelRect.x + pad, panelRect.y + pad - 4f, panelRect.width - pad * 2f - closeButtonSize - 8f, 22f);
            GUI.Label(titleRect, "Tech Tree", _titleStyle);

            var researchRect = new Rect(panelRect.x + pad, panelRect.y + pad - 4f, panelRect.width - pad * 2f - closeButtonSize - 8f, 22f);
            GUI.Label(researchRect, $"Research: {harness.Controller.ResearchPool[TheatreFaction.Player]}", _researchStyle);

            if (GUI.Button(closeButtonRect, "X"))
                _isOpen = false;

            const float tabHeight = 26f;
            const float tabWidth = 110f;
            float tabsY = panelRect.y + pad + 22f + 6f;
            var airframeTabRect = new Rect(panelRect.x + pad, tabsY, tabWidth, tabHeight);
            var missileTabRect = new Rect(panelRect.x + pad + tabWidth + 6f, tabsY, tabWidth, tabHeight);
            DrawTab(airframeTabRect, "Airframe", TechGroup.Airframe);
            DrawTab(missileTabRect, "Missile", TechGroup.Missile);

            float descY = tabsY + tabHeight + 6f;
            float descWidth = panelRect.width - pad * 2f;
            float descHeight = _descriptionStyle.CalcHeight(new GUIContent(DescriptionText), descWidth);
            var descRect = new Rect(panelRect.x + pad, descY, descWidth, descHeight);
            GUI.Label(descRect, DescriptionText, _descriptionStyle);

            float scrollY = descRect.yMax + 8f;
            var viewRect = new Rect(panelRect.x + pad, scrollY, panelRect.width - pad * 2f, panelRect.yMax - scrollY - pad);
            Rect contentRect = ComputeContentRect(_group);

            _scroll = GUI.BeginScrollView(viewRect, _scroll, contentRect);
            DrawColumnHeaders(_group);
            DrawConnections(_group);
            foreach (TheatreTechNode node in TheatreTechCatalog.Nodes)
            {
                if (node.Group == _group)
                    DrawNode(node);
            }
            GUI.EndScrollView();
        }

        private void DrawTab(Rect rect, string label, TechGroup group)
        {
            bool selected = _group == group;
            Color previousColor = GUI.color;
            GUI.color = selected ? new Color(0.2f, 0.6f, 0.7f) : new Color(0.4f, 0.4f, 0.45f);
            if (GUI.Button(rect, label))
            {
                _group = group;
                _scroll = Vector2.zero;
            }
            GUI.color = previousColor;
        }

        // ---- Layout helpers ---------------------------------------------------

        private static int ColumnIndex(TechGroup group, string column)
        {
            string[] columns = TheatreTechCatalog.ColumnsFor(group);
            for (int i = 0; i < columns.Length; i++)
            {
                if (columns[i] == column)
                    return i;
            }
            return 0;
        }

        private static Vector2 NodeTopLeft(TheatreTechNode node)
        {
            float x = CanvasPadding + ColumnIndex(node.Group, node.Column) * (NodeWidth + ColumnGap);
            float y = CanvasPadding + ColumnHeaderHeight + (node.Tier - 1) * (NodeHeight + RowGap);
            return new Vector2(x, y);
        }

        private static Rect NodeRect(TheatreTechNode node)
        {
            Vector2 topLeft = NodeTopLeft(node);
            return new Rect(topLeft.x, topLeft.y, NodeWidth, NodeHeight);
        }

        private static Rect ComputeContentRect(TechGroup group)
        {
            int maxTier = 1;
            foreach (TheatreTechNode node in TheatreTechCatalog.Nodes)
            {
                if (node.Group == group)
                    maxTier = Mathf.Max(maxTier, node.Tier);
            }

            int columnCount = TheatreTechCatalog.ColumnsFor(group).Length;
            float width = CanvasPadding * 2f + columnCount * NodeWidth + (columnCount - 1) * ColumnGap;
            float height = CanvasPadding * 2f + ColumnHeaderHeight + maxTier * NodeHeight + (maxTier - 1) * RowGap;
            return new Rect(0f, 0f, width, height);
        }

        private void DrawColumnHeaders(TechGroup group)
        {
            string[] columns = TheatreTechCatalog.ColumnsFor(group);
            for (int i = 0; i < columns.Length; i++)
            {
                float x = CanvasPadding + i * (NodeWidth + ColumnGap);
                GUI.Label(new Rect(x, CanvasPadding, NodeWidth, ColumnHeaderHeight), columns[i].ToUpperInvariant(), _columnHeaderStyle);
            }
        }

        private void DrawConnections(TechGroup group)
        {
            foreach (TheatreTechNode node in TheatreTechCatalog.Nodes)
            {
                if (node.Group != group)
                    continue;

                Rect nodeRect = NodeRect(node);
                Vector2 nodeTop = new Vector2(nodeRect.x + nodeRect.width / 2f, nodeRect.y);

                foreach (string prerequisiteId in node.PrerequisiteIds)
                {
                    TheatreTechNode? prerequisite = TheatreTechCatalog.Find(prerequisiteId);
                    if (prerequisite == null)
                        continue;

                    Rect prereqRect = NodeRect(prerequisite.Value);
                    Vector2 prereqBottom = new Vector2(prereqRect.x + prereqRect.width / 2f, prereqRect.y + prereqRect.height);

                    bool sameColumn = prerequisite.Value.Column == node.Column;
                    Color lineColor = harness.IsTechUnlocked(node.Id)
                        ? new Color(0.3f, 0.85f, 0.95f) // cyan — unlocked
                        : (sameColumn ? new Color(0.55f, 0.55f, 0.62f) : new Color(0.85f, 0.65f, 0.35f)); // gray same-column, gold cross-column

                    DrawElbowConnector(prereqBottom, nodeTop, lineColor, sameColumn ? 2f : 2f);
                }
            }
        }

        private void DrawNode(TheatreTechNode node)
        {
            Rect rect = NodeRect(node);
            bool unlocked = harness.IsTechUnlocked(node.Id);
            bool prereqsMet = ArePrerequisitesMet(node);

            Color boxColor = unlocked
                ? new Color(0.16f, 0.45f, 0.5f)    // cyan — unlocked
                : prereqsMet
                    ? new Color(0.32f, 0.24f, 0.42f) // purple — affordable/researchable now
                    : new Color(0.16f, 0.17f, 0.2f); // dark slate — locked
            Color previousColor = GUI.color;
            GUI.color = boxColor;
            GUI.Box(rect, GUIContent.none);
            GUI.color = previousColor;

            var titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = true, fontSize = 11 };
            var descStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 9 };
            var statusStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic, fontSize = 9 };

            GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 32f), node.DisplayName, titleStyle);
            GUI.Label(new Rect(rect.x + 6f, rect.y + 34f, rect.width - 12f, 36f), node.Description, descStyle);

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

        /// <summary>
        /// Draws a clean right-angle (down / across / down) connector between a
        /// prerequisite's bottom edge and a node's top edge, instead of a straight
        /// diagonal — this is what keeps the tree from turning into a tangle of
        /// crisscrossing lines once cross-column dependencies are involved.
        /// </summary>
        private static void DrawElbowConnector(Vector2 from, Vector2 to, Color color, float thickness)
        {
            if (Mathf.Approximately(from.x, to.x))
            {
                DrawLine(from, to, color, thickness);
                return;
            }

            float midY = (from.y + to.y) / 2f;
            var elbowA = new Vector2(from.x, midY);
            var elbowB = new Vector2(to.x, midY);

            DrawLine(from, elbowA, color, thickness);
            DrawLine(elbowA, elbowB, color, thickness);
            DrawLine(elbowB, to, color, thickness);
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
