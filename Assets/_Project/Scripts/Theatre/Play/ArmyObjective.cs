using Vanquish.Combat;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// Stand-in <see cref="IObjective"/> representing an opposing field army's
    /// collective toughness for a headless army-vs-army resolution (see
    /// <see cref="TheatreCombatResolver"/>). Mechanically identical to
    /// <see cref="StructureObjectiveBase"/> (destroy-fraction win condition) —
    /// a deliberate simplification: neither side's army has any active defensive
    /// fire of its own in this pass (no symmetric "both sides shoot back"
    /// simulation, no per-army point-defense/garrison model yet), matching how site
    /// combat here also has no defender-side stockpile/point-defense. Revisit once
    /// theatre-level point-defense/garrisons are modeled.
    /// </summary>
    public class ArmyObjective : StructureObjectiveBase
    {
    }
}
