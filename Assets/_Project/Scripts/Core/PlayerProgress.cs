using System.Collections.Generic;
using UnityEngine;
using Vanquish.Data;
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
