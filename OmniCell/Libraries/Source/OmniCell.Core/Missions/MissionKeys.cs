namespace OmniCell.Core.Missions
{
    /// <summary>
    /// The item a mission hands over, and what it is called.
    /// </summary>
    /// <remarks>
    /// Identity type 51053, template 28577, quality 1 whatever the mission's
    /// own quality is - the key on a QL 33 mission reads ACGItemLevel 1. The
    /// name is the whole of what it carries: "Mission key to A building in
    /// Borealis" and nothing else distinguishes one key from another, which is
    /// why a duplicate works. See Documentation/Missions.md.
    /// </remarks>
    public static class MissionKeys
    {
        /// <summary>
        /// The key's template, both halves, from itemnames: "Mission Key to".
        /// </summary>
        public const int Template = 28577;

        /// <summary>
        /// The Mission Key Duplicator's template. It is not consumed by use.
        /// </summary>
        public const int DuplicatorTemplate = 28564;

        /// <summary>
        /// The quality every captured mission key carries.
        /// </summary>
        public const int Quality = 1;

        /// <summary>
        /// What the key to this mission is called.
        /// </summary>
        public static string Name(MissionOffer offer)
        {
            return offer == null ? string.Empty : "Mission key to " + offer.Building;
        }
    }
}
