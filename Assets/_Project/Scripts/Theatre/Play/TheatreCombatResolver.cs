using System;
using System.Linq;
using UnityEngine;
using Vanquish.Combat;

namespace Vanquish.Theatre.Play
{
    public enum TheatreCombatOutcome
    {
        AttackerWin,
        DefenderWin,
    }

    /// <summary>Result of one headless army-vs-(site|army) resolution — see <see cref="TheatreCombatResolver"/>.</summary>
    public readonly struct TheatreCombatResult
    {
        public readonly TheatreCombatOutcome Outcome;

        /// <summary>How many of the attacking army's missiles were actually expended resolving this fight — apply via <see cref="Army.ConsumeMissiles"/>.</summary>
        public readonly int AttackerMissilesUsed;

        public TheatreCombatResult(TheatreCombatOutcome outcome, int attackerMissilesUsed)
        {
            Outcome = outcome;
            AttackerMissilesUsed = attackerMissilesUsed;
        }
    }

    /// <summary>
    /// Bridges an army's move onto a hostile hex (the design's "moved into the
    /// combat scene") to the existing combat-instance engine
    /// (<see cref="EngagementController"/>/<see cref="Stockpile"/>/<see cref="IObjective"/>),
    /// resolved headlessly/instantly (an AI-vs-static-objective strike loop, the
    /// same pattern <c>SmokeTest</c> already uses to drive
    /// <see cref="EngagementController"/> without a live scene) rather than via a
    /// full playable 3D scene transition — PLAN.md's "combat instance results
    /// writing back into theatre-map state" feedback loop is still explicitly
    /// deferred/placeholder territory, so this is the pragmatic first wiring of
    /// theatre-map army combat into the real stockpile/damage/hardness-soft-cap
    /// machinery rather than a bespoke separate resolution system.
    ///
    /// The moving army is always the Attacker; the stationary target (a Site's
    /// structure health, or the defending army's collective toughness) is always
    /// the Objective. Neither a defended Site nor a defending Army has any active
    /// defensive fire of its own yet (no theatre-level point-defense/garrison model
    /// exists) — a known, deliberate simplification, not an oversight.
    /// </summary>
    public static class TheatreCombatResolver
    {
        private const float QuadcopterRawDamage = 8f;
        private const float QuadcopterPayloadSize = 1f;
        private const float HexacopterRawDamage = 14f;
        private const float HexacopterPayloadSize = 2f;

        /// <summary>Experience awarded to the attacking army on a win — destroying an opposing army is the bigger, riskier accomplishment.</summary>
        private const int ExperienceOnArmyKill = 2;
        private const int ExperienceOnSiteWin = 1;

        /// <summary>Resolves an attacking army moving into an enemy Site's hex. Applies resulting damage directly to <paramref name="site"/> via <see cref="Site.ApplyDamage"/>.</summary>
        public static TheatreCombatResult ResolveArmyVsSite(Army attacker, Site site)
        {
            var objectiveGo = new GameObject("TheatreCombat_SiteObjective");
            try
            {
                StructureObjectiveBase objective = site.Type switch
                {
                    SiteType.Factory => (StructureObjectiveBase)objectiveGo.AddComponent<FactoryObjective>(),
                    SiteType.Warehouse => objectiveGo.AddComponent<WarehouseObjective>(),
                    _ => objectiveGo.AddComponent<BaseObjective>(),
                };

                SiteCombatProfile.Profile profile = SiteCombatProfile.For(site.Type);
                float scaledMaxHealth = Mathf.Max(1f, profile.MaxHealth * Mathf.Max(0.01f, site.HealthFraction01));
                objective.Damageable.Configure(scaledMaxHealth, profile.Hardness, profile.SoftCapResidualFraction);
                objective.attackerWinDestroyedFraction = 0.75f;

                TheatreCombatResult result = RunEngagement(attacker, objective);

                // The objective's remaining-health fraction is relative to the
                // scaled max above (i.e. relative to the site's health *before*
                // this fight) — translate that back into the site's own 0..1
                // absolute health scale before calling ApplyDamage.
                float remainingFractionOfScaled = objective.Damageable.HealthFraction01;
                float newSiteFraction = site.HealthFraction01 * remainingFractionOfScaled;
                float damageFraction01 = Mathf.Clamp01(site.HealthFraction01 - newSiteFraction);
                site.ApplyDamage(damageFraction01);

                if (result.Outcome == TheatreCombatOutcome.AttackerWin)
                    attacker.AddExperience(ExperienceOnSiteWin);

                return result;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objectiveGo);
            }
        }

