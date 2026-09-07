using System.Collections.Generic;
using UnityEngine;

namespace Vanquish.Combat.Play
{
    /// <summary>
    /// Keeps a drone's visibly-mounted missile props in sync with its actual
    /// remaining ammo — removes one mounted missile visual each time
    /// WeaponController.Fire() succeeds, so a drone that took off with N missiles
    /// visibly shows N, then N-1, then N-2, ... as it expends them, instead of always
    /// showing a full rack regardless of ammo remaining. Ported from the pre-pivot
    /// project near-verbatim.
    /// </summary>
    public class MountedMissileVisuals : MonoBehaviour
    {
        private readonly List<Transform> _mountedVisuals = new List<Transform>();
        private WeaponController _weapon;

        public void Initialize(WeaponController weapon, List<Transform> mountedVisuals)
        {
            _weapon = weapon;
            _mountedVisuals.Clear();
            if (mountedVisuals != null)
                _mountedVisuals.AddRange(mountedVisuals);

            if (_weapon != null)
                _weapon.OnFired += HandleFired;
        }

        private void OnDestroy()
        {
            if (_weapon != null)
                _weapon.OnFired -= HandleFired;
        }

        /// <summary>
        /// Removes the most recently added still-alive mounted visual. Which
        /// physical hardpoint "empties first" is cosmetic — there's no real store-
        /// management simulation here, just "one fewer missile visible per shot."
        /// </summary>
        private void HandleFired()
        {
            for (int i = _mountedVisuals.Count - 1; i >= 0; i--)
            {
                if (_mountedVisuals[i] == null)
                    continue;
                Destroy(_mountedVisuals[i].gameObject);
                _mountedVisuals.RemoveAt(i);
                return;
            }
        }

        /// <summary>Number of still-alive mounted visuals — exposed for HUD/tooling readouts and tests.</summary>
        public int MountedCount
        {
            get
            {
                int count = 0;
                foreach (Transform visual in _mountedVisuals)
                {
                    if (visual != null)
                        count++;
                }
                return count;
            }
        }
    }
}
