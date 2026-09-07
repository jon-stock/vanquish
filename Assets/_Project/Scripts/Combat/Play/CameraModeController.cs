using UnityEngine;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Owns which target the <see cref="ChaseCamera"/> is actually looking at: either
    /// following the currently-active piloted unit (the normal mode — see
    /// <see cref="PlayerUnitSwitcher"/>) or, toggled with V, a dedicated objective
    /// view that looks at the target instead of any unit. Switching the active unit
    /// (1/2 keys) always returns to follow mode, since piloting a different craft
    /// implies you want to see it.
    /// </summary>
    public class CameraModeController : MonoBehaviour
    {
        public ChaseCamera chaseCamera;
        public Transform objective;

        private Transform _activeUnit;
        private bool _objectiveView;

        public bool IsObjectiveView => _objectiveView;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.V))
                ToggleObjectiveView();
        }

        /// <summary>Called by PlayerUnitSwitcher when the piloted unit changes — always drops back to follow mode.</summary>
        public void SetActiveUnit(Transform unit)
        {
            _activeUnit = unit;
            _objectiveView = false;
            Apply();
        }

        /// <summary>
        /// Public (not just Update-private) so this can be driven directly from a
        /// headless test without needing to simulate a real key press.
        /// </summary>
        public void ToggleObjectiveView()
        {
            _objectiveView = !_objectiveView;
            Apply();
        }

        private void Apply()
        {
            if (chaseCamera == null)
                return;

            if (_objectiveView)
            {
                chaseCamera.primary = objective;
                chaseCamera.secondary = null;
            }
            else
            {
                chaseCamera.primary = _activeUnit;
                chaseCamera.secondary = objective;
            }
        }
    }
}
