using System.Collections.Generic;
using UnityEngine;
using Vanquish.Core;

namespace Vanquish.Simulation.Sensors
{
    /// <summary>
    /// Probabilistic detection sensor (Phase 2C — was a Phase 0 binary detect/no-detect
    /// prototype). Detection chance falls off smoothly with distance/RCS instead of a
    /// hard cutoff, a lost contact isn't dropped instantly (see reacquisitionGraceSeconds,
    /// fed from SeekerDefinition.reacquisitionTimeSeconds for missiles), and nearby
    /// enemy jamming (JammerSource) degrades detection chance, offset by this sensor's
    /// own jamResistance. Still no field-of-view/line-of-sight check — that remains a
    /// future refinement, not part of this pass.
    /// </summary>
    public class DetectionSensor : MonoBehaviour
    {
        [Tooltip("Maximum detection range in meters at reference RCS of 1 m^2. Detection probability is " +
            "1.0 at zero distance, falling smoothly to 0.0 at this range (see ComputeDetectionProbability) " +
            "— there's no sense in which a target 'just outside' this range is still detectable at all.")]
        public float baseRangeMeters = 5000f;

        [Tooltip("Detected targets are re-scanned this often, in seconds.")]
        public float scanIntervalSeconds = 0.5f;

        [Tooltip("Which team this sensor belongs to — used to filter contacts to enemies only, feed " +
            "TeamAwareness, and decide which JammerSources count as hostile jamming against this sensor.")]
        public Team ownerTeam = Team.Player;

        [Tooltip("How long a contact remains 'known' after a scan fails to re-detect it, in seconds, " +
            "before it's actually dropped — represents a tracker not instantly losing lock on a single " +
            "missed return. Set from SeekerDefinition.reacquisitionTimeSeconds for missiles (previously " +
            "unused dead data — see PLAN.md Phase 2C); left at a short default for drones' general-purpose " +
            "sensor suites, which have no equivalent per-part stat yet.")]
        public float reacquisitionGraceSeconds = 0.5f;

        [Tooltip("Resistance to enemy jamming, 0-1 — offsets incoming JammerSource.jammingStrength before " +
            "it reduces detection probability. Set from MissileRuntimeStats.jamResistance for missiles " +
            "(SeekerDefinition.jamResistance + any JammingDefinition.counterJammingStrength); left at 0 " +
            "for drones, which have no jamming/ECCM part slot yet.")]
        [Range(0f, 1f)]
        public float jamResistance;

        public IReadOnlyList<DetectableSignature> CurrentContacts => _currentContacts;

        private readonly List<DetectableSignature> _currentContacts = new List<DetectableSignature>();
        private readonly Dictionary<DetectableSignature, float> _timeSinceLastDetection = new Dictionary<DetectableSignature, float>();
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

        /// <summary>
        /// Detection probability curve: 1.0 at distance=0, falling to 0.0 at
        /// distance=effectiveRange (and clamped there for anything beyond), using a
        /// quadratic falloff so a target stays confidently detectable through most of
        /// the effective range and only becomes unreliable near the edge, rather than
        /// a straight linear taper from the very first meter. Pure function, exposed
        /// for headless validation.
        /// </summary>
        public static float ComputeDetectionProbability(float distance, float effectiveRange)
        {
            if (effectiveRange <= 0f)
                return 0f;
            float ratio = Mathf.Clamp01(distance / effectiveRange);
            return 1f - ratio * ratio;
        }

        private void Rescan()
        {
            // Find the strongest hostile jamming affecting this sensor's *receiver*
            // (i.e. distance from the jammer to this sensor, not to any particular
            // contact — broadband ECM degrades the whole receiver, not one target's
            // return specifically) before evaluating any contacts, so every contact
            // this scan sees the same jamming state.
            //
            // Phase 0/2 performance note (same brute-force pattern as the
            // DetectableSignature scan below): replace both with a spatial query
            // once contact/jammer counts grow large enough to matter.
            float incomingJamStrength = 0f;
            var jammers = FindObjectsByType<JammerSource>(FindObjectsSortMode.None);
            foreach (var jammer in jammers)
            {
                if (jammer.team == ownerTeam)
                    continue;
                float jamDistance = Vector3.Distance(transform.position, jammer.transform.position);
                if (jamDistance <= jammer.jammingRangeMeters)
                    incomingJamStrength = Mathf.Max(incomingJamStrength, jammer.jammingStrength);
            }
            float effectiveJamming = Mathf.Clamp01(incomingJamStrength - jamResistance);
            float jammingProbabilityMultiplier = 1f - effectiveJamming;

            var allSignatures = FindObjectsByType<DetectableSignature>(FindObjectsSortMode.None);
            var seenThisScan = new HashSet<DetectableSignature>();

            foreach (var signature in allSignatures)
            {
                if (signature.gameObject == gameObject)
                    continue;

                seenThisScan.Add(signature);

                IDetectable detectable = signature;
                float distance = Vector3.Distance(transform.position, detectable.Position);

                // Effective range scales with sqrt(RCS) as a simple stand-in for the
                // real radar range equation (range ~ RCS^0.25 in reality; simplified
                // here for Phase 0/2 tuning purposes).
                float effectiveRange = baseRangeMeters * Mathf.Sqrt(Mathf.Max(0.01f, detectable.RadarCrossSection));

                float probability = ComputeDetectionProbability(distance, effectiveRange) * jammingProbabilityMultiplier;
                bool detectedThisScan = Random.value < probability;

                if (detectedThisScan)
                {
                    _timeSinceLastDetection[signature] = 0f;
                    if (!_currentContacts.Contains(signature))
                        _currentContacts.Add(signature);
                    continue;
                }

                // Missed this scan. If it was a known contact, give it a grace period
                // (reacquisitionGraceSeconds) before actually dropping it — a single
                // missed return shouldn't instantly erase a tracked contact.
                if (_timeSinceLastDetection.TryGetValue(signature, out float timeSinceDetected))
                {
                    timeSinceDetected += scanIntervalSeconds;
                    if (timeSinceDetected > reacquisitionGraceSeconds)
                    {
                        _currentContacts.Remove(signature);
                        _timeSinceLastDetection.Remove(signature);
                    }
                    else
                    {
                        _timeSinceLastDetection[signature] = timeSinceDetected;
                    }
                }
            }

            // Clean up bookkeeping for anything that no longer exists in the scene at
            // all (destroyed since last scan) rather than leaving a stale dictionary
            // entry that can never be reached again.
            if (_timeSinceLastDetection.Count > 0)
            {
                var stale = new List<DetectableSignature>();
                foreach (var key in _timeSinceLastDetection.Keys)
                {
                    if (key == null || !seenThisScan.Contains(key))
                        stale.Add(key);
                }
                foreach (var key in stale)
                {
                    _timeSinceLastDetection.Remove(key);
                    _currentContacts.Remove(key);
                }
            }
        }
    }
}
