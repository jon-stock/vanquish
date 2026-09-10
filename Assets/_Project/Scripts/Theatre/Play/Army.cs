using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// A field force of drones/missiles deployed out of an Operational, owned
    /// Airfield (<see cref="SiteType.LaunchPlatform"/> — see
    /// <see cref="SiteStorageCatalog.CanDeployArmies"/>), visible on the theatre map
    /// at all times via <see cref="ArmyMarkerView"/> and movable 1 hex per turn
    /// (see <see cref="TheatreMapHarness.TryMoveArmy"/>). Composition is a single
    /// shared army-level pool (no per-drone missile loadout in this POC) — an army
    /// needs at least one missile to be combat effective (PLAN.md: "Drone armies
    /// must have missiles to be combat effective").
    /// </summary>
    public class Army
    {
        private static int _nextId = 1;

        // 10 adjectives x 10 nouns = 100 distinct army-sounding names to draw from
        // at random when a new army is deployed (see GenerateRandomName below).
        private static readonly string[] NameAdjectives =
        {
            "Iron", "Crimson", "Shadow", "Silent", "Storm",
            "Ember", "Frost", "Onyx", "Ghost", "Thunder",
        };

        private static readonly string[] NameNouns =
        {
            "Wolves", "Hawks", "Legion", "Vanguard", "Talons",
            "Fangs", "Reapers", "Sentinels", "Falcons", "Vipers",
        };

        /// <summary>Experience required to reach each <see cref="ArmyRank"/>, indexed by rank ordinal.</summary>
        private static readonly int[] RankThresholds = { 0, 2, 4, 7, 10, 14, 19, 25 };

        public int Id { get; }
        public TheatreFaction Owner { get; }
        public HexCoordinate Location { get; private set; }

        /// <summary>A randomly-assigned, human-readable name (see <see cref="GenerateRandomName"/>) shown on its map marker and info panel — purely cosmetic, not a gameplay id (see <see cref="Id"/> for that).</summary>
        public string Name { get; }

        /// <summary>Unit counts by designed Plan — both drones and missiles live in the same dictionary, distinguished by Plan.Category.</summary>
        public Dictionary<DronePlan, int> Composition { get; } = new Dictionary<DronePlan, int>();

        /// <summary>Reset to false every <see cref="TheatreMapHarness.AdvanceTurn"/> — an army can move at most 1 hex per turn.</summary>
        public bool HasMovedThisTurn { get; set; }

        /// <summary>Accrued via <see cref="AddExperience"/> (awarded by <see cref="TheatreCombatResolver"/> on a combat win) — determines <see cref="Rank"/>. Purely cosmetic progression in this POC, no stat bonuses yet.</summary>
        public int Experience { get; private set; }

        /// <summary>Mints a brand-new army with the next sequential id and a random name.</summary>
        public Army(TheatreFaction owner, HexCoordinate location) : this(_nextId++, owner, location, GenerateRandomName())
        {
        }

        /// <summary>Restores an army to a specific id/name (save/load) rather than minting a new one — also bumps the id counter so future new armies never collide with a loaded one.</summary>
        public Army(int id, TheatreFaction owner, HexCoordinate location, string name)
        {
            Id = id;
            Owner = owner;
            Location = location;
            Name = string.IsNullOrWhiteSpace(name) ? GenerateRandomName() : name;
            if (id >= _nextId)
                _nextId = id + 1;
        }

        /// <summary>Resets the shared id counter — call on "New Game" so ids stay small/predictable rather than climbing across sessions.</summary>
        public static void ResetIdCounterForNewGame() => _nextId = 1;

        private static string GenerateRandomName()
        {
            string adjective = NameAdjectives[UnityEngine.Random.Range(0, NameAdjectives.Length)];
            string noun = NameNouns[UnityEngine.Random.Range(0, NameNouns.Length)];
            return $"{adjective} {noun}";
        }

        /// <summary>The rank implied by the current <see cref="Experience"/> total — see <see cref="RankThresholds"/>.</summary>
        public ArmyRank Rank
        {
            get
            {
                ArmyRank rank = ArmyRank.Private;
                for (int i = 0; i < RankThresholds.Length; i++)
                {
                    if (Experience >= RankThresholds[i])
                        rank = (ArmyRank)i;
                }
                return rank;
            }
        }

        /// <summary>0..1 progress from the current rank's threshold toward the next rank's — 1 once at the top rank (General). Drives the rank progress slider in the army panel.</summary>
        public float RankProgress01
        {
            get
            {
                int rankIndex = (int)Rank;
                if (rankIndex >= RankThresholds.Length - 1)
                    return 1f;

                int current = RankThresholds[rankIndex];
                int next = RankThresholds[rankIndex + 1];
                return next <= current ? 1f : Mathf.Clamp01((Experience - current) / (float)(next - current));
            }
        }

        /// <summary>Awards experience (e.g. for winning a fight — see <see cref="TheatreCombatResolver"/>).</summary>
        public void AddExperience(int amount) => Experience = Mathf.Max(0, Experience + amount);

        /// <summary>Sets experience directly — save/load restoration only, bypasses the "award" framing of <see cref="AddExperience"/>.</summary>
        public void SetExperienceForRestore(int experience) => Experience = Mathf.Max(0, experience);

        public int DroneCount => Composition.Where(kv => kv.Key.Category != UnitCategory.Missile).Sum(kv => kv.Value);
        public int MissileCount => Composition.Where(kv => kv.Key.Category == UnitCategory.Missile).Sum(kv => kv.Value);

        /// <summary>
        /// Total missile-carrying capacity across every drone in this army — a hard
        /// pylon/hardpoint-count cap (see <see cref="DronePlan.MissilePylons"/>),
        /// deliberately independent of missile mass/payload. Enforced whenever
        /// missiles are added to an army (see <see cref="TheatreMapHarness.TryDeployArmy"/>/
        /// <see cref="TheatreMapHarness.TryRestockArmy"/>/<see cref="TheatreMapHarness.TryTransferUnits"/>)
        /// so an army can never carry more missiles than its drones have pylons for,
        /// no matter how light those missiles are individually.
        /// </summary>
        public int MissileCapacity => Composition.Where(kv => kv.Key.Category != UnitCategory.Missile).Sum(kv => kv.Key.MissilePylons * kv.Value);

        /// <summary>PLAN.md: "Drone armies must have missiles to be combat effective."</summary>
        public bool IsCombatEffective => DroneCount > 0 && MissileCount > 0;

        public bool IsEmpty => DroneCount <= 0 && MissileCount <= 0;

        public void MoveTo(HexCoordinate location) => Location = location;

        /// <summary>Removes up to <paramref name="amount"/> total missiles, spread across whichever missile Plans this army holds — used after a fight to reflect expended ammo.</summary>
        public void ConsumeMissiles(int amount)
        {
            foreach (DronePlan plan in Composition.Keys.Where(p => p.Category == UnitCategory.Missile).ToList())
            {
                if (amount <= 0)
                    break;

                int take = Mathf.Min(amount, Composition[plan]);
                Composition[plan] -= take;
                amount -= take;

                if (Composition[plan] <= 0)
                    Composition.Remove(plan);
            }
        }

        /// <summary>Adds units of a Plan to this army's composition (deploy/restock/merge/transfer-in — see <see cref="TheatreMapHarness"/>).</summary>
        public void AddUnits(DronePlan plan, int amount)
        {
            if (amount <= 0)
                return;

            Composition.TryGetValue(plan, out int count);
            Composition[plan] = count + amount;
        }

        /// <summary>Removes up to <paramref name="amount"/> units of a Plan from this army's composition — fails (no change) if it doesn't hold enough. Used for unloading back into storage or transferring to another army.</summary>
        public bool TryRemoveUnits(DronePlan plan, int amount)
        {
            if (amount <= 0)
                return true;
            if (!Composition.TryGetValue(plan, out int count) || count < amount)
                return false;

            int remaining = count - amount;
            if (remaining <= 0)
                Composition.Remove(plan);
            else
                Composition[plan] = remaining;
            return true;
        }

        /// <summary>Proportionally reduces every held Plan's count by <paramref name="fraction"/> (0..1) — used when this army takes losses without being wiped out outright.</summary>
        public void ApplyProportionalLosses(float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            foreach (DronePlan plan in Composition.Keys.ToList())
            {
                int reduced = Mathf.RoundToInt(Composition[plan] * (1f - fraction));
                if (reduced <= 0)
                    Composition.Remove(plan);
                else
                    Composition[plan] = reduced;
            }
        }
    }
}
