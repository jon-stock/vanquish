namespace Vanquish.Combat
{
    /// <summary>
    /// The "Factory" combat-instance target type (PLAN.md Phase 1) — expect the
    /// heaviest fixed defenses of the structure target types; author a higher
    /// <see cref="Simulation.Damage.Damageable"/> hardness than <see cref="BaseObjective"/>
    /// when configuring one. Also the target checked by the theatre map's economic-
    /// collapse victory condition once Phase 2 exists.
    /// </summary>
    public class FactoryObjective : StructureObjectiveBase
    {
    }
}
