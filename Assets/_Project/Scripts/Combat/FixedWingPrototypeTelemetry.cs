using UnityEngine;
using Vanquish.Simulation.Flight;

namespace Vanquish.Combat
{
    /// <summary>
    /// Fixed-wing flight-model rework: an ugly-art-is-fine OnGUI telemetry overlay for
    /// the FixedWingPrototype test rig (see Phase3GFixedWingPrototypeSceneBuilder) —
    /// airspeed, altitude, angle of attack, and throttle, so the literal "little flying
    /// rectangle" the plan called for is actually inspectable while flying it, not
    /// just a black box. Same "log the numbers, plain overlay" precedent as
    /// Phase0TestHarness/TestRangeTelemetry — deliberately not a HUDController-style
    /// UI Toolkit HUD, since this rig is a disposable physics prototype, not a shipped
    /// combat scene.
    /// </summary>
    public class FixedWingPrototypeTelemetry : MonoBehaviour
    {
        public FlightBody flightBody;
        public Rigidbody body;

        private void OnGUI()
        {
            if (flightBody == null || body == null)
                return;

            GUIStyle style = Style();
            float speed = body.linearVelocity.magnitude;

            GUI.Label(new Rect(10, 10, 520, 22), $"Airspeed: {speed:F1} m/s", style);
            GUI.Label(new Rect(10, 32, 520, 22), $"Altitude: {body.position.y:F1} m", style);
            GUI.Label(new Rect(10, 54, 520, 22), $"Angle of Attack: {flightBody.CurrentAngleOfAttackDegrees:F1} deg " +
                $"(stall beyond {flightBody.criticalAoADegrees:F0} deg)", style);
            GUI.Label(new Rect(10, 76, 520, 22), $"Throttle: {flightBody.throttleFraction * 100f:F0}%", style);
            GUI.Label(new Rect(10, 108, 620, 22),
                "Controls: A/D roll, W/S pitch, Shift/Space throttle up/down, right-drag/scroll to orbit camera", style);

            if (body.position.y < -50f)
                GUI.Label(new Rect(10, 140, 520, 30), "CRASHED — reset by re-entering Play mode", style);
        }

        private static GUIStyle Style()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            style.normal.textColor = Color.white;
            return style;
        }
    }
}
