namespace Vanquish.Theatre.Play
{
    /// <summary>One in-progress build of a <see cref="DronePlan"/> at a specific Factory, ticked down by <see cref="TheatreMapHarness.AdvanceTurn"/>. Completed units are delivered into <see cref="Destination"/>'s storage (an Airfield or Warehouse with room) rather than a global player-wide pool.</summary>
    public class ProductionOrder
    {
        public DronePlan Plan { get; }
        public int TurnsRemaining { get; set; }

        /// <summary>The Airfield/Warehouse this order's finished output is queued to be delivered into, chosen by the player when the order was queued.</summary>
        public Site Destination { get; }

        public ProductionOrder(DronePlan plan, int turnsRemaining, Site destination)
        {
            Plan = plan;
            TurnsRemaining = turnsRemaining;
            Destination = destination;
        }
    }
}
