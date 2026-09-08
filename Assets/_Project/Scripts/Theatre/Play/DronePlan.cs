using UnityEngine;

namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// A player-named design created at a <see cref="SiteType.Lab"/> — what a
    /// Factory can choose to build (PLAN.md's "army building" activity, in
    /// deliberately minimal POC form: a name + category + an accent color for its
    /// preview, not yet the full modular part-composition design system the
    /// combat-instance side already has). Shown as an actual 3D preview model both
    /// at the Lab that designed it and at any Factory building it — see
    /// <see cref="PlanPreviewBuilder"/>.
    /// </summary>
    public class DronePlan
    {
        public string Name { get; }
        public UnitCategory Category { get; }
        public Color AccentColor { get; }

        public DronePlan(string name, UnitCategory category, Color accentColor)
        {
            Name = name;
            Category = category;
            AccentColor = accentColor;
        }
    }
}
