using UnityEngine;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// The Lab's "Design" window — opened via <see cref="TheatreMapHarness.DrawLabCards"/>
    /// ("New Design"/"Edit" buttons) — replacing the old inline name-field-plus-
    /// three-category-buttons card with a real part-selection design screen, the
    /// same modal-window pattern as <see cref="TechTreeController"/>. A Quadcopter/
    /// Hexacopter picks a Propeller + Battery; a Missile picks a Warhead + Guidance
    /// + Propulsion (see <see cref="PlanPartCatalog"/>) — only options whose
    /// <see cref="PlanPartOption.RequiredTechId"/> is unlocked (or which have none)
    /// are selectable. A live rotating preview (<see cref="PlanIconRenderer"/>) and
    /// computed stats (<see cref="DronePlan.WeightKg"/>/<see cref="DronePlan.SpeedKph"/>/
    /// <see cref="DronePlan.RangeKm"/>/<see cref="DronePlan.PayloadKg"/>) update live
    /// as parts are picked. Supports both creating a new Plan
    /// (<see cref="TheatreMapHarness.TryCreatePlanWithParts"/>) and editing an
    /// already-saved one in place (<see cref="TheatreMapHarness.TryUpdatePlan"/>) —
    /// every existing Plan is listed on the left so it can be reopened for editing.
    /// Since a saved Plan's parts (and therefore its rendered appearance) can change
    /// after the fact, anywhere a Plan's icon is shown (Factory/Warehouse/Airfield/
    /// army cards) automatically reflects the edit next time it's drawn — no extra
    /// wiring needed, since those call sites always read the same live
    /// <see cref="DronePlan"/> instance.
    /// </summary>
    public class DesignController : MonoBehaviour
    {
        public TheatreMapHarness harness;

        private bool _isOpen;
        private DronePlan _editingPlan; // null while creating a brand new Plan
        private string _nameInput = "";
        private UnitCategory _category = UnitCategory.Quadcopter;
        private string _propellerId, _batteryId, _warheadId, _guidanceId, _propulsionId;
        private string _feedback;
        private Vector2 _listScroll;
        private Vector2 _formScroll;

        // A fixed placeholder name/accent for the live-preview draft plan, kept
        // stable while the player types a real name or tweaks parts — otherwise
        // every keystroke would change the preview's cache key and force
        // PlanIconRenderer to re-render a fresh 48-frame rotation ring per
        // keystroke instead of just once per actual part change.
        private const string DraftPreviewName = "__design_preview__";
        private static readonly Color DraftPreviewAccent = new Color(0.55f, 0.6f, 0.65f);

        private const float PanelWidth = 780f;
        private const float PanelMargin = 16f;
        private const float ListWidth = 190f;
        private const float PreviewSize = 110f;

        /// <summary>Centered within the space above the bottom action bar — same convention as <see cref="TechTreeController"/>.</summary>
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

        public bool IsPointerOverPanel() => _isOpen && PanelRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));

        public void OpenForNew()
        {
            _editingPlan = null;
            _nameInput = "";
            _category = UnitCategory.Quadcopter;
            ResetPartsToDefaults();
            _feedback = null;
            _isOpen = true;
        }

        public void OpenForEdit(DronePlan plan)
        {
            _editingPlan = plan;
            _nameInput = plan.Name;
            _category = plan.Category;
            _propellerId = plan.PropellerId;
            _batteryId = plan.BatteryId;
            _warheadId = plan.WarheadId;
            _guidanceId = plan.GuidanceId;
            _propulsionId = plan.PropulsionId;
            _feedback = null;
            _isOpen = true;
        }

        private void ResetPartsToDefaults()
        {
            _propellerId = PlanPartCatalog.DefaultId(PartSlot.Propeller);
            _batteryId = PlanPartCatalog.DefaultId(PartSlot.Battery);
            _warheadId = PlanPartCatalog.DefaultId(PartSlot.Warhead);
            _guidanceId = PlanPartCatalog.DefaultId(PartSlot.Guidance);
            _propulsionId = PlanPartCatalog.DefaultId(PartSlot.Propulsion);
        }

        private DronePlan BuildDraftPlan() =>
            new DronePlan(DraftPreviewName, _category, DraftPreviewAccent, _propellerId, _batteryId, _warheadId, _guidanceId, _propulsionId);

        // ---- Styles (built lazily in OnGUI — constructing GUIStyle outside an
        // OnGUI/Awake/Start call throws, and a failed static initializer then
        // re-throws on every subsequent touch of this type; see TechTreeController's
        // fix for the same mistake) ----
        private GUIStyle _titleStyle;
        private GUIStyle _boldStyle;
        private GUIStyle _italicStyle;
        private GUIStyle _sectionStyle;

        private void OnGUI()
        {
            if (!_isOpen)
                return;

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle { fontStyle = FontStyle.Bold, fontSize = 14, normal = { textColor = Color.white } };
                _boldStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                _italicStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic, wordWrap = true };
                _sectionStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 12 };
            }

            Rect panelRect = PanelRect;
            Color previousBoxColor = GUI.color;
            GUI.color = new Color(0.102f, 0.114f, 0.141f, 0.97f);
            GUI.Box(panelRect, GUIContent.none);
            GUI.color = previousBoxColor;

            const float pad = 12f;
            var inset = new Rect(panelRect.x + pad, panelRect.y + pad, panelRect.width - pad * 2f, panelRect.height - pad * 2f);

            GUILayout.BeginArea(inset);

            GUILayout.BeginHorizontal();
            GUILayout.Label(_editingPlan == null ? "Design New Plan" : $"Edit Plan — {_editingPlan.Name}", _titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", GUILayout.Width(26), GUILayout.Height(22)))
                _isOpen = false;
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(ListWidth));
            DrawPlanList();
            GUILayout.EndVertical();

            GUILayout.Space(10f);

            GUILayout.BeginVertical();
            DrawForm();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawPlanList()
        {
            GUILayout.Label("YOUR DESIGNS", _sectionStyle);

            _listScroll = GUILayout.BeginScrollView(_listScroll, GUI.skin.box, GUILayout.ExpandHeight(true));
            foreach (DronePlan plan in harness.PlayerPlans)
            {
                bool selected = plan == _editingPlan;
                Color previous = GUI.color;
                GUI.color = selected ? new Color(0.3f, 0.6f, 0.7f) : Color.white;
                if (GUILayout.Button($"{plan.Name}\n({plan.Category})", GUILayout.Height(40)))
                    OpenForEdit(plan);
                GUI.color = previous;
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("+ New Design", GUILayout.Height(28)))
                OpenForNew();
        }

        private void DrawForm()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", GUILayout.Width(45));
            _nameInput = GUILayout.TextField(_nameInput, GUILayout.Width(220));
            GUILayout.FlexibleSpace();
            DrawPreviewAndStats();
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            if (_editingPlan == null)
            {
                GUILayout.Label("Category", _sectionStyle);
                GUILayout.BeginHorizontal();
                DrawCategoryButton("Quadcopter", UnitCategory.Quadcopter);
                DrawCategoryButton("Hexacopter", UnitCategory.Hexacopter);
                DrawCategoryButton("Missile", UnitCategory.Missile);
                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
            }

            _formScroll = GUILayout.BeginScrollView(_formScroll, GUILayout.ExpandHeight(true));

            if (_category == UnitCategory.Missile)
            {
                DrawPartRow("Warhead", PartSlot.Warhead, ref _warheadId);
                DrawPartRow("Guidance", PartSlot.Guidance, ref _guidanceId);
                DrawPartRow("Propulsion", PartSlot.Propulsion, ref _propulsionId);
            }
            else
            {
                DrawPartRow("Propeller", PartSlot.Propeller, ref _propellerId);
                DrawPartRow("Battery", PartSlot.Battery, ref _batteryId);
            }

            GUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(_feedback))
                GUILayout.Label(_feedback, _italicStyle);

            if (GUILayout.Button(_editingPlan == null ? "Save Design" : "Save Changes", GUILayout.Height(30)))
                Save();
        }

        private void DrawCategoryButton(string label, UnitCategory category)
        {
            Color previous = GUI.color;
            GUI.color = _category == category ? new Color(0.3f, 0.6f, 0.7f) : Color.white;
            if (GUILayout.Button(label, GUILayout.Height(28)))
                _category = category;
            GUI.color = previous;
        }

        private void DrawPreviewAndStats()
        {
            DronePlan draft = BuildDraftPlan();
            Texture2D icon = PlanIconRenderer.GetOrCreateIcon(draft);

            GUILayout.BeginHorizontal();
            GUILayout.Label(icon, GUILayout.Width(PreviewSize), GUILayout.Height(PreviewSize));
            GUILayout.BeginVertical();
            GUILayout.Label($"Weight: {draft.WeightKg:0.0} kg");
            GUILayout.Label($"Speed: {draft.SpeedKph:0} km/h");
            GUILayout.Label($"Range: {draft.RangeKm:0.0} km");
            GUILayout.Label($"Payload: {draft.PayloadKg:0.0} kg");
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawPartRow(string label, PartSlot slot, ref string selectedId)
        {
            GUILayout.Label(label, _sectionStyle);
            GUILayout.BeginHorizontal();

            string currentSelection = selectedId;
            foreach (PlanPartOption option in PlanPartCatalog.OptionsFor(slot))
            {
                bool unlocked = string.IsNullOrEmpty(option.RequiredTechId) || harness.IsTechUnlocked(option.RequiredTechId);
                bool selected = currentSelection == option.Id;

                Color previous = GUI.color;
                GUI.color = selected ? new Color(0.3f, 0.6f, 0.7f) : (unlocked ? Color.white : new Color(0.5f, 0.5f, 0.55f));
                GUI.enabled = unlocked;
                if (GUILayout.Button(option.DisplayName, GUILayout.Width(150), GUILayout.Height(44)))
                    selectedId = option.Id;
                GUI.enabled = true;
                GUI.color = previous;
            }

            GUILayout.EndHorizontal();

            PlanPartOption? current = PlanPartCatalog.Find(slot, selectedId);
            if (current != null)
                GUILayout.Label(current.Value.Description, _italicStyle);

            GUILayout.Space(6f);
        }

        private void Save()
        {
            bool ok;
            string error;

            if (_editingPlan == null)
                ok = harness.TryCreatePlanWithParts(_nameInput, _category, _propellerId, _batteryId, _warheadId, _guidanceId, _propulsionId, out error);
            else
                ok = harness.TryUpdatePlan(_editingPlan, _nameInput, _propellerId, _batteryId, _warheadId, _guidanceId, _propulsionId, out error);

            if (ok)
                _isOpen = false;
            else
                _feedback = error;
        }
    }
}
