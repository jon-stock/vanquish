using System;
using Vanquish.Data.Shared;

namespace Vanquish.Data.Missiles
{
    /// <summary>
    /// A player-assembled missile design: one part chosen per slot. Not a
    /// ScriptableObject asset — built at runtime by the Workshop UI from whichever
    /// PartDefinitions are currently unlocked, then handed to the spawner. Persisted
    /// via part ids in SaveData.SavedDesign, not by serializing this class directly.
    /// </summary>
    [Serializable]
    public class MissileLoadout
    {
        public string designName = "Missile";

        public MissilePayloadDefinition payload;
        public MissileEngineDefinition engine;
        public MissileAirframeDefinition airframe;
        public SeekerDefinition seeker;
        public FuelDefinition fuel;
        public CountermeasureDefinition countermeasure; // optional, may be null
        public JammingDefinition jamming; // optional, may be null

        public bool IsComplete => payload != null && engine != null && airframe != null && seeker != null && fuel != null;
    }
}
