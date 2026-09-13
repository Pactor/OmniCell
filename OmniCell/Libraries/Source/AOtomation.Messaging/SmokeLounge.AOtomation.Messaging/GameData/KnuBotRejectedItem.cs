// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KnuBotRejectedItem.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the KnuBotRejectedItem type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One pattern of item the bot will not take.
    /// </summary>
    /// <remarks>
    /// Sixteen bytes, and what they are comes from the one thing that reads
    /// them back. The matcher at Gamecode 0x10128628 takes an item, asks it for
    /// three stats through virtual slot 0x3C - 0x2BE, 0x2BF and 0x36, which are
    /// acgitemtemplateid, acgitemtemplateid2 and level - and walks this list
    /// comparing the record's first word against the first stat, its second
    /// against the second and its third against the level. A record whose third
    /// word is -1 is skipped by the level test, so -1 there means any quality.
    /// On a match it erases the record from the list, so a pattern is spent
    /// once.
    ///
    /// The first eight bytes were two int32s, then an Identity, and now two
    /// int32s again. The reader does take them with the client's identity
    /// reader, which is what the Identity was based on, but the matcher is the
    /// stronger evidence: it compares the two halves against two item template
    /// stats, and an identity type of 295756 is not a type. The one captured
    /// record carries 295756 in both halves, which is the ACGItem convention of
    /// a high id defaulting to the low one.
    ///
    /// The whole record is the ACGItem shape - two template ids, a quality and
    /// a fourth word - and GameData's own ACGItem operator reads four int32s
    /// and drops the fourth in the same way this matcher never looks at it.
    /// </remarks>
    public class KnuBotRejectedItem
    {
        #region AoMember Properties

        /// <summary>
        /// Matched against the item's acgitemtemplateid, stat 702.
        /// </summary>
        [AoMember(0)]
        public int ItemTemplateId { get; set; }

        /// <summary>
        /// Matched against acgitemtemplateid2, stat 703.
        /// </summary>
        [AoMember(1)]
        public int ItemTemplateId2 { get; set; }

        /// <summary>
        /// Matched against the item's level, stat 54, which for an item is its
        /// quality. -1 matches any.
        /// </summary>
        [AoMember(2)]
        public int QualityLevel { get; set; }

        /// <summary>
        /// A fourth word the matcher never looks at.
        /// </summary>
        /// <remarks>
        /// 1234567890 in the one captured record, which is this protocol's "not
        /// set" sentinel, and the same position ACGItem's own stream operator
        /// reads and discards.
        /// </remarks>
        [AoMember(3)]
        public int Unused { get; set; }

        #endregion
    }
}
