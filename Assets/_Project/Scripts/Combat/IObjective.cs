namespace Vanquish.Combat
{
    /// <summary>
    /// Phase 2E: pluggable victory-condition strategy for CombatManager. Defeat ("all
    /// player units destroyed") remains a universal rule inside CombatManager itself —
    /// only the *victory* condition varies per scenario, per this sub-milestone's own
    /// scope ("CombatManager's win condition to become pluggable").
    ///
    /// Deliberately a plain C# interface, not a MonoBehaviour/ScriptableObject:
    /// CombatManager constructs the right implementation at runtime (in Awake, from
    /// its own serialized objectiveType enum + objectiveTarget reference — see
    /// CombatManager.BuildObjective) rather than holding a live reference across a
    /// scene save/reload, since a plain interface reference assigned by an Editor
    /// scene-builder script would NOT survive Unity's scene serialization (it isn't a
    /// UnityEngine.Object reference or a [Serializable] value type). Storing
    /// (enum, GameObject reference) and building the strategy object in code is the
    /// standard Unity-idiomatic way to make a serializable "pluggable strategy."
    /// </summary>
    public interface IObjective
    {
        /// <summary>Player-facing summary, shown by HUDController alongside the VICTORY/DEFEAT banner.</summary>
        string Description { get; }

        /// <summary>Polled by CombatManager whenever any registered unit is destroyed.</summary>
        bool IsVictoryAchieved();
    }
}
