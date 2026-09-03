using UnityEngine;
using UnityEngine.UIElements;
using Vanquish.Core;

namespace Vanquish.MainMenu
{
    /// <summary>
    /// Phase 3A: the actual entry point of the app — previously Workshop.unity was the
    /// de facto first scene (nothing loaded before it), with no real "start a session"
    /// screen at all. Built with UI Toolkit, mirroring WorkshopController's
    /// UIDocument + Menu.uxml/.uss pattern rather than OnGUI, since this is meant to be
    /// a real player-facing screen from the start, not Phase 2-style scaffolding.
    ///
    /// Only "Design Craft" is wired to a real destination right now (the Workshop) —
    /// Campaign/Skirmish are shown but disabled, since neither has anywhere to send
    /// the player yet (the Sandbox Campaign map is still pre-Phase-3 deep-dive work,
    /// per PLAN.md; a dedicated skirmish-scenario-only entry point doesn't exist
    /// separately from the Workshop's own scenario picker). Removing them entirely
    /// would hide that they're coming; showing them disabled signals "not yet" without
    /// implying they're broken.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        private UIDocument _document;
        private Button _designCraftButton;
        private Button _campaignButton;
        private Button _skirmishButton;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            VisualElement root = _document.rootVisualElement;
            _designCraftButton = root.Q<Button>("design-craft-button");
            _campaignButton = root.Q<Button>("campaign-button");
            _skirmishButton = root.Q<Button>("skirmish-button");

            _designCraftButton.clicked += OnDesignCraftClicked;

            // Not implemented yet — disabled rather than omitted, see class doc comment.
            _campaignButton.SetEnabled(false);
            _campaignButton.text = "Campaign (Coming Soon)";
            _skirmishButton.SetEnabled(false);
            _skirmishButton.text = "Skirmish (Coming Soon)";
        }

        private void OnDisable()
        {
            if (_designCraftButton != null)
                _designCraftButton.clicked -= OnDesignCraftClicked;
        }

        private void OnDesignCraftClicked()
        {
            GameFlowController.LoadWorkshop();
        }
    }
}
