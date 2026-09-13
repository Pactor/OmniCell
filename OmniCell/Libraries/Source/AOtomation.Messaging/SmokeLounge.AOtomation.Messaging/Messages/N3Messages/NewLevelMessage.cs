// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NewLevelMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the NewLevelMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Sent when a character gains a level.
    /// </summary>
    /// <remarks>
    /// N3MessageType.NewLevel was in the enum with no class behind it, so these
    /// never deserialised. Reconstructed from 7 captured across several 18.8.x
    /// sessions, all 61 bytes:
    ///
    ///   Level  Unknown1  Experience  ThisLevel  NextLevel  U2  U3  U4
    ///       2      4662        1450       1450       4050   0   4   145
    ///       3      6471        4310       4050       7150   0   4   520
    ///       3      6631        4050       4050       7150   0   4   260
    ///       2      4444        1450       1450       4050   0   4   145
    ///       3      6515        4310       4050       7150   0   4   520
    ///       4      7798        7410       7150      11150   0   4   310
    ///       5      8400       11825      11150      15650   0   4   800
    ///
    /// The three experience fields identify themselves. ThisLevel for a given
    /// level always equals NextLevel for the level below - 4050 at level 3 is
    /// 4050 at level 2, 7150 at level 4 is 7150 at level 3, and so on - which
    /// fixes those two as the thresholds either side. Experience then falls
    /// within that band in all seven, which fixes it as the running total.
    ///
    /// Unknown1, Unknown3 and Unknown4 are left unnamed. Unknown3 was 4 in all
    /// seven and Unknown4 moved without tracking level or experience, so
    /// neither can be called from this sample.
    /// </remarks>
    [AoContract((int)N3MessageType.NewLevel)]
    public class NewLevelMessage : N3Message
    {
        #region Constructors and Destructors

        public NewLevelMessage()
        {
            this.N3MessageType = N3MessageType.NewLevel;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The level just reached.
        /// </summary>
        [AoMember(0)]
        public int Level { get; set; }

        [AoMember(1)]
        /// <summary>
        /// The character's improvement point balance after the level.
        /// </summary>
        /// <remarks>
        /// Named on 2026-09-12 from the dispatcher rather than from the
        /// captures, which is why it stayed anonymous: 0x10075FB4 pushes the
        /// message's + 0x1C with stat 0x35 - 53, IP - into the setter on the
        /// character's stat table. The client writes this straight into the
        /// player's IP, so a server that sends zero here zeroes their
        /// improvement points on every level.
        /// </remarks>
        public int ImprovementPoints { get; set; }

        /// <summary>
        /// Total experience, which sits between <see cref="ExperienceThisLevel"/>
        /// and <see cref="ExperienceNextLevel"/>.
        /// </summary>
        [AoMember(2)]
        public int Experience { get; set; }

        /// <summary>
        /// Experience needed for the level just reached.
        /// </summary>
        [AoMember(3)]
        public int ExperienceThisLevel { get; set; }

        /// <summary>
        /// Experience needed for the next level.
        /// </summary>
        [AoMember(4)]
        public int ExperienceNextLevel { get; set; }

        /// <summary>
        /// 0 in every captured sample.
        /// </summary>
        [AoMember(5)]
        /// <summary>
        /// The character's title level, written only when positive.
        /// </summary>
        /// <remarks>
        /// The message's + 0x2C. The dispatcher writes it to stat 37,
        /// titlelevel, on a branch of its own rather than in the run of
        /// unconditional stat writes, which is why it is not beside the others
        /// at 0x10075FA1 onward.
        /// </remarks>
        public int TitleLevel { get; set; }

        /// <summary>
        /// 4 in every captured sample.
        /// </summary>
        [AoMember(6)]
        /// <summary>
        /// The experience kill range.
        /// </summary>
        /// <remarks>
        /// The message's + 0x30, pushed at 0x10075FDB with stat 0x113 - 275,
        /// xpkillrange. 4 in all seven captured messages.
        /// </remarks>
        public int ExperienceKillRange { get; set; }

        [AoMember(7)]
        /// <summary>
        /// The experience the event that caused the level awarded.
        /// </summary>
        /// <remarks>
        /// The message's + 0x34, and the one field of the eight that does not
        /// go into a stat. The dispatcher reads it at 0x10075FFA, skips the
        /// rest when it is zero, and otherwise hands it to the Feedback_NewLevel
        /// string - so it is what the player is told they gained. The captures
        /// agree arithmetically: previous total plus this equals the total in
        /// the same message.
        /// </remarks>
        public int ExperienceAward { get; set; }

        #endregion
    }
}
