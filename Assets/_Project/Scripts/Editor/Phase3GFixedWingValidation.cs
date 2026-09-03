using UnityEditor;
using UnityEngine;
using Vanquish.Combat;
using Vanquish.Data.Drones;
using Vanquish.Simulation.Flight;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Headless sanity checks for the fixed-wing flight-model rework — same pattern as
    /// every other PhaseXValidation: exercise the real production pure functions
    /// (FlightBody.ComputeAngleOfAttackDegrees/ComputeLiftFactor,
    /// PlayerDroneController.ComputeControlAuthority, DroneCompatibility) directly,
    /// logging PASS/FAIL for a human or a batchmode CI log grep. No Play mode/Physics
    /// needed for most of these — like Phase2CValidation's guidance-law kinematic
    /// simulator, ValidateCoordinatedTurn reproduces FlightBody/PlayerDroneController's
    /// exact force/rotation math in a plain C# loop (calling the same static functions
    /// the real MonoBehaviours call) rather than needing a live scene.
    /// Run via `Unity.exe -batchmode -quit -executeMethod
    /// Vanquish.EditorTools.Phase3GFixedWingValidation.&lt;MethodName&gt;`.
    /// </summary>
    public static class Phase3GFixedWingValidation
    {
        // Mirrors Phase3GFixedWingPrototypeSceneBuilder's hand-tuned rig exactly, so a
        // failure here is directly meaningful about the actual prototype rig a human
        // would fly, not an arbitrary/unrelated set of numbers.
        private const float Mass = 20f;
        private const float DragCoefficient = 0.05f;
        private const float MaxGForce = 8f;
        private const float ZeroLiftAoA = -2f;
        private const float ReferenceAoA = 5f;
        private const float CriticalAoA = 15f;
        private const float InducedDragFactor = 0.02f;
        private const float CruiseSpeed = 25f;
        private const float ThrustNewtons = 180f;
        private const float ControlAuthorityReferenceSpeed = 35f;
        private const float RollRateDegPerSec = 120f;
        private const float PitchRateDegPerSec = 60f;
        private const float VelocityAlignmentStrength = 2f;
        private const float Gravity = 9.81f;

        private static float LiftCoefficient => (Mass * Gravity) / (CruiseSpeed * CruiseSpeed);

        [MenuItem("Vanquish/Phase 3G/Validate Fixed-Wing Flight Model (Headless)")]
        public static void ValidateAll()
        {
            bool allPass = true;
            allPass &= ValidateLiftCurveShape();
            allPass &= ValidateAngleOfAttackSign();
            allPass &= ValidateTrimLevelFlight();
            allPass &= ValidateStallAtLowSpeed();
            allPass &= ValidateBankedTurnRedirectsLift();
            allPass &= ValidateControlAuthorityScalesWithAirspeed();
            allPass &= ValidateCoordinatedTurnSimulation();
            allPass &= ValidateDroneCompatibilityDetectsMismatch();

            Debug.Log(allPass
                ? "[Phase3GFixedWingValidation] All fixed-wing flight-model checks PASSED."
                : "[Phase3GFixedWingValidation] One or more fixed-wing flight-model checks FAILED — see log above.");

            if (!allPass)
                Debug.LogError("[Phase3GFixedWingValidation] Fixed-wing flight-model validation FAILED.");
        }

        [MenuItem("Vanquish/Phase 3G/Validate Lift Curve Shape (Headless)")]
        public static bool ValidateLiftCurveShape()
        {
            bool pass = true;

            float atReference = FlightBody.ComputeLiftFactor(ReferenceAoA, ZeroLiftAoA, ReferenceAoA, CriticalAoA);
            bool referenceOk = Mathf.Approximately(atReference, 1f);
            Debug.Log($"[Phase3GFixedWingValidation] Lift factor at referenceAoA ({ReferenceAoA}deg) = {atReference:F3} (expect 1.0). {(referenceOk ? "PASS" : "FAIL")}");
            pass &= referenceOk;

            float atZeroLift = FlightBody.ComputeLiftFactor(ZeroLiftAoA, ZeroLiftAoA, ReferenceAoA, CriticalAoA);
            bool zeroLiftOk = Mathf.Approximately(atZeroLift, 0f);
            Debug.Log($"[Phase3GFixedWingValidation] Lift factor at zeroLiftAoA ({ZeroLiftAoA}deg) = {atZeroLift:F3} (expect 0.0). {(zeroLiftOk ? "PASS" : "FAIL")}");
            pass &= zeroLiftOk;

            float belowZeroLift = FlightBody.ComputeLiftFactor(ZeroLiftAoA - 3f, ZeroLiftAoA, ReferenceAoA, CriticalAoA);
            bool negativeLiftOk = belowZeroLift < 0f;
            Debug.Log($"[Phase3GFixedWingValidation] Lift factor below zeroLiftAoA = {belowZeroLift:F3} (expect negative/downforce). {(negativeLiftOk ? "PASS" : "FAIL")}");
            pass &= negativeLiftOk;

            float atCritical = FlightBody.ComputeLiftFactor(CriticalAoA, ZeroLiftAoA, ReferenceAoA, CriticalAoA);
            float pastStall = FlightBody.ComputeLiftFactor(CriticalAoA + 10f, ZeroLiftAoA, ReferenceAoA, CriticalAoA);
            bool stallOk = atCritical > atReference && pastStall < atCritical;
            Debug.Log($"[Phase3GFixedWingValidation] Lift factor rises to peak at critical ({atCritical:F3}) then " +
                $"collapses 10deg past stall ({pastStall:F3}). {(stallOk ? "PASS" : "FAIL")}");
            pass &= stallOk;

            bool monotonicOk = FlightBody.ComputeLiftFactor(0f, ZeroLiftAoA, ReferenceAoA, CriticalAoA) <
                                FlightBody.ComputeLiftFactor(3f, ZeroLiftAoA, ReferenceAoA, CriticalAoA);
            Debug.Log($"[Phase3GFixedWingValidation] Lift factor rises monotonically in the pre-stall linear region: {(monotonicOk ? "PASS" : "FAIL")}");
            pass &= monotonicOk;

            return pass;
        }

        [MenuItem("Vanquish/Phase 3G/Validate Angle Of Attack Sign (Headless)")]
        public static bool ValidateAngleOfAttackSign()
        {
            // Nose pitched +10deg above a purely horizontal velocity (a classic
            // "climbing attitude relative to flight path" / positive-AoA case) should
            // report a positive angle of attack. Constructed via a known rotation
            // rather than guessed numbers, so this test is unambiguous about what it's
            // checking regardless of Unity's rotation-direction convention.
            Quaternion pitchedUp = Quaternion.AngleAxis(-10f, Vector3.right); // -10 about +X pitches +Z (forward) upward in Unity's convention
            Vector3 forward = pitchedUp * Vector3.forward;
            Vector3 right = pitchedUp * Vector3.right;
            Vector3 horizontalVelocity = Vector3.forward * 30f;

            float aoa = FlightBody.ComputeAngleOfAttackDegrees(forward, right, horizontalVelocity);
            bool positiveOk = aoa > 5f; // should read close to +10, comfortably positive
            Debug.Log($"[Phase3GFixedWingValidation] AoA with nose pitched +10deg above horizontal velocity = {aoa:F1}deg (expect positive, ~10). {(positiveOk ? "PASS" : "FAIL")}");

            Quaternion pitchedDown = Quaternion.AngleAxis(10f, Vector3.right);
            float aoaDown = FlightBody.ComputeAngleOfAttackDegrees(pitchedDown * Vector3.forward, pitchedDown * Vector3.right, horizontalVelocity);
            bool negativeOk = aoaDown < -5f;
            Debug.Log($"[Phase3GFixedWingValidation] AoA with nose pitched -10deg below horizontal velocity = {aoaDown:F1}deg (expect negative, ~-10). {(negativeOk ? "PASS" : "FAIL")}");

            bool levelOk = Mathf.Abs(FlightBody.ComputeAngleOfAttackDegrees(Vector3.forward, Vector3.right, horizontalVelocity)) < 0.01f;
            Debug.Log($"[Phase3GFixedWingValidation] AoA with nose exactly aligned to velocity = 0deg: {(levelOk ? "PASS" : "FAIL")}");

            return positiveOk && negativeOk && levelOk;
        }

        [MenuItem("Vanquish/Phase 3G/Validate Trim Level Flight (Headless)")]
        public static bool ValidateTrimLevelFlight()
        {
            float liftFactor = FlightBody.ComputeLiftFactor(ReferenceAoA, ZeroLiftAoA, ReferenceAoA, CriticalAoA);
            float liftForce = LiftCoefficient * CruiseSpeed * CruiseSpeed * liftFactor;
            float weight = Mass * Gravity;
            bool pass = Mathf.Abs(liftForce - weight) < 0.5f;
            Debug.Log($"[Phase3GFixedWingValidation] At cruise speed {CruiseSpeed}m/s and referenceAoA, lift={liftForce:F1}N vs weight={weight:F1}N. {(pass ? "PASS" : "FAIL")}");
            return pass;
        }

        [MenuItem("Vanquish/Phase 3G/Validate Stall At Low Speed (Headless)")]
        public static bool ValidateStallAtLowSpeed()
        {
            float lowSpeed = CruiseSpeed * 0.5f;
            float maxLiftFactor = FlightBody.ComputeLiftFactor(CriticalAoA, ZeroLiftAoA, ReferenceAoA, CriticalAoA);
            float bestPossibleLift = LiftCoefficient * lowSpeed * lowSpeed * maxLiftFactor;
            float weight = Mass * Gravity;
            bool pass = bestPossibleLift < weight;
            Debug.Log($"[Phase3GFixedWingValidation] At half cruise speed ({lowSpeed}m/s) even at max (critical) AoA, " +
                $"best possible lift={bestPossibleLift:F1}N < weight={weight:F1}N (cannot sustain level flight — realistic stall/sink): {(pass ? "PASS" : "FAIL")}");
            return pass;
        }

        [MenuItem("Vanquish/Phase 3G/Validate Banked Turn Redirects Lift (Headless)")]
        public static bool ValidateBankedTurnRedirectsLift()
        {
            const float bankAngleDegrees = 30f;

            // Bank the "up" axis around forward by bankAngleDegrees, keeping AoA at
            // referenceAoA (forward pitched referenceAoA above velocity, in the banked
            // plane) so liftFactor stays exactly 1 and this test isolates direction,
            // not magnitude.
            Quaternion bank = Quaternion.AngleAxis(bankAngleDegrees, Vector3.forward);
            Vector3 velocity = Vector3.forward * CruiseSpeed;
            Quaternion pitch = Quaternion.AngleAxis(-ReferenceAoA, Vector3.right);
            Vector3 forward = bank * pitch * Vector3.forward;
            Vector3 up = bank * pitch * Vector3.up;

            float liftMag = LiftCoefficient * CruiseSpeed * CruiseSpeed *
                FlightBody.ComputeLiftFactor(ReferenceAoA, ZeroLiftAoA, ReferenceAoA, CriticalAoA);
            Vector3 liftDirection = Vector3.ProjectOnPlane(up, velocity).normalized;
            Vector3 liftForce = liftDirection * liftMag;

            float horizontalComponent = new Vector3(liftForce.x, 0f, 0f).magnitude; // velocity is along world +Z, bank rotates lift toward world X
            float verticalComponent = liftForce.y;
            float expectedHorizontal = liftMag * Mathf.Sin(bankAngleDegrees * Mathf.Deg2Rad);
            float expectedVertical = liftMag * Mathf.Cos(bankAngleDegrees * Mathf.Deg2Rad);

            bool horizontalOk = Mathf.Abs(horizontalComponent - expectedHorizontal) < 1f;
            bool verticalOk = Mathf.Abs(verticalComponent - expectedVertical) < 1f;
            bool lessVerticalThanUnbanked = verticalComponent < liftMag;

            Debug.Log($"[Phase3GFixedWingValidation] Banked {bankAngleDegrees}deg: lift horizontal component={horizontalComponent:F1}N " +
                $"(expect ~{expectedHorizontal:F1}), vertical component={verticalComponent:F1}N (expect ~{expectedVertical:F1}, " +
                $"less than unbanked {liftMag:F1}N — real 'you lose vertical lift in a bank' behavior). " +
                $"{(horizontalOk && verticalOk && lessVerticalThanUnbanked ? "PASS" : "FAIL")}");

            return horizontalOk && verticalOk && lessVerticalThanUnbanked;
        }

        [MenuItem("Vanquish/Phase 3G/Validate Control Authority Scaling (Headless)")]
        public static bool ValidateControlAuthorityScalesWithAirspeed()
        {
            float atZero = PlayerDroneController.ComputeControlAuthority(0f, ControlAuthorityReferenceSpeed);
            float atReference = PlayerDroneController.ComputeControlAuthority(ControlAuthorityReferenceSpeed, ControlAuthorityReferenceSpeed);
            float aboveReference = PlayerDroneController.ComputeControlAuthority(ControlAuthorityReferenceSpeed * 2f, ControlAuthorityReferenceSpeed);
            float atHalf = PlayerDroneController.ComputeControlAuthority(ControlAuthorityReferenceSpeed * 0.5f, ControlAuthorityReferenceSpeed);

            bool pass = Mathf.Approximately(atZero, 0f) && Mathf.Approximately(atReference, 1f) &&
                        Mathf.Approximately(aboveReference, 1f) && atHalf > 0f && atHalf < atReference;

            Debug.Log($"[Phase3GFixedWingValidation] Control authority: at 0 speed={atZero:F2} (expect 0), " +
                $"at reference speed={atReference:F2} (expect 1), above reference={aboveReference:F2} (expect clamped to 1), " +
                $"at half reference={atHalf:F2} (expect between 0 and 1). {(pass ? "PASS" : "FAIL")}");

            return pass;
        }

        /// <summary>
        /// Reproduces FlightBody.FixedUpdate + PlayerDroneController.FixedUpdateFixedWing's
        /// exact math (same static functions, same force/rotation formulas) in a plain
        /// C# loop — no Play mode/Rigidbody/scene needed, same "faster, fully
        /// deterministic" rationale Phase2CValidation's guidance-law simulator gives.
        /// Holds a constant roll+compensating pitch input (as if a player were holding
        /// a bank-and-pull turn) for a few seconds and confirms: (1) heading actually
        /// turns (a real yaw change occurs, purely as an emergent result of banked lift
        /// + alignVelocityToForward — there is no direct yaw control in this flight
        /// model at all), and (2) the flight path (velocity direction) follows the nose
        /// reasonably closely rather than skidding sideways, which is exactly
        /// alignVelocityToForward's job.
        /// </summary>
        [MenuItem("Vanquish/Phase 3G/Validate Coordinated Turn Simulation (Headless)")]
        public static bool ValidateCoordinatedTurnSimulation()
        {
            const float dt = 0.02f;
            const float durationSeconds = 5f;
            const float rollInput = 1f; // hold "roll right"
            const float pitchInput = 0.5f; // hold a partial "pull up" to compensate lift lost to banking

            Vector3 position = new Vector3(0f, 200f, 0f);
            Vector3 velocity = Vector3.forward * CruiseSpeed;
            Quaternion attitude = Quaternion.identity;
            float liftCoefficient = LiftCoefficient;

            Quaternion initialAttitude = attitude;

            for (float t = 0f; t < durationSeconds; t += dt)
            {
                float speed = velocity.magnitude;
                float authority = PlayerDroneController.ComputeControlAuthority(speed, ControlAuthorityReferenceSpeed);

                Vector3 currentForward = attitude * Vector3.forward;
                Vector3 currentRight = attitude * Vector3.right;
                Quaternion roll = Quaternion.AngleAxis(-rollInput * RollRateDegPerSec * authority * dt, currentForward);
                Quaternion pitch = Quaternion.AngleAxis(-pitchInput * PitchRateDegPerSec * authority * dt, currentRight);
                attitude = roll * pitch * attitude;

                Vector3 forward = attitude * Vector3.forward;
                Vector3 up = attitude * Vector3.up;

                float aoa = FlightBody.ComputeAngleOfAttackDegrees(forward, currentRight, velocity);
                float liftFactor = FlightBody.ComputeLiftFactor(aoa, ZeroLiftAoA, ReferenceAoA, CriticalAoA);

                Vector3 totalForce = Vector3.down * (Mass * Gravity) + forward * ThrustNewtons;

                if (speed > 0.01f)
                {
                    Vector3 liftDirection = Vector3.ProjectOnPlane(up, velocity).normalized;
                    if (liftDirection.sqrMagnitude < 0.001f)
                        liftDirection = up;
                    float maxLift = MaxGForce * Gravity * Mass;
                    float liftMag = Mathf.Clamp(liftCoefficient * speed * speed * liftFactor, -maxLift, maxLift);
                    totalForce += liftDirection * liftMag;

                    float totalDragCoefficient = DragCoefficient + InducedDragFactor * liftFactor * liftFactor;
                    totalForce += -velocity.normalized * (totalDragCoefficient * speed * speed);
                }

                velocity += totalForce / Mass * dt;

                // alignVelocityToForward damping — matches FlightBody.FixedUpdate exactly.
                Vector3 forwardComponent = Vector3.Project(velocity, forward);
                Vector3 lateralComponent = velocity - forwardComponent;
                velocity += -lateralComponent * VelocityAlignmentStrength * dt;

                position += velocity * dt;

                if (float.IsNaN(position.x) || float.IsNaN(velocity.x))
                {
                    Debug.LogError("[Phase3GFixedWingValidation] FAIL: simulation diverged to NaN.");
                    return false;
                }
            }

            Vector3 finalForward = attitude * Vector3.forward;
            float headingYawChange = Vector3.SignedAngle(initialAttitude * Vector3.forward, finalForward, Vector3.up);
            bool turnedOk = Mathf.Abs(headingYawChange) > 20f;

            Vector3 velocityHorizontal = new Vector3(velocity.x, 0f, velocity.z).normalized;
            Vector3 forwardHorizontal = new Vector3(finalForward.x, 0f, finalForward.z).normalized;
            float velocityToNoseAngle = Vector3.Angle(velocityHorizontal, forwardHorizontal);
            bool coordinatedOk = velocityToNoseAngle < 25f;

            bool altitudeReasonable = Mathf.Abs(position.y - 200f) < 150f;

            Debug.Log($"[Phase3GFixedWingValidation] Coordinated turn over {durationSeconds}s: heading yaw changed " +
                $"{headingYawChange:F1}deg (expect >20, purely emergent from banking — no direct yaw control exists), " +
                $"velocity-to-nose angle={velocityToNoseAngle:F1}deg (expect <25, i.e. not skidding sideways), " +
                $"final altitude={position.y:F1}m from start 200m. {(turnedOk && coordinatedOk && altitudeReasonable ? "PASS" : "FAIL")}");

            return turnedOk && coordinatedOk && altitudeReasonable;
        }

        [MenuItem("Vanquish/Phase 3G/Validate Drone Compatibility Detects Mismatch (Headless)")]
        public static bool ValidateDroneCompatibilityDetectsMismatch()
        {
            const string DronesDir = "Assets/_Project/Data/Drones";

            var multirotorAirframe = AssetDatabase.LoadAssetAtPath<DroneAirframeDefinition>($"{DronesDir}/Airframe_SmallQuad.asset");
            var multirotorPropulsion = AssetDatabase.LoadAssetAtPath<PropulsionDefinition>($"{DronesDir}/Propulsion_Electric_Basic.asset");
            var multirotorWing = AssetDatabase.LoadAssetAtPath<WingOrPropellerDefinition>($"{DronesDir}/Propeller_Basic.asset");
            var multirotorEngine = AssetDatabase.LoadAssetAtPath<DroneEngineDefinition>($"{DronesDir}/Engine_Electric_Basic.asset");

            var fixedWingAirframe = AssetDatabase.LoadAssetAtPath<DroneAirframeDefinition>($"{DronesDir}/Airframe_FixedWing.asset");
            var fixedWingPropulsion = AssetDatabase.LoadAssetAtPath<PropulsionDefinition>($"{DronesDir}/Propulsion_Jet_Subsonic.asset");
            // Wing_FixedWing was retired by Phase3HPlanformSeeder (superseded by the
            // merged Planform picker) — Wing_DeltaWing is its planform-preset-era
            // replacement pairing for this same airframe (see Planform_TwinTailFighter).
            var fixedWingWing = AssetDatabase.LoadAssetAtPath<WingOrPropellerDefinition>($"{DronesDir}/Wing_DeltaWing.asset");
            var fixedWingEngine = AssetDatabase.LoadAssetAtPath<DroneEngineDefinition>($"{DronesDir}/Engine_Jet_Subsonic.asset");

            if (multirotorAirframe == null || multirotorPropulsion == null || multirotorWing == null || multirotorEngine == null ||
                fixedWingAirframe == null || fixedWingPropulsion == null || fixedWingWing == null || fixedWingEngine == null)
            {
                Debug.LogError("[Phase3GFixedWingValidation] FAIL: missing seeded assets — run the Phase 1/Phase 2B seeders first.");
                return false;
            }

            var consistentMultirotor = new DroneLoadout
            {
                airframe = multirotorAirframe,
                propulsion = multirotorPropulsion,
                wingOrPropeller = multirotorWing,
                engine = multirotorEngine,
            };
            bool consistentMultirotorOk = DroneCompatibility.IsLoadoutFlightConfigurationConsistent(consistentMultirotor, out _);

            var consistentFixedWing = new DroneLoadout
            {
                airframe = fixedWingAirframe,
                propulsion = fixedWingPropulsion,
                wingOrPropeller = fixedWingWing,
                engine = fixedWingEngine,
            };
            bool consistentFixedWingOk = DroneCompatibility.IsLoadoutFlightConfigurationConsistent(consistentFixedWing, out _);

            var mismatched = new DroneLoadout
            {
                airframe = multirotorAirframe, // multirotor airframe...
                propulsion = fixedWingPropulsion, // ...with jet propulsion
                wingOrPropeller = multirotorWing,
                engine = multirotorEngine,
            };
            bool mismatchDetected = !DroneCompatibility.IsLoadoutFlightConfigurationConsistent(mismatched, out string reason);

            bool pass = consistentMultirotorOk && consistentFixedWingOk && mismatchDetected;
            Debug.Log($"[Phase3GFixedWingValidation] DroneCompatibility: consistent multirotor design passes={consistentMultirotorOk}, " +
                $"consistent fixed-wing design passes={consistentFixedWingOk}, jet-propulsion-on-multirotor-airframe correctly " +
                $"flagged={mismatchDetected} (reason: \"{reason}\"). {(pass ? "PASS" : "FAIL")}");

            return pass;
        }
    }
}
