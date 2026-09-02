using UnityEngine;

namespace Vanquish.Workshop
{
    /// <summary>
    /// Phase 2G: "Test Range" entry point — a single OnGUI button (same "ugly art is
    /// fine, the loop must be complete" precedent as ScenarioPickerOverlay/
    /// HUDController) that loads the Test Range scene with the player's
    /// currently-configured design. Kept as its own tiny component rather than folded
    /// into ScenarioPickerOverlay since it's a wholly separate flow (observe a design
    /// vs. pick a battle) that happens to render on the same screen.
    /// </summary>
    public class TestRangeEntryOverlay : MonoBehaviour
    {
        public WorkshopController workshopController;

        private GUIStyle _buttonStyle;

        private void OnGUI()
        {
            if (workshopController == null)
                return;

            _buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 14 };

            var rect = new Rect(10f, 10f, 230f, 32f);
            if (GUI.Button(rect, "Test Range (no cost)", _buttonStyle))
                workshopController.EnterTestRange();
        }
    }
}
