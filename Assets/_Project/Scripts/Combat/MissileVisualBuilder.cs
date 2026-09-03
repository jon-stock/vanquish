using UnityEngine;
using Vanquish.Core;
using Vanquish.Data;
using Vanquish.Data.Missiles;

namespace Vanquish.Combat
{
    /// <summary>
    /// Phase 3B: procedurally builds a missile's visual — body proportions driven by
    /// the airframe's own stats, and a seeker-specific nose so a Semi-Active Radar
    /// seeker, an Imaging Infrared seeker, and a Laser seeker are visibly different
    /// missiles, not the same grey capsule with a different stat sheet. One single
    /// builder used identically for every context a missile visual appears in — a
    /// free-flying projectile (VehicleFactory.SpawnMissile), a missile mounted on a
    /// drone's hardpoint (VehicleFactory.SpawnDrone), and the Workshop's live design
    /// preview (WorkshopPreviewStage) — matching this doc's long-standing "one
    /// implementation, not two" principle (see PLAN.md Phase 3B goal).
    ///
    /// MissileAirframeDefinition has no discrete "class" enum (unlike
    /// DroneAirframeDefinition's SmallQuad/FixedWing/... — see its own doc comment),
    /// so body proportions are derived continuously from its stats instead of a
    /// clean switch: higher maxGForce (more maneuverable, short-range AAM-style
    /// designs) shortens the body; higher structuralMassKg (a bigger/heavier
    /// airframe) widens it. This is a deliberately simple, real-world-motivated
    /// mapping (short-range AAMs like AIM-9 are short and fat; long-range/cruise
    /// designs are long and slender) rather than a physically rigorous model.
    ///
    /// Known open tension (not solved here): this same missile size range is used
    /// for every carrier, from a small multirotor (SmallQuad, ~1.8m across) up to a
    /// full fixed-wing UCAV (~6-8m span post-3B-follow-up) — a real Predator-class
    /// aircraft's Hellfire missiles are small relative to the aircraft, but the same
    /// missile mounted on a tiny quadcopter can look oversized relative to *that*
    /// carrier. Fully solving this would mean scaling munition size to carrier size
    /// (or gating which missile "classes" fit which hardpoint/airframe size), which
    /// is out of scope for this pass — flagged here as its own "needs its own design
    /// pass" item rather than solved incidentally.
    /// </summary>
    public static class MissileVisualBuilder
    {
        // Plausible min/max stat ranges used only to normalize proportions — these
        // aren't hard game-balance limits, just the range the current seeded data
        // roughly spans, chosen generously so future higher-tier parts still land
        // somewhere reasonable on the curve instead of clamping to one extreme.
        private const float MinStructuralMassKg = 2f;
        private const float MaxStructuralMassKg = 40f;
        private const float MinMaxGForce = 5f;
        private const float MaxMaxGForce = 45f;

        /// <summary>
        /// Builds a complete missile visual (body, tail fins, seeker nose, engine
        /// glow) as a new child of `parent`, at `parent`'s local origin. `scale`
        /// lets a mounted-on-hardpoint missile be drawn slightly smaller than a
        /// free-flying one without duplicating any of this logic.
        /// </summary>
        public static Transform Build(Transform parent, MissileLoadout loadout, Team team, float scale = 1f)
        {
            var root = new GameObject("MissileVisual");
            root.transform.SetParent(parent, worldPositionStays: false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one * scale;

            MissileAirframeDefinition airframe = loadout?.airframe;
            float agility = airframe != null
                ? Mathf.InverseLerp(MinMaxGForce, MaxMaxGForce, airframe.maxGForce)
                : 0.5f;
            float massFactor = airframe != null
                ? Mathf.InverseLerp(MinStructuralMassKg, MaxStructuralMassKg, airframe.structuralMassKg)
                : 0.5f;

            // Phase 3B follow-up (direct user feedback: "think about actual real
            // world dimensions of missiles"): was Lerp(1.0, 0.55) — a ~1.1-2.0m total
            // length, noticeably smaller than a real short-range AAM (AIM-9
            // Sidewinder: ~3m) let alone a BVR/cruise-scale weapon. Bumped toward
            // real missile scale (full length now ~1.5-2.6m) while staying small
            // enough to still look plausible mounted on this game's smaller combat
            // drones rather than dwarfing them — see MissileVisualBuilder's own
            // ongoing tension with drone scale in its class doc comment.
            float diameter = Mathf.Lerp(0.3f, 0.65f, massFactor);
            float halfLength = Mathf.Lerp(1.3f, 0.75f, agility);
            float radius = diameter * 0.5f;

            Transform body = BuildBody(root.transform, diameter, halfLength);
            BuildTailFins(root.transform, radius, halfLength, agility);

            // Team color applied now, before any seeker-specific/engine-glow detail
            // pieces exist — later additions get their own distinct materials rather
            // than being swept up into the flat team-color pass (same ordering
            // convention VehicleFactory already used for the old capsule + glow).
            TeamColorUtility.ApplyTeamColor(root.transform, team);

            BuildSeekerNose(root.transform, loadout?.seeker, radius, halfLength);
            DroneVisualBuilder.AddEngineGlow(root.transform, new Vector3(0f, 0f, -halfLength), TeamColorUtility.GetColor(team));

            return root.transform;
        }

        private static Transform BuildBody(Transform parent, float diameter, float halfLength)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            DestroyColliderImmediate(body);
            body.transform.SetParent(parent, worldPositionStays: false);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(diameter, halfLength, diameter);
            return body.transform;
        }

