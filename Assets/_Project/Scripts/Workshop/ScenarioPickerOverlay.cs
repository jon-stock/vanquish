using UnityEngine;
using Vanquish.Core;
using Vanquish.Data.Scenarios;

namespace Vanquish.Workshop
{
    /// <summary>
    /// Phase 2E: "scenario selection needs a place to live" — a small picker screen in
    /// the Workshop scene, implemented with OnGUI immediate-mode rendering rather than
    /// a UI Toolkit panel (same "ugly art is fine, the loop must be complete"
    /// precedent as HUDController's combat HUD — see its own class doc comment), so
    /// this doesn't require editing Workshop.uxml/.uss for a Phase 2 milestone whose
    /// own goal is just "more than one arena/objective exists and is choosable," not
    /// UI polish (that's Phase 3's job).
    ///
    /// Selecting a scenario stores it on PlayerProgress.PendingScenario;
    /// WorkshopController.OnEnterCombatClicked reads that instead of its own hardcoded
    /// combatSceneName when set. Not selecting anything (or this component/its
    /// scenarios array not being present at all, e.g. an older/rebuilt Workshop scene)
    /// leaves PendingScenario null, which falls back to the original single-arena
    /// behavior — this is purely additive.
    /// </summary>
    public class ScenarioPickerOverlay : MonoBehaviour
    {
        public ScenarioDefinition[] scenarios = System.Array.Empty<ScenarioDefinition>();

        private GUIStyle _headerStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _selectedBodyStyle;
        private int _selectedIndex;

        private void OnGUI()
        {
            if (scenarios == null || scenarios.Length == 0)
                return;

            _headerStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            _bodyStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = Color.white }, wordWrap = true };
            _selectedBodyStyle ??= new GUIStyle(_bodyStyle) { normal = { textColor = Color.yellow } };

            const float panelWidth = 260f;
            float panelHeight = 30f + scenarios.Length * 70f;
            var panelRect = new Rect(10f, Screen.height - panelHeight - 10f, panelWidth, panelHeight);
            GUI.Box(panelRect, GUIContent.none);

            GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + 5f, panelWidth - 20f, 20f), "Scenario", _headerStyle);

            for (int i = 0; i < scenarios.Length; i++)
            {
                ScenarioDefinition scenario = scenarios[i];
                if (scenario == null)
                    continue;

                float rowY = panelRect.y + 30f + i * 70f;
                bool isSelected = i == _selectedIndex;

                var rowRect = new Rect(panelRect.x + 10f, rowY, panelWidth - 20f, 65f);
                if (GUI.Button(rowRect, GUIContent.none))
                {
                    _selectedIndex = i;
                    if (PlayerProgress.Instance != null)
                        PlayerProgress.Instance.PendingScenario = scenario;
                }

                string title = (isSelected ? "> " : "") + scenario.displayName;
                GUI.Label(new Rect(rowRect.x + 4f, rowRect.y + 2f, rowRect.width - 8f, 18f), title, isSelected ? _selectedBodyStyle : _bodyStyle);
                GUI.Label(new Rect(rowRect.x + 4f, rowRect.y + 20f, rowRect.width - 8f, 44f),
                    scenario.objectiveSummary, _bodyStyle);
            }
        }

        private void Start()
        {
            // Default to the first scenario so PlayerProgress.PendingScenario is
            // already meaningful even if the player never touches this picker at all
            // (matches the existing default-part-selection convention elsewhere in
            // the Workshop, rather than requiring an explicit click before Enter
            // Combat becomes meaningful).
            if (scenarios.Length > 0 && scenarios[0] != null && PlayerProgress.Instance != null)
                PlayerProgress.Instance.PendingScenario = scenarios[0];
        }
    }
}
