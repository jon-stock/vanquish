namespace Vanquish.Theatre.Play
{
    /// <summary>
    /// One in-progress logistics shipment of a Plan between two storage-capable
    /// sites (e.g. a Warehouse sending stock out to an Airfield — see
    /// <see cref="TheatreMapHarness.TryBeginTransfer"/>). The shipped amount is
    /// deducted from <see cref="Source"/>'s storage the instant the transfer is
    /// queued (so the player sees it leave immediately) and only added to
    /// <see cref="Destination"/>'s storage once <see cref="TurnsRemaining"/>
    /// reaches zero — always exactly 1 turn, regardless of the two sites'
    /// distance apart (no travel-time-by-distance modeling in this POC).
    /// </summary>
    public class TransferOrder
    {
        public Site Source { get; }
        public Site Destination { get; }
        public DronePlan Plan { get; }
        public int Amount { get; }
        public int TurnsRemaining { get; set; }

        public TransferOrder(Site source, Site destination, DronePlan plan, int amount, int turnsRemaining)
        {
            Source = source;
            Destination = destination;
            Plan = plan;
            Amount = amount;
            TurnsRemaining = turnsRemaining;
        }
    }
}
