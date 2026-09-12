namespace Vanquish.Theatre.Play
{
    /// <summary>An army's experience rank (see <see cref="Army.Experience"/>/<see cref="Army.Rank"/>) — purely cosmetic progression for this POC, no gameplay stat bonuses yet.</summary>
    public enum ArmyRank
    {
        Private,
        Corporal,
        Sergeant,
        Lieutenant,
        Captain,
        Major,
        Colonel,
        General,
    }

    public static class ArmyRankNames
    {
        /// <summary>Short label for compact UI (in-world name tags, narrow panels).</summary>
        public static string Abbreviation(ArmyRank rank) => rank switch
        {
            ArmyRank.Private => "Pvt",
            ArmyRank.Corporal => "Cpl",
            ArmyRank.Sergeant => "Sgt",
            ArmyRank.Lieutenant => "Lt",
            ArmyRank.Captain => "Capt",
            ArmyRank.Major => "Maj",
            ArmyRank.Colonel => "Col",
            ArmyRank.General => "Gen",
            _ => rank.ToString(),
        };
    }
}
