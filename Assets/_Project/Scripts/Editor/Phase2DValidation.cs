using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Vanquish.Combat;
using Vanquish.Data.Support;
using Vanquish.Simulation.Sensors;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Headless sanity checks for Phase 2D's role-aware targeting archetypes
    /// (Interceptor, Scout-hunter). The behavior under test in both cases — "prefer a
    /// known contact matching this archetype's preferred role over a merely-nearer
    /// contact of the wrong role" — lives in TeamAwareness.SelectNearest (factored out
    /// specifically so it's callable without a live scene of DetectionSensors for
    /// TeamAwareness to scan every LateUpdate), so this instantiates a handful of
    /// throwaway DetectableSignature GameObjects directly (same disposable-GameObject
    /// pattern as Phase2CValidation.ValidateCountermeasureDecoys) rather than requiring
    /// a full Play-mode battle.
    /// </summary>
    public static class Phase2DValidation
    {
        [MenuItem("Vanquish/Phase 2D/Validate Interceptor Armed-Target Priority (Headless)")]
        public static void ValidateInterceptorArmedTargetPriority()
        {
            var scoutGo = new GameObject("Phase2DValidation_TempScout");
            var strikeGo = new GameObject("Phase2DValidation_TempStrike");
            var farStrikeGo = new GameObject("Phase2DValidation_TempFarStrike");
            try
            {
                // Mirrors Phase1CombatSceneBuilder's actual arrangement: an unarmed
                // scout sits slightly CLOSER to the seeker than the armed strike drone
                // it's escorting — this is exactly the case that used to make plain
                // nearest-contact selection latch onto the scout instead.
                var scout = scoutGo.AddComponent<DetectableSignature>();
                scout.isArmed = false;
                scoutGo.transform.position = new Vector3(30f, 5f, -190f);

                var strike = strikeGo.AddComponent<DetectableSignature>();
                strike.isArmed = true;
                strikeGo.transform.position = new Vector3(0f, 5f, -200f);

                var farStrike = farStrikeGo.AddComponent<DetectableSignature>();
                farStrike.isArmed = true;
                farStrikeGo.transform.position = new Vector3(0f, 5f, -600f);

                var contacts = new List<DetectableSignature> { scout, strike, farStrike };
                Vector3 seekerPosition = new Vector3(0f, 5f, 200f);

                DetectableSignature nearestAny = TeamAwareness.SelectNearest(contacts, seekerPosition);
                DetectableSignature nearestArmed = TeamAwareness.SelectNearest(contacts, seekerPosition, c => c.isArmed);

                bool nearestAnyIsScout = nearestAny == scout;
                bool nearestArmedIsStrike = nearestArmed == strike;

                Debug.Log($"[Phase2DValidation] Nearest contact of any role: {nearestAny.gameObject.name} " +
                    $"(expect the closer unarmed scout, confirming the scout really is nearer).");
                Debug.Log($"[Phase2DValidation] Plain nearest-contact selection would target the unarmed scout: {(nearestAnyIsScout ? "PASS" : "FAIL")}");

                Debug.Log($"[Phase2DValidation] Armed-only selection picked: {nearestArmed.gameObject.name} " +
                    $"(expect the nearer of the two armed strike drones, ignoring the closer unarmed scout).");
                Debug.Log($"[Phase2DValidation] Interceptor's armed-only targeting correctly picks the strike drone over a closer scout: {(nearestArmedIsStrike ? "PASS" : "FAIL")}");

                // With no armed contact known at all, armed-only selection must return
                // null so InterceptorAI.AcquireTarget's fallback path (any known
                // contact) actually has a reason to run instead of the archetype going
                // permanently inert when only a lone scout is present.
                var unarmedOnlyContacts = new List<DetectableSignature> { scout };
                DetectableSignature armedOnlyResultWithNoArmedContacts = TeamAwareness.SelectNearest(unarmedOnlyContacts, seekerPosition, c => c.isArmed);
                bool correctlyReturnsNullWhenNoneArmed = armedOnlyResultWithNoArmedContacts == null;
                Debug.Log($"[Phase2DValidation] Armed-only selection with no armed contacts known returns null (so InterceptorAI can fall back): {(correctlyReturnsNullWhenNoneArmed ? "PASS" : "FAIL")}");

                bool allPass = nearestAnyIsScout && nearestArmedIsStrike && correctlyReturnsNullWhenNoneArmed;
                Debug.Log(allPass
                    ? "[Phase2DValidation] Interceptor armed-target priority: ALL PASS"
                    : "[Phase2DValidation] Interceptor armed-target priority: ONE OR MORE FAILURES ABOVE");
                if (!allPass)
                    Debug.LogError("[Phase2DValidation] Interceptor armed-target priority validation FAILED.");
            }
            finally
            {
                Object.DestroyImmediate(scoutGo);
                Object.DestroyImmediate(strikeGo);
                Object.DestroyImmediate(farStrikeGo);
            }
        }

        [MenuItem("Vanquish/Phase 2D/Validate Scout-Hunter Scout-Target Priority (Headless)")]
        public static void ValidateScoutHunterScoutTargetPriority()
        {
            var scoutGo = new GameObject("Phase2DValidation_TempScout2");
            var strikeGo = new GameObject("Phase2DValidation_TempStrike2");
            try
            {
                // A scout drone's sensor suite has isScout=true (SensorSuiteDefinition.
                // sharesContactsWithTeam); an armed strike drone's does not, even though
                // it's otherwise closer — mirrors ValidateInterceptorArmedTargetPriority's
                // arrangement so both archetypes are validated against the same kind of
                // "closer contact of the wrong role" trap.
                var strike = strikeGo.AddComponent<DetectableSignature>();
                strike.isArmed = true;
                strike.isScout = false;
                strikeGo.transform.position = new Vector3(0f, 5f, -200f);

                var scout = scoutGo.AddComponent<DetectableSignature>();
                scout.isArmed = false;
                scout.isScout = true;
                scoutGo.transform.position = new Vector3(30f, 5f, -600f); // farther away than the strike drone

                var contacts = new List<DetectableSignature> { scout, strike };
                Vector3 seekerPosition = new Vector3(0f, 5f, 200f);

                DetectableSignature nearestAny = TeamAwareness.SelectNearest(contacts, seekerPosition);
                DetectableSignature nearestScout = TeamAwareness.SelectNearest(contacts, seekerPosition, c => c.isScout);

                bool nearestAnyIsStrike = nearestAny == strike;
                bool nearestScoutIsScout = nearestScout == scout;

                Debug.Log($"[Phase2DValidation] Nearest contact of any role: {nearestAny.gameObject.name} " +
                    $"(expect the closer armed strike drone, confirming it really is nearer than the scout).");
                Debug.Log($"[Phase2DValidation] Plain nearest-contact selection would target the strike drone, not the scout: {(nearestAnyIsStrike ? "PASS" : "FAIL")}");

                Debug.Log($"[Phase2DValidation] Scout-priority selection picked: {nearestScout.gameObject.name} " +
                    $"(expect the farther-away scout, ignoring the closer strike drone).");
                Debug.Log($"[Phase2DValidation] Scout-hunter's scout-priority targeting correctly picks the scout over a closer strike drone: {(nearestScoutIsScout ? "PASS" : "FAIL")}");

                // With no scout known at all, scout-only selection must return null so
                // ScoutHunterAI.AcquireTarget's fallback path (any known contact) has a
                // reason to run instead of the archetype going permanently inert when
                // the opposing team fields no scout.
                var noScoutContacts = new List<DetectableSignature> { strike };
                DetectableSignature scoutOnlyResultWithNoScouts = TeamAwareness.SelectNearest(noScoutContacts, seekerPosition, c => c.isScout);
                bool correctlyReturnsNullWhenNoScouts = scoutOnlyResultWithNoScouts == null;
                Debug.Log($"[Phase2DValidation] Scout-only selection with no scouts known returns null (so ScoutHunterAI can fall back): {(correctlyReturnsNullWhenNoScouts ? "PASS" : "FAIL")}");

                bool allPass = nearestAnyIsStrike && nearestScoutIsScout && correctlyReturnsNullWhenNoScouts;
                Debug.Log(allPass
                    ? "[Phase2DValidation] Scout-hunter scout-target priority: ALL PASS"
                    : "[Phase2DValidation] Scout-hunter scout-target priority: ONE OR MORE FAILURES ABOVE");
                if (!allPass)
                    Debug.LogError("[Phase2DValidation] Scout-hunter scout-target priority validation FAILED.");
            }
            finally
            {
                Object.DestroyImmediate(scoutGo);
                Object.DestroyImmediate(strikeGo);
            }
        }

        /// <summary>
        /// Data-integrity check on the seeded SAM site asset (mirrors 2A/2B's own
        /// "confirm assets seeded correctly" pattern) — the actual targeting/steering
        /// logic behind SamSiteAI has nothing novel to unit-test in isolation (its
        /// entire decision is a one-line distance comparison against
        /// TeamAwareness.GetNearestKnownEnemy, both already covered by the two tests
        /// above/TeamAwareness's own tests), so live behavior is instead verified via a
        /// full headless Play-mode regression against CombatTestSceneBuilder's
        /// multi-archetype scene (see PLAN.md's Phase 2D writeup) rather than a
        /// redundant pure-logic test here.
        /// </summary>
        [MenuItem("Vanquish/Phase 2D/Validate SAM Site Definition Asset (Headless)")]
        public static void ValidateSamSiteDefinitionAsset()
        {
            var definition = AssetDatabase.LoadAssetAtPath<BaseDefenseDefinition>(
                "Assets/_Project/Data/Support/BaseDefense_SamSite_Basic.asset");

            bool assetExists = definition != null;
            Debug.Log($"[Phase2DValidation] BaseDefense_SamSite_Basic asset exists: {(assetExists ? "PASS" : "FAIL")}");
            if (!assetExists)
            {
                Debug.LogError("[Phase2DValidation] SAM site asset validation FAILED — run " +
                    "Vanquish/Phase 2D/Seed SAM Site Definition first.");
                return;
            }

            bool hasCompleteMissileLoadout = definition.missileLoadout != null && definition.missileLoadout.IsComplete;
            bool hasPositiveEngagementRange = definition.engagementRangeMeters > 0f;
            bool hasPositiveFireRate = definition.rateOfFirePerSecond > 0f;
            bool hasPositiveHealth = definition.health > 0f;
            bool hasAmmo = definition.ammoCount > 0;

            Debug.Log($"[Phase2DValidation] missileLoadout is non-null and complete: {(hasCompleteMissileLoadout ? "PASS" : "FAIL")}");
            Debug.Log($"[Phase2DValidation] engagementRangeMeters ({definition.engagementRangeMeters}) > 0: {(hasPositiveEngagementRange ? "PASS" : "FAIL")}");
            Debug.Log($"[Phase2DValidation] rateOfFirePerSecond ({definition.rateOfFirePerSecond}) > 0: {(hasPositiveFireRate ? "PASS" : "FAIL")}");
            Debug.Log($"[Phase2DValidation] health ({definition.health}) > 0: {(hasPositiveHealth ? "PASS" : "FAIL")}");
            Debug.Log($"[Phase2DValidation] ammoCount ({definition.ammoCount}) > 0: {(hasAmmo ? "PASS" : "FAIL")}");

            bool allPass = hasCompleteMissileLoadout && hasPositiveEngagementRange && hasPositiveFireRate && hasPositiveHealth && hasAmmo;
            Debug.Log(allPass
                ? "[Phase2DValidation] SAM site definition asset: ALL PASS"
                : "[Phase2DValidation] SAM site definition asset: ONE OR MORE FAILURES ABOVE");
            if (!allPass)
                Debug.LogError("[Phase2DValidation] SAM site definition asset validation FAILED.");
        }

        [MenuItem("Vanquish/Phase 2D/Validate All AI Depth (Headless)")]
        public static void ValidateAll()
        {
            ValidateInterceptorArmedTargetPriority();
            ValidateScoutHunterScoutTargetPriority();
            ValidateSamSiteDefinitionAsset();
        }
    }
}