        /// <summary>
        /// Resolves an attacking army moving into a hex occupied by a hostile
        /// army. On an attacker win, <paramref name="defender"/> is left empty
        /// (caller removes it from the world). On a defender win, the defender
        /// takes proportional losses instead via <see cref="Army.ApplyProportionalLosses"/>.
        /// </summary>
        public static TheatreCombatResult ResolveArmyVsArmy(Army attacker, Army defender)
        {
            var objectiveGo = new GameObject("TheatreCombat_ArmyObjective");
            try
            {
                var objective = objectiveGo.AddComponent<ArmyObjective>();
                float defenderMaxHealth = Mathf.Max(1f, 10f * defender.DroneCount + 4f * defender.MissileCount);
                objective.Damageable.Configure(defenderMaxHealth, 0f, 0f);
                objective.attackerWinDestroyedFraction = 1f;

                TheatreCombatResult result = RunEngagement(attacker, objective);

                float destroyedFraction01 = 1f - objective.Damageable.HealthFraction01;
                if (result.Outcome == TheatreCombatOutcome.AttackerWin)
                {
                    defender.Composition.Clear();
                    attacker.AddExperience(ExperienceOnArmyKill);
                }
                else
                {
                    defender.ApplyProportionalLosses(destroyedFraction01);
                }

                return result;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objectiveGo);
            }
        }

        /// <summary>
        /// The actual attacker-vs-objective strike loop, shared by both resolve
        /// methods above: builds a single aggregate <see cref="StockpileEntry"/>
        /// from the attacking army's drone/missile counts (one "strike" == one
        /// drone expending one missile — drones themselves are not consumed, only
        /// the missiles they carry, matching "armies need missiles to be combat
        /// effective" rather than drones being one-shot expendables), then commits
        /// strikes one at a time via <see cref="EngagementController"/> until the
        /// objective is destroyed (attacker win) or the attacker's stock is
        /// depleted (defender win). An attacker with zero usable strikes (no
        /// drones and/or no missiles) resolves as an immediate defender win.
        /// </summary>
        private static TheatreCombatResult RunEngagement(Army attacker, IObjective objective)
        {
            int totalStrikes = Mathf.Max(0, Mathf.Min(attacker.DroneCount, attacker.MissileCount));
            float rawDamage = WeightedAverage(attacker, QuadcopterRawDamage, HexacopterRawDamage);
            float payloadSize = WeightedAverage(attacker, QuadcopterPayloadSize, HexacopterPayloadSize);

            var part = ScriptableObject.CreateInstance<Vanquish.Data.Drones.DroneAirframeDefinition>();
            part.id = "theatre.strike." + Guid.NewGuid();

            var controllerGo = new GameObject("TheatreCombat_Engagement");
            int usedStrikes = 0;
            TheatreCombatOutcome outcome;
            try
            {
                var controller = controllerGo.AddComponent<EngagementController>();
                controller.Objective = objective;
                controller.attackerLoadout = new[]
                {
                    new StockpileEntry { part = part, startingCount = totalStrikes, rawDamage = rawDamage, payloadSize = payloadSize },
                };
                controller.defenderLoadout = Array.Empty<StockpileEntry>();
                controller.timeLimitSeconds = float.MaxValue;
                controller.Initialize();

                while (controller.Result == EngagementResult.InProgress)
                {
                    AttackerStrikeResult strike = controller.CommitAttackerStrike(part.id);
                    if (strike == AttackerStrikeResult.Depleted)
                        break;

                    usedStrikes++;
                    controller.Tick(1f);
                }

                outcome = controller.Result == EngagementResult.AttackerWin
                    ? TheatreCombatOutcome.AttackerWin
                    : TheatreCombatOutcome.DefenderWin;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(controllerGo);
                UnityEngine.Object.DestroyImmediate(part);
            }

            return new TheatreCombatResult(outcome, usedStrikes);
        }

        private static float WeightedAverage(Army army, float quadcopterValue, float hexacopterValue)
        {
            int quad = army.Composition.Where(kv => kv.Key.Category == UnitCategory.Quadcopter).Sum(kv => kv.Value);
            int hex = army.Composition.Where(kv => kv.Key.Category == UnitCategory.Hexacopter).Sum(kv => kv.Value);
            int total = quad + hex;
            return total <= 0 ? quadcopterValue : (quad * quadcopterValue + hex * hexacopterValue) / total;
        }
    }
}
