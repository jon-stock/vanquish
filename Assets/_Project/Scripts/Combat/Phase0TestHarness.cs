using UnityEngine;
using Vanquish.Simulation.Flight;
using Vanquish.Simulation.Sensors;

namespace Vanquish.Combat
{
    /// <summary>
    /// Phase 0 exit-criteria validation script. Periodically logs missile-to-target
    /// distance and whether the missile's DetectionSensor currently has the target
    /// as a contact, plus reports a timeout ("MISS") if no impact occurs in time.
    /// Remove/replace once Phase 1 combat scenes have proper HUD/telemetry.
    /// </summary>
    public class Phase0TestHarness : MonoBehaviour
    {
        public Transform missile;
        public Transform target;
        public DetectionSensor missileSensor;
        public MissileImpact missileImpact;

        public float logIntervalSeconds = 0.25f;
        public float missTimeoutSeconds = 30f;

        private float _logTimer;
        private float _elapsed;
        private bool _reported;
        private Rigidbody _missileRb;
        private Rigidbody _targetRb;
        private FlightBody _flightBody;

        private void Start()
        {
            if (missile != null)
            {
                _missileRb = missile.GetComponent<Rigidbody>();
                _flightBody = missile.GetComponent<FlightBody>();
            }
            if (target != null)
                _targetRb = target.GetComponent<Rigidbody>();

            Debug.Log($"[Phase0Test] START missile.pos={missile.position} target.pos={target.position} " +
                      $"missile.useGravity={_missileRb?.useGravity} flightBody.thrust={_flightBody?.thrustNewtons} " +
                      $"flightBody.drag={_flightBody?.dragCoefficient} flightBody.maxG={_flightBody?.maxGForce} " +
                      $"flightBody.mass={_flightBody?.massKg}");
        }

        private void Update()
        {
            if (_reported || missile == null || target == null)
                return;

            _elapsed += Time.deltaTime;
            _logTimer += Time.deltaTime;

            if (missileImpact != null && missileImpact.hasImpacted)
            {
                _reported = true;
                Debug.Log("[Phase0Test] RESULT: SUCCESS — missile intercepted target.");
                return;
            }

            if (_elapsed >= missTimeoutSeconds)
            {
                _reported = true;
                Debug.Log($"[Phase0Test] RESULT: MISS — no impact within {missTimeoutSeconds}s timeout.");
                return;
            }

            if (_logTimer >= logIntervalSeconds)
            {
                _logTimer = 0f;
                float distance = Vector3.Distance(missile.position, target.position);
                bool detected = false;
                if (missileSensor != null)
                {
                    foreach (var contact in missileSensor.CurrentContacts)
                    {
                        if (Vector3.Distance(contact.Position, target.position) < 0.5f)
                        {
                            detected = true;
                            break;
                        }
                    }
                }

                float missileSpeed = _missileRb != null ? _missileRb.linearVelocity.magnitude : -1f;
                float targetSpeed = _targetRb != null ? _targetRb.linearVelocity.magnitude : -1f;

                Debug.Log($"[Phase0Test] t={_elapsed:F1}s distance={distance:F1}m detected={detected} " +
                          $"missile.pos={missile.position} missile.speed={missileSpeed:F1} missile.fwd={missile.forward} " +
                          $"target.pos={target.position} target.speed={targetSpeed:F1}");
            }
        }
    }
}
