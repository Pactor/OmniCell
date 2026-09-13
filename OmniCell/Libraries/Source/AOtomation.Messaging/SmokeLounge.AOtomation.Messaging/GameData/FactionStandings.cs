// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// The twelve faction standings, behind InfoPacket's flag 0x20.
    /// </summary>
    /// <remarks>
    /// Twelve int32s that no capture has ever carried. The inherited comment
    /// called the flag hasFaction and guessed a standing per faction; the
    /// reader turns that guess into the client's own word. It files the
    /// record's + 0xE0 through + 0x10C into the examined character's stat
    /// table with 0x231 to 0x23C, from 0x10046574 onward - stats 561 to 572,
    /// in this order.
    ///
    /// Called InfoPacketExtraBlock, with twelve fields called Unknown1 to
    /// Unknown12, until 2026-09-12.
    /// </remarks>
    public class FactionStandings
    {
        #region AoMember Properties

        /// <summary>
        /// Standing with clansentinels - stat 561.
        /// </summary>
        [AoMember(0)]
        public int ClanSentinels { get; set; }

        /// <summary>
        /// Standing with otmed - stat 562.
        /// </summary>
        [AoMember(1)]
        public int OtMed { get; set; }

        /// <summary>
        /// Standing with clangaia - stat 563.
        /// </summary>
        [AoMember(2)]
        public int ClanGaia { get; set; }

        /// <summary>
        /// Standing with ottrans - stat 564.
        /// </summary>
        [AoMember(3)]
        public int OtTrans { get; set; }

        /// <summary>
        /// Standing with clanvanguards - stat 565.
        /// </summary>
        [AoMember(4)]
        public int ClanVanguards { get; set; }

        /// <summary>
        /// Standing with gos - stat 566.
        /// </summary>
        [AoMember(5)]
        public int Gos { get; set; }

        /// <summary>
        /// Standing with otfollowers - stat 567.
        /// </summary>
        [AoMember(6)]
        public int OtFollowers { get; set; }

        /// <summary>
        /// Standing with otoperator - stat 568.
        /// </summary>
        [AoMember(7)]
        public int OtOperator { get; set; }

        /// <summary>
        /// Standing with otunredeemed - stat 569.
        /// </summary>
        [AoMember(8)]
        public int OtUnredeemed { get; set; }

        /// <summary>
        /// Standing with clandevoted - stat 570.
        /// </summary>
        [AoMember(9)]
        public int ClanDevoted { get; set; }

        /// <summary>
        /// Standing with clanconserver - stat 571.
        /// </summary>
        [AoMember(10)]
        public int ClanConserver { get; set; }

        /// <summary>
        /// Standing with clanredeemed - stat 572.
        /// </summary>
        [AoMember(11)]
        public int ClanRedeemed { get; set; }

        #endregion
    }
}
