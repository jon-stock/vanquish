using System.Collections.Generic;
using UnityEngine;
using Vanquish.Core;
using Vanquish.Data.Drones;

namespace Vanquish.Combat
{
    /// <summary>
    /// Dev-visibility pass (raised during Phase 2D testing): procedurally colors a
    /// unit's visual mesh by team (blue player / red enemy) instead of leaving every
    /// primitive on Unity's default grey material, so units stay readable at combat
    /// distance without needing any imported art assets.
    ///
    /// Phase 3B follow-up (direct user feedback: "everything is blue... make the
    /// colours more in keeping"): the first cut multiplied each hull material's
    /// "finish" by the full team color, so every craft's *base* color was still
    /// dominated by the team hue regardless of what it was actually made of — a
    /// titanium hull and a composite-plastic hull looked like the same blue paint
    /// job with slightly different shine. Hull finishes now start from each
    /// HullMaterialType's own real-world base color (titanium/aluminum read as bare
    /// metal, RAM/carbon fiber read dark, composite plastic reads light) and only
    /// blend in a *small* amount of team color (see TeamTintWeight) for
    /// identification — team recognition (the whole reason this class exists) still
    /// works at a glance via that tint plus the (also now more subdued) emissive
    /// glow, but a design's actual hull material is what you see first.
    ///
    /// Planform-preset pass (direct user feedback: real reference aircraft — X-47B,
    /// YFQ-44A, Gambit — are neutral greys with only small national-insignia-sized
    /// color accents, not a full "blue rinse" tint over the whole airframe): both
    /// team colors were desaturated toward real low-visibility roundel colors
    /// (a muted blue-grey / muted brick-red instead of the previous saturated
    /// cyan-blue / pure red), HullTeamTintWeight was cut further, and the hull
    /// finish's emissive contribution was removed entirely (a matte military
    /// airframe doesn't glow) — team recognition still works via the tint plus the
    /// mini-radar/HUD markers, but the airframe itself now reads as grey metal
    /// first, team color a distant second.
    /// </summary>
    public static class TeamColorUtility
    {
        public static readonly Color PlayerColor = new Color(0.32f, 0.42f, 0.58f);
        public static readonly Color EnemyColor = new Color(0.58f, 0.32f, 0.26f);

        /// <summary>How much of the team color gets blended into a hull material's own
        /// natural base color — low, since the material's real color should dominate.</summary>
        private const float HullTeamTintWeight = 0.08f;

        /// <summary>Missiles (and anything else with no hull material at all) use a
        /// neutral gunmetal base with a stronger tint than hulls get, since a bare
        /// grey missile in flight is genuinely hard to distinguish by team at range —
        /// still nowhere near the old "100% team color" flat look.</summary>
        private const float NeutralTeamTintWeight = 0.22f;
        private static readonly Color NeutralGunmetalBase = new Color(0.42f, 0.44f, 0.47f);

        private static Material _playerMaterial;
        private static Material _enemyMaterial;
        private static readonly Dictionary<(Team, HullMaterialType), Material> HullFinishMaterials = new();

        /// <summary>Assigns the team-appropriate shared material to every Renderer under visualRoot.</summary>
        public static void ApplyTeamColor(Transform visualRoot, Team team)
        {
            ApplyTeamColor(visualRoot, team, null);
        }