        /// <summary>
        /// Four small tail fins, sized by `agility` — a more maneuverable design
        /// (higher maxGForce) gets visibly larger control surfaces, a longer-range/
        /// low-G design gets small stabilizer-only fins. Purely cosmetic; the actual
        /// maneuverability comes from FlightBody/GuidanceController, not these.
        /// </summary>
        private static void BuildTailFins(Transform parent, float radius, float halfLength, float agility)
        {
            float finSpan = Mathf.Lerp(0.12f, 0.32f, agility);
            float finZ = -halfLength * 0.75f;

            for (int i = 0; i < 4; i++)
            {
                float angleDeg = 45f + i * 90f; // X configuration, matching the body's round cross-section
                GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fin.name = "TailFin";
                DestroyColliderImmediate(fin);
                fin.transform.SetParent(parent, worldPositionStays: false);
                Quaternion finRotation = Quaternion.Euler(0f, 0f, angleDeg);
                fin.transform.localRotation = finRotation;
                fin.transform.localPosition = finRotation * new Vector3(0f, radius + finSpan * 0.5f, 0f) + new Vector3(0f, 0f, finZ);
                fin.transform.localScale = new Vector3(0.02f, finSpan, 0.14f);
            }
        }

        /// <summary>
        /// The actual "seeker changes what the missile looks like" payoff — one
        /// distinct nose treatment per SeekerType, each with its own non-team-colored
        /// material (seeker hardware doesn't belong to a "team paint job", real
        /// missiles' radomes/domes look the same regardless of operator). Positioned
        /// at the body's nose tip (local +Z, per the body capsule's halfLength).
        /// </summary>
        private static void BuildSeekerNose(Transform parent, SeekerDefinition seeker, float radius, float halfLength)
        {
            Vector3 noseTip = new Vector3(0f, 0f, halfLength);
            SeekerType seekerType = seeker != null ? seeker.seekerType : SeekerType.None;

            switch (seekerType)
            {
                case SeekerType.None:
                    // Inert/unguided nose — a short blunt cone, dark flat matte (no
                    // optics at all, just an aerodynamic warhead tip).
                    BuildConeNose(parent, noseTip, radius * 0.95f, radius * 0.9f, DarkMatteColor, metallic: 0f, smoothness: 0.15f);
                    break;

                case SeekerType.Optical:
                    // A small glassy lens dome — pale, high-smoothness "glass" look.
                    BuildDomeNose(parent, noseTip, radius * 0.55f, new Color(0.75f, 0.85f, 0.95f), metallic: 0.1f, smoothness: 0.9f);
                    break;

                case SeekerType.Infrared:
                    // IR-transparent windows read dark/near-black to the naked eye.
                    BuildDomeNose(parent, noseTip, radius * 0.55f, new Color(0.08f, 0.05f, 0.1f), metallic: 0.05f, smoothness: 0.5f);
                    break;

                case SeekerType.ImagingInfrared:
                    // Same "dark window" language as plain Infrared, but a visibly
                    // larger dome — a bigger imaging array, not just a point sensor.
                    BuildDomeNose(parent, noseTip, radius * 0.7f, new Color(0.1f, 0.08f, 0.14f), metallic: 0.1f, smoothness: 0.55f);
                    break;

                case SeekerType.SemiActiveRadar:
                    // Classic light-grey radome — rounded cone, matte composite finish.
                    BuildConeNose(parent, noseTip, radius * 1.0f, radius * 1.3f, RadomeGreyColor, metallic: 0.05f, smoothness: 0.3f);
                    break;

                case SeekerType.ActiveRadar:
                    // A larger radome than SARH (its own onboard transmitter needs
                    // more internal antenna) plus a thin gimbal-ring accent band.
                    BuildConeNose(parent, noseTip, radius * 1.15f, radius * 1.5f, RadomeGreyColor, metallic: 0.05f, smoothness: 0.3f);
                    BuildGimbalRing(parent, noseTip - new Vector3(0f, 0f, radius * 0.5f), radius * 1.05f);
                    break;

                case SeekerType.MultiSpectral:
                    // The most advanced/expensive seeker — the biggest nose of all:
                    // a full radome plus an inset dark IR window, telegraphing "does
                    // everything" at a glance.
                    BuildConeNose(parent, noseTip, radius * 1.15f, radius * 1.5f, RadomeGreyColor, metallic: 0.05f, smoothness: 0.3f);
                    BuildDomeNose(parent, noseTip - new Vector3(0f, 0f, radius * 0.3f), radius * 0.45f,
                        new Color(0.1f, 0.08f, 0.14f), metallic: 0.1f, smoothness: 0.55f);
                    break;

                case SeekerType.Laser:
                    // A small bright red-tinted lens right at the tip — the one
                    // seeker type that gets an emissive accent, hinting at active
                    // laser-spot tracking rather than a passive window/radome.
                    BuildEmissiveLensNose(parent, noseTip, radius * 0.45f, new Color(0.9f, 0.15f, 0.1f));
                    break;

                case SeekerType.WireOrDatalinkGuided:
                default:
                    // No onboard seeker optics at all — flat blunt nose, plus a small
                    // trailing wire-spool cylinder near the tail (a nod to real
                    // wire-guided ATGM/SACLOS missiles trailing a physical wire).
                    BuildConeNose(parent, noseTip, radius * 0.95f, radius * 0.7f, DarkMatteColor, metallic: 0f, smoothness: 0.15f);
                    BuildWireSpool(parent, new Vector3(0f, 0f, -halfLength * 0.55f), radius);
                    break;
            }
        }

