using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Lets the player switch which committed unit they're directly piloting — the
    /// "drop into any single committed drone" half of PLAN.md's Command &amp; Control
    /// Model. Only the active unit's <see cref="PlayerDroneController"/> accepts
    /// WASD/mouse input; the rest hold position (see PlayerDroneController.IsActive).
    /// Slot N is bound to number key N (slot 0 -&gt; '1', slot 1 -&gt; '2', ...).
    /// </summary>
    public class PlayerUnitSwitcher : MonoBehaviour
    {
        [Serializable]
        public class UnitEntry
        {
            public string label;
            public PlayerDroneController controller;
            public Transform cameraTarget;
        }

        public List<UnitEntry> units = new List<UnitEntry>();
        public CameraModeController cameraModeController;

        public int ActiveIndex { get; private set; } = -1;

        private static readonly KeyCode[] SlotKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
            KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9,
        };

        private void Start()
        {
            if (units.Count > 0)
                SetActive(0);
        }

        private void Update()
        {
            for (int i = 0; i < units.Count && i < SlotKeys.Length; i++)
            {
                if (Input.GetKeyDown(SlotKeys[i]))
                {
                    SetActive(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Public (not just Update-private) so this can be driven directly from a
        /// headless test without needing to simulate real key presses.
        /// </summary>
        public void SetActive(int index)
        {
            if (index < 0 || index >= units.Count)
                return;

            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].controller != null)
                    units[i].controller.IsActive = i == index;
            }

            ActiveIndex = index;
            cameraModeController?.SetActiveUnit(units[index].cameraTarget);
        }
    }
}
