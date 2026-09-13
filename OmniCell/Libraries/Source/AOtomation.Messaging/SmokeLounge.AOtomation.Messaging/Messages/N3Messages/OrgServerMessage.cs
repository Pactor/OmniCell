// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OrgServerMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the OrgServerMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.OrgServer)]
    [AoKnownType(29, IdentifierType.Byte)]
    public abstract class OrgServerMessage : N3Message
    {
        #region Constructors and Destructors

        protected OrgServerMessage()
        {
            this.N3MessageType = N3MessageType.OrgServer;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        [AoFlags("orgKind")]
        public OrgServerMessageType OrgServerMessageType { get; set; }

        /// <summary>
        /// An Identity, empty in all 130 captured copies, that the client reads
        /// and writes and never once looks at.
        /// </summary>
        /// <remarks>
        /// It was two int32s here, which is the same eight bytes and the wrong
        /// shape: the reader at Gamecode.dll 0x10126DD2 takes it with the
        /// standard Identity reader, in the same call that takes the
        /// organization after it, and the writer at 0x10126C6C puts it back the
        /// same way.
        ///
        /// Nothing else in the client touches the message at that offset - not
        /// the dispatcher, not any of the nine per-type branches - so what it is
        /// for is not in this module. Every captured copy is an OrgContract and
        /// carries 0:0, which is one message type out of nine and no evidence
        /// about the rest.
        /// </remarks>
        [AoMember(1)]
        public Identity Unknown1 { get; set; }

        /// <summary>
        /// Which organization the message is about.
        /// </summary>
        [AoMember(2)]
        public Identity Organization { get; set; }

        /// <summary>
        /// Only two of the message types carry this.
        /// </summary>
        /// <remarks>
        /// It used to be read for every type, and for OrgContract that is
        /// wrong: the reader's branch for type 6 at Gamecode 0x10126E13 takes
        /// three int32s and a byte and no string at all. Reading a string there
        /// eats the top half of the first int32, and it survived only because
        /// that half is always zero - all 404 captured copies are OrgContracts
        /// and every one round tripped through the mistake.
        ///
        /// The types that do read a string into this member are 5, whose branch
        /// at 0x10126E67 takes one and then an int32, and 2, whose branch at
        /// 0x10126E87 takes eight of them and this is the first.
        /// </remarks>
        [AoMember(3, SerializeSize = ArraySizeType.Int16)]
        [AoUsesFlags("orgKind", typeof(string), FlagsCriteria.EqualsToAny,
            new[] { (int)OrgServerMessageType.OrgInfo, (int)OrgServerMessageType.OrgInvite })]
        public string OrganizationName { get; set; }

        #endregion
    }
}