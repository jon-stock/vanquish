namespace Vanquish.Theatre.Play
{
    /// <summary>One in-progress build of a <see cref="DronePlan"/> at a specific Factory, ticked down by <see cref="TheatreMapHarness.AdvanceTurn"/>.</summary>
    public class ProductionOrder
    {
        public DronePlan Plan { get; }
        public int TurnsRemaining { get; set; }

        public ProductionOrder(DronePlan plan, int turnsRemaining)
        {
            Plan = plan;
            TurnsRemaining = turnsRemaining;
        }
    }
}
