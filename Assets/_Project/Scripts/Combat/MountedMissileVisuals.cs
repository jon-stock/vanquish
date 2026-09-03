using System.Collections.Generic;
using UnityEngine;

namespace Vanquish.Combat
{
    /// <summary>
    /// Phase 3B: keeps a drone's visibly-mounted missile models in sync with its
    /// actual remaining ammo — removing one mounted missile visual each time
    /// WeaponController.Fire() succeeds, so a drone that took off with 4 missiles
    /// visibly shows 3, then 2, then 1, then none as it expends them in combat,
    /// instead of always showing a full rack regardless of ammo remaining (the
    /// literal "if a craft has 3 missiles, show 3; if 5, show 5" ask this exists to
    /// satisfy — dynamically, not just at spawn time). Added by VehicleFactory.
    /// SpawnDrone alongside the mounted missile visuals themselves; harmless no-op
    /// for drones with nothing mounted (unarmed scouts, or an internal weapon bay —
    /// see WeaponBayDefinition.isInternal).
    /// </summary>
    public class MountedMissileVisuals : MonoBehaviour
    {
        private readonly List<Transform> _mountedVisuals = new();
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
    }
}
