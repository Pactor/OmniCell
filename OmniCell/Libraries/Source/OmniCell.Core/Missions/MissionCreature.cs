namespace OmniCell.Core.Missions
{
    /// <summary>
    /// A creature a mission in this pool has been seen to spawn.
    /// </summary>
    /// <remarks>
    /// Measured, not authored: 239 of these came out of the AOBuddy10 bot's
    /// packet recordings, over the six pools it has run.
    /// <c>Tools/Capture/MobExtract</c> builds the table and
    /// <c>PoolExtract</c> folds it into the pack; re-run both as the bot
    /// records more.
    ///
    /// Three things had to be got right for it to mean anything.
    ///
    /// A recording begins at the previous zone-in, so the creatures the bot
    /// walked past on the way are in the stream too. Every update says which
    /// playfield it is in and the zone-in says which instance the mission is,
    /// so the cut is the server's own answer rather than a guess at the
    /// building's extent: it removed 463 of them, and with them every absurd
    /// level.
    ///
    /// The bot's own pets stand in every mission it runs - it is a
    /// Meta-Physicist - and a pet has a body like any creature. They are cut
    /// on PetMaster, which carries the owner's dynel. Not on the IsPet flag:
    /// that bit reads set on ordinary mission creatures too, and trusting it
    /// throws away most of the table.
    ///
    /// And the level is given against the mission's QL rather than as it
    /// arrived, because that is what it follows. The QL is the server's own
    /// word for it - QuestInfo.Quality, off the quest in the same stream -
    /// not the player's level, which stood still while the monsters moved.
    /// </remarks>
    public class MissionCreature
    {
        /// <summary>
        /// The body, which is SimpleCharFullUpdate's MonsterData.
        /// </summary>
        /// <remarks>
        /// One body covers a creature's rank variants: 17649 arrives as both
        /// "34 - Automatic" and "34-V worker", 17697 as "A-500 elite" and
        /// "A-500 soldier". So the name belongs to the row and the body does
        /// not identify it on its own.
        /// </remarks>
        public int Monster { get; set; }

        /// <summary>
        /// What it was called.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// How many were seen, which is the weight to choose by.
        /// </summary>
        public int Seen { get; set; }

        /// <summary>
        /// Its level against the mission's QL, at the low end.
        /// </summary>
        /// <remarks>
        /// Across the whole table the offsets run -4 to +3.
        /// </remarks>
        public int MinLevelOffset { get; set; }

        /// <summary>
        /// Its level against the mission's QL, at the high end.
        /// </summary>
        public int MaxLevelOffset { get; set; }

        /// <summary>
        /// How much health this creature has for each level it is.
        /// </summary>
        /// <remarks>
        /// The median of every sighting in the bot's recordings, and it is a
        /// property of the creature rather than of the level: 889 monsters
        /// over fifteen levels run from 7.5 health a level to 80.3, in two
        /// clear bands, so nothing about the level alone predicts it.
        /// </remarks>
        public double HealthPerLevel { get; set; }
    }
}
