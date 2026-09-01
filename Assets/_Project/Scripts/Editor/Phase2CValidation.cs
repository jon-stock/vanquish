using UnityEditor;
using UnityEngine;
using Vanquish.Simulation.Guidance;
using Vanquish.Simulation.Sensors;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Headless sanity checks for Phase 2C's guidance and sensor depth. Guidance law
    /// comparison uses a pure-C# kinematic simulator (no scene/Play mode/Physics
    /// required) so it's fast, deterministic, and runnable via a single
    /// -executeMethod call — mirrors the "missile vs. weaving target" scenario
    /// Phase 0 validated PursuitGuidance against, per Phase 2C's own technical note
    /// ("Validate headlessly the same way PursuitGuidance was validated in Phase 0").
    /// </summary>
    public static class Phase2CValidation
    {
        [MenuItem("Vanquish/Phase 2C/Validate Guidance Laws Vs Weaving Target (Headless)")]
        public static void ValidateGuidanceLaws()
        {
            var pursuitResult = SimulateIntercept(new PursuitGuidance());
            var pnResult = SimulateIntercept(new ProportionalNavigation());

            Debug.Log($"[Phase2CValidation] Pursuit vs weaving target: minDistance={pursuitResult.minDistance:F1}m, " +
                $"hit={pursuitResult.hit} (at t={pursuitResult.hitTime:F2}s)");
            Debug.Log($"[Phase2CValidation] Proportional Navigation vs weaving target: minDistance={pnResult.minDistance:F1}m, " +
                $"hit={pnResult.hit} (at t={pnResult.hitTime:F2}s)");

            // Both should be dramatically better than an unguided/no-correction
            // baseline (sanity: guidance is doing *something*).
            var noGuidanceResult = SimulateIntercept(null);
            Debug.Log($"[Phase2CValidation] No guidance (ballistic) vs weaving target: minDistance={noGuidanceResult.minDistance:F1}m, " +
                $"hit={noGuidanceResult.hit}");

            bool bothBeatBaseline = pursuitResult.minDistance < noGuidanceResult.minDistance
                && pnResult.minDistance < noGuidanceResult.minDistance;
            Debug.Log($"[Phase2CValidation] Both guidance laws beat the unguided baseline: {(bothBeatBaseline ? "PASS" : "FAIL")}");

            // PN's whole value proposition (per PLAN.md) is out-intercepting pure
            // pursuit against a maneuvering (weaving) target at the same tuning
            // effort — pursuit chronically lags behind a turning target's position,
            // PN leads the intercept point instead.
            bool pnBeatsPursuit = pnResult.minDistance <= pursuitResult.minDistance;
            Debug.Log($"[Phase2CValidation] PN out-intercepts (or ties) pure pursuit against a weaving target: " +
                $"{(pnBeatsPursuit ? "PASS" : "FAIL")} (PN={pnResult.minDistance:F1}m vs Pursuit={pursuitResult.minDistance:F1}m)");

            bool bothHit = pursuitResult.hit && pnResult.hit;
            Debug.Log($"[Phase2CValidation] Both guidance laws achieve a hit (min distance < hit threshold): {(bothHit ? "PASS" : "FAIL")}");

            bool allPass = bothBeatBaseline && pnBeatsPursuit && bothHit;
            Debug.Log(allPass
                ? "[Phase2CValidation] Guidance law comparison: ALL PASS"
                : "[Phase2CValidation] Guidance law comparison: ONE OR MORE FAILURES ABOVE");
            if (!allPass)
                Debug.LogError("[Phase2CValidation] Guidance law validation FAILED.");
        }

        [MenuItem("Vanquish/Phase 2C/Validate Datalink Mid-Course Handoff (Headless)")]
        public static void ValidateDatalinkMidCourseHandoff()
        {
            // A datalink+PN missile should behave identically to plain PN once the
            // target is within the (simulated) seeker's terminal range, and should
            // still eventually hit even though it only sees a stale, periodically-
            // updated position while outside that range.
            const float terminalRangeMeters = 2000f;
            var datalinkGuidance = new DatalinkMidCourseGuidance(new PursuitGuidance(), new ProportionalNavigation(),
                terminalRangeMeters, updateIntervalSeconds: 2f);

            var datalinkResult = SimulateIntercept(datalinkGuidance, initialRangeMeters: 6000f);
            Debug.Log($"[Phase2CValidation] Datalink+PN vs weaving target from 6000m: minDistance={datalinkResult.minDistance:F1}m, " +
                $"hit={datalinkResult.hit}, handedOffToTerminal={datalinkGuidance.HasHandedOffToTerminalSeeker}");

            bool handedOff = datalinkGuidance.HasHandedOffToTerminalSeeker;
            bool hit = datalinkResult.hit;
            Debug.Log($"[Phase2CValidation] Datalink missile hands off to terminal seeker before impact: {(handedOff ? "PASS" : "FAIL")}");
            Debug.Log($"[Phase2CValidation] Datalink+PN missile still achieves a hit from long range: {(hit ? "PASS" : "FAIL")}");

            bool allPass = handedOff && hit;
            Debug.Log(allPass
                ? "[Phase2CValidation] Datalink mid-course handoff: ALL PASS"
                : "[Phase2CValidation] Datalink mid-course handoff: ONE OR MORE FAILURES ABOVE");
            if (!allPass)
                Debug.LogError("[Phase2CValidation] Datalink mid-course handoff validation FAILED.");
        }

        [MenuItem("Vanquish/Phase 2C/Validate Detection Probability & Jamming Math (Headless)")]
        public static void ValidateDetectionAndJammingMath()
        {
            bool allPass = true;

            float pClose = DetectionSensor.ComputeDetectionProbability(distance: 0f, effectiveRange: 1000f);
            float pMid = DetectionSensor.ComputeDetectionProbability(distance: 500f, effectiveRange: 1000f);
            float pEdge = DetectionSensor.ComputeDetectionProbability(distance: 1000f, effectiveRange: 1000f);
            float pBeyond = DetectionSensor.ComputeDetectionProbability(distance: 1500f, effectiveRange: 1000f);

            bool closeOk = Mathf.Approximately(pClose, 1f);
            bool edgeOk = Mathf.Approximately(pEdge, 0f);
            bool beyondOk = Mathf.Approximately(pBeyond, 0f);
            bool monotonicOk = pClose > pMid && pMid > pEdge;

            Debug.Log($"[Phase2CValidation] Detection probability: at 0m={pClose:F2} (expect 1.0), at 500m={pMid:F2}, " +
                $"at range edge (1000m)={pEdge:F2} (expect 0.0), beyond range (1500m)={pBeyond:F2} (expect 0.0).");
            Debug.Log($"[Phase2CValidation] Probability curve sane (1.0 at 0, falls to 0.0 at/beyond range, monotonic): " +
                $"{(closeOk && edgeOk && beyondOk && monotonicOk ? "PASS" : "FAIL")}");
            allPass &= closeOk && edgeOk && beyondOk && monotonicOk;

            // Jamming math: this mirrors DetectionSensor.Rescan's
            // effectiveJamming/jammingProbabilityMultiplier computation directly,
            // since that logic lives inline in Rescan rather than as its own pure
            // function (Rescan itself needs live scene objects, so it isn't
            // headlessly callable without a scene — this validates the same formula
            // in isolation instead).
            float strongJamWeakResistance = ComputeJammingMultiplier(incomingJamStrength: 0.8f, jamResistance: 0.2f);
            float weakJamStrongResistance = ComputeJammingMultiplier(incomingJamStrength: 0.3f, jamResistance: 0.9f);
            float noJam = ComputeJammingMultiplier(incomingJamStrength: 0f, jamResistance: 0f);

            bool strongJamReducesDetection = strongJamWeakResistance < 1f && strongJamWeakResistance > 0f;
            bool strongResistanceFullyOffsets = Mathf.Approximately(weakJamStrongResistance, 1f);
            bool noJamNoEffect = Mathf.Approximately(noJam, 1f);

            Debug.Log($"[Phase2CValidation] Jamming multiplier: strong jam (0.8) vs weak resistance (0.2) = " +
                $"{strongJamWeakResistance:F2} (expect ~0.4, reduced detection); weak jam (0.3) vs strong resistance (0.9) = " +
                $"{weakJamStrongResistance:F2} (expect 1.0, fully offset); no jam = {noJam:F2} (expect 1.0).");
            Debug.Log($"[Phase2CValidation] Jamming reduces detection when it exceeds resistance: {(strongJamReducesDetection ? "PASS" : "FAIL")}");
            Debug.Log($"[Phase2CValidation] Counter-jamming fully offsets weaker jamming: {(strongResistanceFullyOffsets ? "PASS" : "FAIL")}");
            Debug.Log($"[Phase2CValidation] No jamming present has no effect: {(noJamNoEffect ? "PASS" : "FAIL")}");
            allPass &= strongJamReducesDetection && strongResistanceFullyOffsets && noJamNoEffect;

            Debug.Log(allPass
                ? "[Phase2CValidation] Detection & jamming math: ALL PASS"
                : "[Phase2CValidation] Detection & jamming math: ONE OR MORE FAILURES ABOVE");
            if (!allPass)
                Debug.LogError("[Phase2CValidation] Detection & jamming math validation FAILED.");
        }

        [MenuItem("Vanquish/Phase 2C/Validate Countermeasure Decoy Rolls (Headless)")]
        public static void ValidateCountermeasureDecoys()
        {
            var go = new GameObject("Phase2CValidation_TempCountermeasure");
            try
            {
                var countermeasures = go.AddComponent<CountermeasureController>();
                countermeasures.decoyChargesRemaining = 3;
                countermeasures.decoySuccessChance = 1f; // guaranteed success for a deterministic check

                bool firstDeploy = countermeasures.TryDeployDecoy();
                bool secondDeploy = countermeasures.TryDeployDecoy();
                bool thirdDeploy = countermeasures.TryDeployDecoy();
                bool fourthDeployAfterChargesExhausted = countermeasures.TryDeployDecoy();

                Debug.Log($"[Phase2CValidation] Decoy deploys with 3 charges @ 100% chance: " +
                    $"1st={firstDeploy}, 2nd={secondDeploy}, 3rd={thirdDeploy}, 4th (should fail, no charges left)={fourthDeployAfterChargesExhausted}, " +
                    $"chargesRemaining={countermeasures.decoyChargesRemaining} (expect 0).");

                bool allThreeSucceeded = firstDeploy && secondDeploy && thirdDeploy;
                bool fourthCorrectlyFailed = !fourthDeployAfterChargesExhausted;
                bool chargesDepleted = countermeasures.decoyChargesRemaining == 0;

                Debug.Log($"[Phase2CValidation] All 3 charges succeed at 100% chance: {(allThreeSucceeded ? "PASS" : "FAIL")}");
                Debug.Log($"[Phase2CValidation] 4th deploy correctly fails once charges are exhausted: {(fourthCorrectlyFailed ? "PASS" : "FAIL")}");
                Debug.Log($"[Phase2CValidation] Charges correctly depleted to 0: {(chargesDepleted ? "PASS" : "FAIL")}");

                var alwaysFailCountermeasures = go.AddComponent<CountermeasureController>();
                alwaysFailCountermeasures.decoyChargesRemaining = 5;
                alwaysFailCountermeasures.decoySuccessChance = 0f; // guaranteed failure
                bool zeroChanceDeploy = alwaysFailCountermeasures.TryDeployDecoy();
                bool zeroChanceCorrectlyFailed = !zeroChanceDeploy;
                Debug.Log($"[Phase2CValidation] 0% success chance deploy correctly fails to break lock: {(zeroChanceCorrectlyFailed ? "PASS" : "FAIL")}");

                bool allPass = allThreeSucceeded && fourthCorrectlyFailed && chargesDepleted && zeroChanceCorrectlyFailed;
                Debug.Log(allPass
                    ? "[Phase2CValidation] Countermeasure decoy rolls: ALL PASS"
                    : "[Phase2CValidation] Countermeasure decoy rolls: ONE OR MORE FAILURES ABOVE");
                if (!allPass)
                    Debug.LogError("[Phase2CValidation] Countermeasure decoy validation FAILED.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [MenuItem("Vanquish/Phase 2C/Validate All Guidance And Sensor Depth (Headless)")]
        public static void ValidateAll()
        {
            ValidateGuidanceLaws();
            ValidateDatalinkMidCourseHandoff();
            ValidateDetectionAndJammingMath();
            ValidateCountermeasureDecoys();
        }

        /// <summary>Mirrors DetectionSensor.Rescan's inline jamming-multiplier formula for
        /// headless testing without needing a live scene of JammerSource/DetectionSensor objects.</summary>
        private static float ComputeJammingMultiplier(float incomingJamStrength, float jamResistance)
        {
            float effectiveJamming = Mathf.Clamp01(incomingJamStrength - jamResistance);
            return 1f - effectiveJamming;
        }

        private struct InterceptResult
        {
            public float minDistance;
            public bool hit;
            public float hitTime;
        }

        /// <summary>
        /// Pure-C# kinematic simulation of a missile chasing a weaving target,
        /// mirroring FlightBody's physics model (constant thrust along current
        /// heading, quadratic drag, steering clamped to maxG, heading chases
        /// velocity) closely enough to validate guidance law behavior without
        /// needing Play mode/Physics/a scene at all. guidanceLaw == null simulates an
        /// unguided/ballistic body (no steering correction at all) as a baseline.
        /// </summary>
        private static InterceptResult SimulateIntercept(IGuidanceLaw guidanceLaw, float initialRangeMeters = 3000f)
        {
            const float hitThresholdMeters = 15f;
            const float dt = 0.02f; // 50Hz, matches Unity's default fixed timestep
            const float durationSeconds = 30f;
            const float missileMass = 25f;
            const float thrustNewtons = 3500f;
            const float dragCoefficient = 0.08f;
            const float maxGForce = 25f;
            const float gravity = 9.81f;

            const float targetSpeed = 40f;
            // Deliberately larger than hitThresholdMeters so a straight-line/no-
            // guidance shot at the target's initial position reliably misses —
            // hitting requires actually tracking the target's lateral motion, which
            // is the whole point of this comparison (a smaller weave than the hit
            // threshold made every guidance law — including "none" — trivially
            // "hit" by construction in an earlier version of this test).
            const float weaveAmplitude = 40f;
            const float weaveFrequencyHz = 0.15f;

            // Launched with a real lateral offset and a heading that does NOT already
            // point at the target — otherwise a dead-on head-on engagement needs
            // negligible correction regardless of guidance quality, which also masked
            // guidance-law differences in an earlier version of this test.
            const float lateralOffsetMeters = 250f;

            Vector3 missilePosition = Vector3.zero;
            Vector3 missileVelocity = new Vector3(0f, 0f, 50f); // launched straight along +Z, not aimed at the target
            Vector3 targetPosition = new Vector3(lateralOffsetMeters, 0f, initialRangeMeters);
            Vector3 targetBaseVelocity = new Vector3(0f, 0f, -targetSpeed); // closing head-on

            float minDistance = float.MaxValue;
            bool hit = false;
            float hitTime = 0f;

            for (float t = 0f; t < durationSeconds; t += dt)
            {
                // Weaving target motion: constant forward velocity plus a sinusoidal
                // lateral weave, same conceptual pattern as Phase 0's TargetMover.
                float weaveOffset = weaveAmplitude * Mathf.Sin(2f * Mathf.PI * weaveFrequencyHz * t);
                float weaveVelocityX = weaveAmplitude * 2f * Mathf.PI * weaveFrequencyHz * Mathf.Cos(2f * Mathf.PI * weaveFrequencyHz * t);
                Vector3 targetVelocity = targetBaseVelocity + new Vector3(weaveVelocityX, 0f, 0f);
                targetPosition += targetVelocity * dt;
                Vector3 weavePosition = targetPosition + new Vector3(weaveOffset, 0f, 0f);

                float distance = Vector3.Distance(missilePosition, weavePosition);
                if (distance < minDistance)
                    minDistance = distance;
                if (distance <= hitThresholdMeters && !hit)
                {
                    hit = true;
                    hitTime = t;
                    break;
                }

                if (guidanceLaw != null)
                {
                    Vector3 steering = guidanceLaw.ComputeSteering(missilePosition, missileVelocity, weavePosition, targetVelocity, dt);
                    float maxAccel = maxGForce * gravity;
                    Vector3 clampedSteering = Vector3.ClampMagnitude(steering, maxAccel);
                    missileVelocity += clampedSteering * dt;
                }

                // FlightBody-equivalent physics: constant thrust along current heading,
                // quadratic drag, heading re-orients toward velocity (orientToVelocity).
                Vector3 heading = missileVelocity.sqrMagnitude > 0.0001f ? missileVelocity.normalized : Vector3.forward;
                Vector3 thrustAccel = heading * (thrustNewtons / missileMass);
                float speed = missileVelocity.magnitude;
                Vector3 dragAccel = speed > 0.01f ? -missileVelocity.normalized * (dragCoefficient * speed * speed / missileMass) : Vector3.zero;

                missileVelocity += (thrustAccel + dragAccel) * dt;
                missilePosition += missileVelocity * dt;
            }

            return new InterceptResult { minDistance = minDistance, hit = hit, hitTime = hitTime };
        }
    }
}
