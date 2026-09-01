using System;
using UnityEngine;
using Vanquish.Data.Shared;
using Vanquish.Data.Support;

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

        [Tooltip("Optional datalink network part (Phase 2C) — when its supportsMidCourseUpdates is " +
            "true, GuidanceLawFactory wraps the missile's normal seeker-based guidance in " +
            "DatalinkMidCourseGuidance so it flies toward periodically-relayed target data before its " +
            "own seeker is in range, instead of needing a lock for its entire flight.")]
        public DatalinkNetworkDefinition datalink; // optional, may be null

        [Tooltip("Continuous fuel tank fill level (0 = empty, 1 = full capacity) — the " +
            "'Continuous Sliders' concept from the design doc. Trades range/burn time " +
            "against total mass and MTOW headroom for other parts.")]
        [Range(0f, 1f)]
        public float fuelFillFraction = 1f;

        public bool IsComplete => payload != null && engine != null && airframe != null && seeker != null && fuel != null;
    }
}
