namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// A single researchable node in the theatre map's tech tree. Deliberately a
    /// flat, hardcoded placeholder catalog — the same "plain static data" pattern
    /// as <see cref="SiteBuildCatalog"/> — rather than <c>Data.TechTree.TechNode</c>
    /// ScriptableObject assets, since none of those exist in this project yet (see
    /// PLAN.md). Unlocking a node here only tracks player progress
    /// (persisted via <c>SaveData.unlockedTechNodeIds</c>); it does not yet grant
    /// any concrete <c>PartDefinition</c>/unit bonus — the same documented,
    /// deliberate POC gap as the rest of the Lab's design system.
    /// </summary>
    public readonly struct TheatreTechNode
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly int ResearchCost;
        public readonly string[] PrerequisiteIds;

        public TheatreTechNode(string id, string displayName, string description, int researchCost, params string[] prerequisiteIds)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            ResearchCost = researchCost;
            PrerequisiteIds = prerequisiteIds ?? System.Array.Empty<string>();
        }
    }

    public static class TheatreTechCatalog
    {
        public static readonly TheatreTechNode[] Nodes =
        {
            new TheatreTechNode("airframe_composites", "Improved Airframes", "Lighter, stronger drone frames.", 20),
            new TheatreTechNode("propulsion_efficiency", "Propulsion Efficiency", "More efficient rotor/engine output.", 20),
            new TheatreTechNode("seeker_optics", "Seeker Optics", "Sharper missile terminal guidance.", 30),
            new TheatreTechNode("warhead_yield", "Warhead Yield", "Denser payload packing per missile.", 30),
            new TheatreTechNode("composite_hulls", "Composite Hulls", "Damage-resistant hull materials.", 40, "airframe_composites"),
            new TheatreTechNode("ecm_suite", "ECM Countermeasures", "Jamming/decoy countermeasure suite.", 40, "seeker_optics"),
            new TheatreTechNode("long_range_fuel", "Extended Fuel Cells", "Longer-range fuel/battery systems.", 50, "propulsion_efficiency"),
            new TheatreTechNode("advanced_payloads", "Advanced Payloads", "Next-tier missile payload design.", 60, "warhead_yield", "ecm_suite"),
        };

        public static TheatreTechNode? Find(string id)
        {
            foreach (TheatreTechNode node in Nodes)
            {
                if (node.Id == id)
                    return node;
            }
            return null;
        }
    }
}
