// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OrgKind1Message.cs" company="OmniCell">
//   Copyright (c) 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the OrgKind1Message type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages.OrgServerMessages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
    /// <summary>
    /// OrgServer kind 1.
    /// </summary>
    /// <remarks>
    /// A container and then a flag byte. The branch at 0x10126EF8 builds a
    /// 0x2C byte object at 0x1002B03C and has it read itself at 0x1002A5DB,
    /// and both of those are shared rather than this message's own:
    ///
    ///   0x1002A5DB is the container reader Bank, BankCorpse, Inspect and
    ///   FullCharacter's inventory all use - an X3F1 count and then, per
    ///   entry, a placement, two int16s, an Identity and a
    ///   GameData::ACGItem_t. That is <see cref="InventorySlot"/>, which this
    ///   model has had all along.
    ///
    ///   0x1002B03C takes a page number and an Identity. Inspect builds its
    ///   container with page 0x40 and the character it is inspecting; this one
    ///   passes 0x1E and the message's second Identity, so the 0x1E recorded
    ///   as an unexplained argument is the container's page.
    ///
    /// So kind 1 is the contents of container page 0x1E belonging to that
    /// Identity, which is what an organization's bank would look like. The
    /// byte after it goes through 0x100A0138, the helper that normalises a
    /// byte to 0 or 1, and lands at the message's + 0xF4.
    ///
    /// Added on 2026-09-11 with the other five kinds nothing had modelled. The
    /// reader at Gamecode 0x10126D95 takes the kind byte and refuses the whole
    /// message unless it is 1 to 9; the subclass table here held 2, 5 and 6, so
    /// a copy of any other kind would have matched no subclass and its body
    /// would not have been read at all. Every one of the 404 captured copies is
    /// a kind 6, so nothing had ever shown it.
    /// </remarks>
    [AoContract((byte)OrgServerMessageType.OrgKind1)]
    public class OrgKind1Message : OrgServerMessage
    {
        #region Constructors and Destructors

        public OrgKind1Message()
        {
            this.OrgServerMessageType = OrgServerMessageType.OrgKind1;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The container's contents, X3F1 counted.
        /// </summary>
        [AoMember(0, SerializeSize = ArraySizeType.X3F1)]
        public InventorySlot[] Contents { get; set; }

        /// <summary>
        /// A flag, normalised to 0 or 1 by 0x100A0138 and kept at + 0xF4.
        /// </summary>
        [AoMember(1)]
        public byte Flag { get; set; }

        #endregion
    }
}
