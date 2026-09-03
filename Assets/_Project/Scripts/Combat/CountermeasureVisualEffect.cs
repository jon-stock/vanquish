using UnityEngine;

namespace Vanquish.Combat
{
    /// <summary>
    /// Depth pass (direct user feedback: "I can't tell if countermeasures do
    /// anything, so they probably don't"): a decoy successfully breaking a missile's
    /// lock previously only produced a Debug.Log line — invisible during actual
    /// play. Spawns a small, brief, bright expanding sphere (same "ugly art is fine,
    /// make the event visible" philosophy as the rest of this project's procedural
    /// visuals) at the point of the break so a flare/chaff deploy is something a
    /// player can actually see happen, not just infer from a missile suddenly
    /// missing.
    /// </summary>
    public static class CountermeasureVisualEffect
    {
        public static void SpawnFlareBurst(Vector3 position)
        {
            GameObject flare = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flare.name = "FlareBurst";
            Object.Destroy(flare.GetComponent<Collider>());
            flare.transform.position = position;
            flare.transform.localScale = Vector3.one * 0.4f;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = new Color(1f, 0.75f, 0.3f) };
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.3f) * 4f);
            }
            flare.GetComponent<Renderer>().sharedMaterial = material;

            flare.AddComponent<FlareBurstAnimator>();
        }

        private class FlareBurstAnimator : MonoBehaviour
        {
            private const float LifetimeSeconds = 0.8f;
            private const float MaxScale = 2.5f;
            private float _elapsed;
            private Vector3 _baseScale;

            private void Awake() => _baseScale = transform.localScale;

            private void Update()
            {
                _elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(_elapsed / LifetimeSeconds);
                transform.localScale = _baseScale * Mathf.Lerp(1f, MaxScale, t);
                if (t >= 1f)
                    Destroy(gameObject);
            }
        }
    }
}
