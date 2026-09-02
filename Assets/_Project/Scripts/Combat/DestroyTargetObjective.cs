namespace Vanquish.Combat
{
    /// <summary>
    /// Phase 2E's non-skirmish objective type: victory once one specific unit's Health
    /// is destroyed, independent of every other enemy unit in the scene — e.g. "destroy
    /// the enemy SAM site" rather than "destroy all enemy units." Deliberately holds
    /// the target's Health directly (not a GameObject + GetComponent lookup each poll)
    /// since CombatManager.BuildObjective already resolves and validates the Health
    /// component once, at construction time.
    /// </summary>
    public class DestroyTargetObjective : IObjective
    {
        private readonly Health _target;
        private readonly string _description;

        public DestroyTargetObjective(Health target, string description)
        {
            _target = target;
            _description = string.IsNullOrEmpty(description) ? "Destroy the designated target." : description;
        }

        public string Description => _description;

        // Null-checked defensively (e.g. the target GameObject/Health could in theory
        // be destroyed by something outside CombatManager's own tracked unit lists),
        // though in practice CombatManager.RegisterUnit's Start()-time scene scan
        // already guarantees this target is registered and its OnDestroyed handled
        // like any other unit.
        public bool IsVictoryAchieved() => _target == null || _target.IsDestroyed;
    }
}
