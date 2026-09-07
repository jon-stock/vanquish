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

    /// <summary>
    /// Phase 0 "engagement setup" flow: wires a hardcoded attacker/defender loadout
    /// (PLAN.md — no theatre map yet, this is the placeholder) against one target
    /// type (a <see cref="BaseObjective"/>), tracks the finite stockpile economy, and
    /// resolves the win/lose result. Attack/defense are meant to become fully
    /// symmetric in Phase 1; this controller has no target-type-specific
    /// special-casing beyond referencing a single objective, so it should generalize
    /// to other target types without rework.
    /// </summary>
    public class EngagementController : MonoBehaviour
    {
        [Header("Objective")]
        public BaseObjective objective;

        [Header("Loadout (Phase 0 placeholder for the theatre map)")]
        public StockpileEntry[] attackerLoadout;
        public StockpileEntry[] defenderLoadout;

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

        private void EvaluateWinConditions()
        {
            if (objective != null && objective.HasMetAttackerWinCondition)
            {
                Resolve(EngagementResult.AttackerWin);
                return;
            }

            bool attackerOutOfStock = Attacker.IsFullyDepleted();
            bool timeUp = ElapsedSeconds >= timeLimitSeconds;

            if (attackerOutOfStock || timeUp)
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
