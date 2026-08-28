using System.Collections.Generic;
using UnityEngine;
using Vanquish.Core;

namespace Vanquish.Simulation.Sensors
{
    /// <summary>
    /// Phase 0 prototype detection sensor: binary detect/no-detect based on range and
    /// a minimum-RCS threshold, no field-of-view or line-of-sight check yet. Used to
    /// validate the IDetectable contract before Phase 2 adds probability-based
    /// detection, field-of-view cones, and jamming interaction.
    /// </summary>
    public class DetectionSensor : MonoBehaviour
    {
        [Tooltip("Maximum detection range in meters at reference RCS of 1 m^2.")]
        public float baseRangeMeters = 5000f;

        [Tooltip("Detected targets are re-scanned this often, in seconds.")]
        public float scanIntervalSeconds = 0.5f;

        [Tooltip("Which team this sensor belongs to — used to filter contacts to enemies only, and to feed TeamAwareness.")]
        public Team ownerTeam = Team.Player;

        public IReadOnlyList<DetectableSignature> CurrentContacts => _currentContacts;

        private readonly List<DetectableSignature> _currentContacts = new List<DetectableSignature>();
        private float _scanTimer;

        /// <summary>Contacts belonging to a different team than this sensor's owner.</summary>
        public IEnumerable<DetectableSignature> EnemyContacts
        {
            get
            {
                foreach (var contact in _currentContacts)
                    if (contact.team != ownerTeam)
                        yield return contact;
            }
        }

        private void Update()
        {
            _scanTimer -= Time.deltaTime;
            if (_scanTimer <= 0f)
            {
                _scanTimer = scanIntervalSeconds;
                Rescan();
            }
        }

        private void Rescan()
        {
            _currentContacts.Clear();

            // Phase 0: brute-force scan of all DetectableSignature components in the scene.
            // Replace with a spatial partitioning / physics overlap query once contact
            // counts grow large enough to matter (Phase 2 performance pass).
            var allSignatures = FindObjectsByType<DetectableSignature>(FindObjectsSortMode.None);
            foreach (var signature in allSignatures)
            {
                if (signature.gameObject == gameObject)
                    continue;

                IDetectable detectable = signature;
                float distance = Vector3.Distance(transform.position, detectable.Position);

                // Effective range scales with sqrt(RCS) as a simple stand-in for the
                // real radar range equation (range ~ RCS^0.25 in reality; simplified
                // here for Phase 0 tuning purposes).
                float effectiveRange = baseRangeMeters * Mathf.Sqrt(Mathf.Max(0.01f, detectable.RadarCrossSection));

                if (distance <= effectiveRange)
                {
                    _currentContacts.Add(signature);
                }
            }
        }
    }
}
