namespace Vanquish.Combat
{
    /// <summary>
    /// The "Warehouse" combat-instance target type (PLAN.md Phase 1) — medium
    /// defense; destroying it removes banked theatre-map stockpile rather than
    /// production capacity. Author a softer <see cref="Simulation.Damage.Damageable"/>
    /// hardness than <see cref="FactoryObjective"/> when configuring one.
    /// </summary>
    public class WarehouseObjective : StructureObjectiveBase
    {
    }
}
