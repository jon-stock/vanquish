using UnityEngine;

namespace Vanquish.Combat
{
    /// <summary>
    /// Phase 0 test-harness script: moves a target drone in a simple constant-velocity
    /// or weaving pattern so the pursuit guidance prototype has something non-trivial
    /// to intercept. Not part of the final game's AI — purely for validating the
    /// flight/guidance/detection prototypes.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TargetMover : MonoBehaviour
    {
        public float speedMetersPerSecond = 20f;
        public bool weave = true;
        public float weaveAmplitude = 5f;
        public float weaveFrequency = 0.25f;

        private Rigidbody _rigidbody;
        private Vector3 _forward;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _forward = transform.forward;
        }

        private void FixedUpdate()
        {
            Vector3 velocity = _forward * speedMetersPerSecond;

            if (weave)
            {
                float lateral = Mathf.Sin(Time.time * weaveFrequency * Mathf.PI * 2f) * weaveAmplitude;
                velocity += transform.right * lateral;
            }

            _rigidbody.linearVelocity = velocity;
        }
    }
}
