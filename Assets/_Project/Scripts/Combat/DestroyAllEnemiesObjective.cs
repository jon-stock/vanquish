namespace Vanquish.Combat
{
    /// <summary>
    /// The original Phase 1 win condition, now formalized as one IObjective
    /// implementation instead of CombatManager's own hardcoded logic — this is
    /// CombatManager's default objective, preserving every existing scene's behavior
    /// exactly (Combat_Arena01.unity/Combat_TestArena.unity never set an objectiveType,
    /// so CombatManager.BuildObjective falls back to this).
    /// </summary>
    public class DestroyAllEnemiesObjective : IObjective
    {
        private readonly CombatManager _manager;

        public DestroyAllEnemiesObjective(CombatManager manager)
        {
            _manager = manager;
        }

        public string Description => "Destroy all enemy units.";

        public bool IsVictoryAchieved() => CombatManager.AllDestroyed(_manager.EnemyUnits);
    }
}
