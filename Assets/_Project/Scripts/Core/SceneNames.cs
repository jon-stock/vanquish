namespace Vanquish.Core
{
    /// <summary>
    /// Phase 3A: the one place every "always exists, always registered" scene name is
    /// defined. Before this, every scene name was a bare string literal duplicated
    /// across WorkshopController/CombatManager/TestRangeTelemetry field defaults and
    /// each Editor scene builder's own local const — nothing wrong at runtime (they
    /// all happened to agree), but no single place to look, and easy to typo-drift.
    /// Scenario-specific combat arenas (Combat_Arena_Valley, Combat_Arena_Plateau, ...)
    /// deliberately stay data (ScenarioDefinition.sceneName), not constants here — the
    /// whole point of that asset is to add more of them without touching code.
    /// </summary>
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string Workshop = "Workshop";
        public const string TestRange = "TestRange";

        /// <summary>The single hardcoded fallback combat arena — used when no
        /// scenario was ever picked (e.g. a headless regression test that opens a
        /// combat scene directly without visiting the Workshop/Main Menu at all).</summary>
        public const string DefaultCombatArena = "Combat_Arena01";
    }
}