        private static readonly Color DarkMatteColor = new Color(0.12f, 0.12f, 0.13f);
        private static readonly Color RadomeGreyColor = new Color(0.72f, 0.72f, 0.68f);

        private static void BuildConeNose(Transform parent, Vector3 baseLocalPosition, float radius, float height,
            Color color, float metallic, float smoothness)
        {
            var go = new GameObject("SeekerNose");
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = baseLocalPosition;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // apex-along-Y mesh -> points along +Z

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = PrimitiveMeshFactory.CreateCone(radius, height);
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = CreateDetailMaterial(color, metallic, smoothness);
        }

        private static void BuildDomeNose(Transform parent, Vector3 localPosition, float radius, Color color,
            float metallic, float smoothness)
        {
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "SeekerDome";
            DestroyColliderImmediate(dome);
            dome.transform.SetParent(parent, worldPositionStays: false);
            dome.transform.localPosition = localPosition;
            dome.transform.localScale = Vector3.one * radius * 2f;
            dome.GetComponent<Renderer>().sharedMaterial = CreateDetailMaterial(color, metallic, smoothness);
        }

        private static void BuildEmissiveLensNose(Transform parent, Vector3 localPosition, float radius, Color color)
        {
            GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lens.name = "SeekerLens";
            DestroyColliderImmediate(lens);
            lens.transform.SetParent(parent, worldPositionStays: false);
            lens.transform.localPosition = localPosition;
            lens.transform.localScale = Vector3.one * radius * 2f;

            Material material = CreateDetailMaterial(color, metallic: 0.2f, smoothness: 0.8f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.5f);
            }
            lens.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void BuildGimbalRing(Transform parent, Vector3 localPosition, float radius)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "GimbalRing";
            DestroyColliderImmediate(ring);
            ring.transform.SetParent(parent, worldPositionStays: false);
            ring.transform.localPosition = localPosition;
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localScale = new Vector3(radius * 2.1f, 0.02f, radius * 2.1f);
            ring.GetComponent<Renderer>().sharedMaterial = CreateDetailMaterial(RadomeGreyColor * 0.7f, metallic: 0.3f, smoothness: 0.3f);
        }

        private static void BuildWireSpool(Transform parent, Vector3 localPosition, float bodyRadius)
        {
            GameObject spool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spool.name = "WireSpool";
            DestroyColliderImmediate(spool);
            spool.transform.SetParent(parent, worldPositionStays: false);
            spool.transform.localPosition = localPosition + new Vector3(0f, -bodyRadius * 0.5f, 0f);
            spool.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            spool.transform.localScale = new Vector3(bodyRadius * 0.5f, 0.08f, bodyRadius * 0.5f);
            spool.GetComponent<Renderer>().sharedMaterial = CreateDetailMaterial(DarkMatteColor, metallic: 0.1f, smoothness: 0.2f);
        }

        private static Material CreateDetailMaterial(Color color, float metallic, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            else if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", smoothness);
            return material;
        }

        private static void DestroyColliderImmediate(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(collider);
            else
                Object.DestroyImmediate(collider);
        }
    }
}
