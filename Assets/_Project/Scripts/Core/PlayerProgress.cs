using System.Collections.Generic;
using UnityEngine;
using Vanquish.Data;
using Vanquish.Data.Drones;
using Vanquish.Data.TechTree;

namespace Vanquish.Core
{
    /// <summary>
    /// Runtime progression state: currency and unlocked tech nodes/parts. Wraps
    /// SaveData so both the Workshop UI and Combat's reward flow read/write through
    /// one place. Persists across scene loads via DontDestroyOnLoad; call Load() once
    /// at game start (see GameBootstrap).
    /// </summary>
    public class PlayerProgress : MonoBehaviour
    {
        public static PlayerProgress Instance { get; private set; }

        public int Currency { get; private set; }
        private readonly HashSet<string> _unlockedTechNodeIds = new HashSet<string>();

        /// <summary>
        /// The player's currently-configured strike/scout drone designs, carried across
        /// the Workshop -> Combat scene transition. Set by WorkshopController.
        /// OnEnterCombatClicked right before it loads the combat scene; read (and left
        /// set, not cleared) by CombatPlayerLoadoutApplier at the start of the combat
        /// scene so the player's own drone actually reflects what was picked in the
        /// Workshop, instead of Combat_Arena01's editor-time-baked Tier-0 default.
        /// Deliberately in-memory only (not part of SaveData/SaveSystem) — this is
        /// transient "what to spawn next" state, not persistent progression; it
        /// survives the scene load because this MonoBehaviour is DontDestroyOnLoad,
        /// and resets naturally on app restart along with everything else not written
        /// to disk. Null means "no override — keep whatever's already baked into the
        /// combat scene", which is what lets Combat_Arena01 still work when entered
        /// directly (headless batch regression tests, or opening the scene without ever
        /// visiting Workshop) instead of crashing on a missing loadout.
        /// </summary>
        public DroneLoadout PendingStrikeDroneLoadout { get; set; }
        public DroneLoadout PendingScoutDroneLoadout { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Load()
        {
            SaveData data = SaveSystem.Load();
            Currency = data.currency;
            _unlockedTechNodeIds.Clear();
            foreach (var id in data.unlockedTechNodeIds)
                _unlockedTechNodeIds.Add(id);
        }

        public void Save()
        {
            SaveData data = SaveSystem.Load(); // preserve fields this class doesn't own (designs, missions)
            data.currency = Currency;
            data.unlockedTechNodeIds = new List<string>(_unlockedTechNodeIds);
            SaveSystem.Save(data);
        }

        public bool IsUnlocked(TechNode node) => node != null && _unlockedTechNodeIds.Contains(node.id);

        public bool CanAfford(int cost) => Currency >= cost;

        public bool TryUnlock(TechNode node)
        {
            if (node == null || IsUnlocked(node))
                return false;

            foreach (var prereq in node.prerequisites)
            {
                if (!IsUnlocked(prereq))
                    return false;
            }

            if (!CanAfford(node.researchCost))
                return false;

            Currency -= node.researchCost;
            _unlockedTechNodeIds.Add(node.id);
            Save();
            return true;
        }

        public void AddCurrency(int amount)
        {
            if (amount <= 0)
                return;
            Currency += amount;
            Save();
        }

        /// <summary>Convenience for Workshop UI: is a specific part currently available to use in a design?</summary>
        public bool IsPartUnlocked(PartDefinition part, IEnumerable<TechNode> allNodes)
        {
            if (part == null)
                return false;

            foreach (var node in allNodes)
            {
                if (!IsUnlocked(node))
                    continue;
                foreach (var unlockedPart in node.unlocks)
                {
                    if (unlockedPart == part)
                        return true;
                }
            }
            return false;
        }
    }
}