        /// <summary>
        /// Same as ApplyTeamColor(Transform, Team), but with an optional hull-material
        /// finish layered on top (see class doc comment). Passing null preserves the
        /// exact original flat-team-color behavior (and its cached materials), so
        /// every existing caller (missiles have no hull material at all) is unaffected.
        /// </summary>
        public static void ApplyTeamColor(Transform visualRoot, Team team, HullMaterialType? hullMaterial)
        {
            if (visualRoot == null)
                return;

            Material material = hullMaterial.HasValue
                ? GetOrCreateHullFinishMaterial(team, hullMaterial.Value)
                : GetOrCreateMaterial(team);
            foreach (var renderer in visualRoot.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterial = material;
        }

        public static Color GetColor(Team team) => team == Team.Player ? PlayerColor : EnemyColor;

        private static Material GetOrCreateMaterial(Team team)
        {
            if (team == Team.Player)
                return _playerMaterial != null ? _playerMaterial : (_playerMaterial = CreateMaterial(PlayerColor));
            return _enemyMaterial != null ? _enemyMaterial : (_enemyMaterial = CreateMaterial(EnemyColor));
        }

        private static Material GetOrCreateHullFinishMaterial(Team team, HullMaterialType hullMaterial)
        {
            var key = (team, hullMaterial);
            if (HullFinishMaterials.TryGetValue(key, out Material cached) && cached != null)
                return cached;

            Color baseColor = GetColor(team);
            Material material = CreateHullFinishMaterial(baseColor, hullMaterial);
            HullFinishMaterials[key] = material;
            return material;
        }

        /// <summary>
        /// Per-HullMaterialType base color + finish — deliberately real-world-
        /// motivated: RAM coatings are dark matte (designed to absorb, not reflect);
        /// titanium and aluminum alloys are bare/polished metal (aluminum brighter,
        /// slightly cooler; titanium a touch warmer/darker); carbon fiber weave is
        /// near-black with a modest sheen; composite plastic (the cheapest Tier-0
        /// default) is a light, low-saturation putty grey. Team color is blended in
        /// afterward at HullTeamTintWeight, not multiplied against these — a full
        /// multiply against a saturated team color was what made every hull material
        /// look "the same blue paint job" in the first cut.
        /// </summary>
        private static Material CreateHullFinishMaterial(Color teamColor, HullMaterialType hullMaterial)
        {
            Color naturalColor;
            float metallic;
            float smoothness;

            switch (hullMaterial)
            {
                case HullMaterialType.RadarAbsorbentMaterial:
                    naturalColor = new Color(0.05f, 0.06f, 0.055f);
                    metallic = 0f;
                    smoothness = 0.08f;
                    break;
                case HullMaterialType.TitaniumAlloy:
                    naturalColor = new Color(0.55f, 0.53f, 0.5f);
                    metallic = 0.85f;
                    smoothness = 0.75f;
                    break;
                case HullMaterialType.AluminumAlloy:
                    naturalColor = new Color(0.72f, 0.74f, 0.76f);
                    metallic = 0.9f;
                    smoothness = 0.6f;
                    break;
                case HullMaterialType.CarbonFiber:
                    naturalColor = new Color(0.06f, 0.06f, 0.07f);
                    metallic = 0.3f;
                    smoothness = 0.5f;
                    break;
                default: // CompositePlastic — the Tier-0 baseline
                    naturalColor = new Color(0.68f, 0.68f, 0.64f);
                    metallic = 0.05f;
                    smoothness = 0.3f;
                    break;
            }

            Color finalColor = Color.Lerp(naturalColor, teamColor, HullTeamTintWeight);
            // Planform-preset pass: no emissive glow on the airframe itself — a real
            // matte military finish doesn't glow, and the previous small emissive tint
            // was part of what read as an overall "blue rinse" across the whole hull.
            return CreateMaterialWithFinish(finalColor, metallic, smoothness, emissiveColor: Color.black);
        }

        /// <summary>Missiles and anything else with no hull material at all — a neutral
        /// gunmetal base tinted a bit more strongly toward the team color than hulls
        /// get (see NeutralTeamTintWeight's doc comment).</summary>
        private static Material CreateMaterial(Color teamColor)
        {
            Color finalColor = Color.Lerp(NeutralGunmetalBase, teamColor, NeutralTeamTintWeight);
            return CreateMaterialWithFinish(finalColor, metallic: 0.2f, smoothness: 0.35f, emissiveColor: teamColor * 0.12f);
        }

        private static Material CreateMaterialWithFinish(Color color, float metallic, float smoothness, Color emissiveColor)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            var material = new Material(shader) { color = color };

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            else if (material.HasProperty("_Glossiness")) // Standard shader's name for the same slider
                material.SetFloat("_Glossiness", smoothness);

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissiveColor);
            }
            return material;
        }
    }
}
