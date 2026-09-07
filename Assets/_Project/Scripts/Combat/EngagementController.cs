using System;
using UnityEngine;

namespace Vanquish.Combat
{
    public enum EngagementResult
    {
        InProgress,
        AttackerWin,
        DefenderWin,
    }

    /// <summary>Result of a single attacker strike attempt against the objective, via <see cref="EngagementController.CommitAttackerStrike"/>.</summary>
    public enum AttackerStrikeResult
    {
        /// <summary>That part type's stockpile was already empty — nothing was spent, nothing happened.</summary>
        Depleted,

        /// <summary>The unit was committed and spent, but a point-defense battery intercepted it before it landed.</summary>
        Intercepted,

        /// <summary>The unit was committed, got through, and landed its damage on the objective.</summary>
        Hit,
    }

    /// <summary>
    /// The "engagement setup" flow (PLAN.md Phase 0/1): wires a hardcoded
    /// attacker/defender loadout (no theatre map yet — this is the placeholder)
    /// against one <see cref="IObjective"/>, tracks the finite stockpile economy on
    /// both sides (including defending point-defense batteries), and resolves the
    /// win/lose result. Deliberately has no target-type-specific special-casing:
    /// it only ever talks to <see cref="IObjective"/>, so base/factory/warehouse/
    /// supply-line all plug in unchanged, and attack/defense are symmetric — nothing
    /// here assumes which side the player is on.
    /// </summary>
    public class EngagementController : MonoBehaviour
    {
        [Header("Objective")]
        [SerializeField] private MonoBehaviour objectiveBehaviour;

        /// <summary>
        /// The active objective. Exposed as <see cref="IObjective"/> per the "no
        /// target-type-specific special-casing" rule above. Backed by a
        /// <see cref="MonoBehaviour"/> field for Inspector assignment (Unity cannot
        /// serialize a plain interface reference); assign any component that
        /// implements <see cref="IObjective"/> (BaseObjective, FactoryObjective,
        /// WarehouseObjective, SupplyLineObjective, ...).
        /// </summary>
        public IObjective Objective
        {
            get => objectiveBehaviour as IObjective;
            set => objectiveBehaviour = value as MonoBehaviour;
        }

        [Header("Loadout (Phase 0 placeholder for the theatre map)")]
        public StockpileEntry[] attackerLoadout;
        public StockpileEntry[] defenderLoadout;

        [Header("Defenses")]
        [Tooltip("Point-defense batteries that get a chance to intercept each attacker strike (PLAN.md stockpile-drain tactic).")]
        public PointDefenseBattery[] defenderBatteries;

        [Header("Timing")]
        [Tooltip("Defender wins by outlasting the attacker's stockpile/patience if this elapses first.")]
        public float timeLimitSeconds = 180f;

        public Stockpile Attacker { get; private set; }
        public Stockpile Defender { get; private set; }
        public EngagementResult Result { get; private set; } = EngagementResult.InProgress;
        public float ElapsedSeconds { get; private set; }

        /// <summary>Raised exactly once, the tick the engagement result stops being InProgress.</summary>
        public event Action<EngagementResult> OnResolved;

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Builds the attacker/defender <see cref="Stockpile"/>s from the configured
        /// loadouts. Called automatically from <see cref="Awake"/> for normal runtime
        /// use. Also safe (and necessary) to call explicitly right after
        /// <c>AddComponent</c> in editor tooling/tests — Unity does not reliably fire
        /// Awake synchronously for components added to a GameObject outside of a
        /// loaded scene/Play mode (confirmed via the batch-mode smoke test), so
        /// anything constructing an EngagementController outside normal scene load
        /// should call this explicitly rather than assume Awake already ran.
        /// </summary>
        public void Initialize()
        {
            Attacker = new Stockpile(attackerLoadout);
            Defender = new Stockpile(defenderLoadout);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// The actual per-frame update logic, factored out of <see cref="Update"/> so
        /// it can be driven directly (e.g. from an EditMode smoke test/unit test)
        /// without needing Play mode to be running.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (Result != EngagementResult.InProgress)
                return;

            ElapsedSeconds += deltaTime;
            Objective?.Tick(deltaTime);
            EvaluateWinConditions();
        }

        /// <summary>
        /// Called by whatever spawns an attacking unit (direct control, standing
        /// order AI, etc.) to consume one unit of stockpile before it's committed to
        /// the fight. Returns false (and spawns nothing) if that part type is
        /// depleted — callers must not spawn on a false result.
        /// </summary>
        public bool TryCommitAttackerUnit(string partId) => Attacker.TryCommit(partId);

        /// <summary>Same as <see cref="TryCommitAttackerUnit"/> but for the defending side's interceptors/point defense.</summary>
        public bool TryCommitDefenderUnit(string partId) => Defender.TryCommit(partId);

        /// <summary>
        /// The full attacker-strike pipeline (PLAN.md Phase 1): commit one unit of
        /// stockpile, give every defending <see cref="PointDefenseBattery"/> a chance
        /// to intercept it (each attempt costs a battery one interceptor regardless
        /// of outcome — this is what makes cheap decoys able to drain expensive
        /// defenses), and if nothing intercepts it, apply its damage to the
        /// objective via the payload/hardness soft-cap model.
        /// </summary>
        public AttackerStrikeResult CommitAttackerStrike(string partId)
        {
            StockpileEntry entry = Attacker.GetEntry(partId);
            if (entry == null || !Attacker.TryCommit(partId))
                return AttackerStrikeResult.Depleted;

            if (defenderBatteries != null)
            {
                foreach (PointDefenseBattery battery in defenderBatteries)
                {
                    if (battery != null && battery.TryIntercept())
                        return AttackerStrikeResult.Intercepted;
                }
            }

            Objective?.Damageable?.TakeDamage(entry.rawDamage, entry.payloadSize);
            return AttackerStrikeResult.Hit;
        }

        private void EvaluateWinConditions()
        {
            IObjective objective = Objective;

            if (objective != null && objective.HasMetAttackerWinCondition)
            {
                Resolve(EngagementResult.AttackerWin);
                return;
            }

            bool attackerOutOfStock = Attacker.IsFullyDepleted();
            bool timeUp = ElapsedSeconds >= timeLimitSeconds;
            bool defenderObjectiveWin = objective != null && objective.HasMetDefenderWinCondition;

            if (attackerOutOfStock || timeUp || defenderObjectiveWin)
            {
                Resolve(EngagementResult.DefenderWin);
            }
        }

        private void Resolve(EngagementResult result)
        {
            Result = result;
            OnResolved?.Invoke(result);
        }
    }
}
