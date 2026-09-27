namespace OmniCell.Core.Missions
{
    using System;

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
        /// What this creature's maximum health is a multiple of.
        /// </summary>
        /// <remarks>
        /// Maximum health is <see cref="Health"/>: one ramp in level that
        /// every creature shares, times a number that belongs to the creature.
        /// This is that number. It came out near 0.185, 0.8, 1.0, 1.2, 1.4 or
        /// 2.0 for almost every creature measured, which reads like a handful
        /// of classes, but it is stored as measured rather than snapped to
        /// them because three creatures sit between.
        ///
        /// It replaced a health-per-level, which was wrong in a way worth
        /// remembering: the ramp does not pass through the origin, so dividing
        /// health by level gives a number that only holds at the level it was
        /// measured at. A Hellhound at level 20 has 1,117 health and the old
        /// table's 78.8 a level predicted 1,576.
        /// </remarks>
        public double HealthScale { get; set; }

        /// <summary>
        /// The maximum health of a creature of this kind at this level.
        /// </summary>
        /// <remarks>
        /// Measured over 559 distinct level-and-health pairs in the bot's run
        /// recordings, covering levels 19 to 44 and 138 creatures. Health is
        /// piecewise linear in level with a knee at exactly 25 - the two lines
        /// meet there to within a tenth of a point - and every creature's
        /// health is the same ramp times its own <see cref="HealthScale"/>.
        ///
        /// It reproduces 539 of the 559 exactly and every one of them to
        /// within a single point, the remainder being where the server rounds
        /// differently than this does.
        ///
        /// Nothing was recorded below 19 or above 44, so both ends are the
        /// measured lines carried on. The lower one reaching zero at level 3
        /// is where it stops meaning anything; a creature is never given less
        /// than one point of health.
        /// </remarks>
        public static int Health(double scale, int level)
        {
            if (scale <= 0)
            {
                // Seen, but never while its health was on the wire. The middle
                // of the measured range, because a creature with no health
                // cannot be fought at all.
                scale = 1.0;
            }

            level = Math.Max(1, level);
            double ramp = level <= Knee
                              ? (LowSlope * level) - LowBase
                              : (HighSlope * level) - HighBase;

            return Math.Max(1, (int)Math.Round(scale * ramp, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// The level the two halves of the ramp meet at.
        /// </summary>
        private const int Knee = 25;

        private const double LowSlope = 33.0;

        private const double LowBase = 101.0;

        private const double HighSlope = 185.0 / 3.0;

        private const double HighBase = 2452.0 / 3.0;
    }
}
