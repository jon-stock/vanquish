using System;

namespace Vanquish.Theatre
{
    /// <summary>
    /// A buildable theatre-map installation (PLAN.md Theatre Map). Deliberately plain
    /// C# (no MonoBehaviour) — this is simulation state a future UI/scene will
    /// visualize, not scene-bound behavior itself, consistent with the rest of the
    /// combat-instance layer's ScriptableObject/plain-data-vs-MonoBehaviour split.
    /// </summary>
    public class Site
    {
        public SiteType Type { get; }
        public TheatreFaction Owner { get; }
        public HexCoordinate Location { get; private set; }
        public SiteState State { get; private set; }
        public int TurnsRemaining { get; private set; }

        /// <summary>1 = full health. Only meaningful once/while the site has been Operational at least once.</summary>
        public float HealthFraction01 { get; private set; }

        public bool IsOperational => State == SiteState.Operational;

        private Site(SiteType type, TheatreFaction owner, HexCoordinate location)
        {
            Type = type;
            Owner = owner;
            Location = location;
        }

        /// <summary>Places a new site under construction. It becomes Operational after <paramref name="turnsToBuild"/> turn ticks.</summary>
        public static Site BeginConstruction(SiteType type, TheatreFaction owner, HexCoordinate location, int turnsToBuild)
        {
            var site = new Site(type, owner, location)
            {
                State = SiteState.UnderConstruction,
                TurnsRemaining = Math.Max(1, turnsToBuild),
                HealthFraction01 = 0f,
            };
            return site;
        }

        /// <summary>
        /// Reconstructs a site directly into an arbitrary saved state (save/load —
        /// see <c>Core.SaveData</c>/<c>SavedSite</c>), bypassing the normal
        /// construction/repair/relocation entry points since a loaded save already
        /// knows exactly what state each site was in.
        /// </summary>
        public static Site Restore(SiteType type, TheatreFaction owner, HexCoordinate location, SiteState state, int turnsRemaining, float healthFraction01)
        {
            return new Site(type, owner, location)
            {
                State = state,
                TurnsRemaining = turnsRemaining,
                HealthFraction01 = healthFraction01,
            };
        }

        /// <summary>Advance one turn of whatever this site is currently doing (build/repair/relocate). No-op if Operational or Destroyed.</summary>
        public void Tick()
        {
            if (State != SiteState.UnderConstruction && State != SiteState.Repairing && State != SiteState.Relocating)
                return;

            TurnsRemaining--;
            if (TurnsRemaining > 0)
                return;

            switch (State)
            {
                case SiteState.UnderConstruction:
                    State = SiteState.Operational;
                    HealthFraction01 = 1f;
                    break;
                case SiteState.Repairing:
                    State = SiteState.Operational;
                    HealthFraction01 = 1f;
                    break;
                case SiteState.Relocating:
                    State = SiteState.Operational;
                    // Relocation moves the site but does not repair it — health carries over.
                    break;
            }
        }

        /// <summary>Begin repairing a damaged, currently-Operational site over <paramref name="turns"/> turn ticks.</summary>
        public bool BeginRepair(int turns)
        {
            if (State != SiteState.Operational || HealthFraction01 >= 1f)
                return false;

            State = SiteState.Repairing;
            TurnsRemaining = Math.Max(1, turns);
            return true;
        }

        /// <summary>
        /// Begin relocating an Operational site to a new hex — PLAN.md's deliberately
        /// most expensive of the three site actions: offline/undefended for the
        /// duration. The site is considered to have left its old location immediately
        /// (it's non-operational at either endpoint while in transit either way).
        /// </summary>
        public bool BeginRelocation(HexCoordinate newLocation, int turns)
        {
            if (State != SiteState.Operational)
                return false;

            Location = newLocation;
            State = SiteState.Relocating;
            TurnsRemaining = Math.Max(1, turns);
            return true;
        }

        /// <summary>
        /// Apply combat-instance damage to this site (the hook for Phase 2's still-
        /// deferred combat-instance-to-theatre feedback loop — see PLAN.md). Reduces
        /// health; destroys the site outright if health reaches zero.
        /// </summary>
        public void ApplyDamage(float damageFraction01)
        {
            if (State == SiteState.Destroyed)
                return;

            HealthFraction01 = Math.Max(0f, HealthFraction01 - damageFraction01);
            if (HealthFraction01 <= 0f)
                State = SiteState.Destroyed;
        }
    }
}
