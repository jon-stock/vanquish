using UnityEngine;
using Vanquish.Combat;
using Vanquish.Simulation.Sensors;

namespace Vanquish.AI
{
    /// <summary>
    /// Phase 2D: the third CPU archetype — static (or minimally-mobile) base defense.
    /// Deliberately does NOT extend DroneCombatAI: that base class requires a
    /// FlightBody/Rigidbody and implements a patrol-vs-engage steering loop, neither of
    /// which applies to a site that never moves — per this item's own instruction, "a
    /// simple 'engage anything in range' controller rather than patrol/pursuit logic."
    /// This is intentionally the simplest archetype in the project: no target
    /// prioritization policy at all (just nearest known enemy of its own team), no
    /// movement, just "is anything in range and can I fire — then fire."
    /// </summary>
    public class SamSiteAI : MonoBehaviour
    {
        [Tooltip("Typically set from BaseDefenseDefinition.engagementRangeMeters at spawn time by " +
            "InstallationFactory.SpawnBaseDefense; exposed here too so it's tunable/visible per-instance.")]
        public float engagementRangeMeters = 1500f;

        private WeaponController _weapon;
        private DetectableSignature _signature;

        private void Awake()
        {
            _weapon = GetComponent<WeaponController>();
            _signature = GetComponent<DetectableSignature>();
        }

        private void Update()
        {
            if (_weapon == null || _signature == null || TeamAwareness.Instance == null)
                return;

            DetectableSignature target = TeamAwareness.Instance.GetNearestKnownEnemy(_signature.team, transform.position);
            if (target == null)
                return;

            float distance = Vector3.Distance(transform.position, target.Position);
            if (distance <= engagementRangeMeters && _weapon.CanFire)
                _weapon.Fire(target.transform);
        }
    }
}
