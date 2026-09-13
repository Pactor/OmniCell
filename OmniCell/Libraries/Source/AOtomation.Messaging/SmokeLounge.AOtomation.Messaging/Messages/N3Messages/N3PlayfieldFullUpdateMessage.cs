// --------------------------------------------------------------------------------------------------------------------
// <copyright file="N3PlayfieldFullUpdateMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the N3PlayfieldFullUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Which playfield the character is in, without the two world-map numbers
    /// PlayfieldAnarchyF adds.
    /// </summary>
    /// <remarks>
    /// n3PlayfieldFullUpdateIIR_t sent as itself. PlayfieldAnarchyF is the same
    /// class with two int32s on the end - its reader at Gamecode 0x101258E3
    /// calls ReadSubClass, exported from N3.dll at 0x10029C24, and then reads
    /// those two - so everything here is already settled by that message, which
    /// has seventeen captures behind it. This one has none.
    ///
    /// The fields carry the same meanings and the same evidence. See
    /// <see cref="PlayfieldAnarchyFMessage"/>, and
    /// <see cref="IPlayfieldFullUpdate"/> for why one reader serves both.
    /// </remarks>
    [AoContract((int)N3MessageType.N3PlayfieldFullUpdate)]
    public class N3PlayfieldFullUpdateMessage : N3Message, IPlayfieldFullUpdate
    {
        #region Constructors and Destructors

        public N3PlayfieldFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.N3PlayfieldFullUpdate;
            this.Unknown = 0x00;
            this.Version = 0x00000004;
            this.TokenMarker = 0x61;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// A version, 4, and it decides how much of the message exists.
        /// </summary>
        /// <remarks>
        /// Above 1 a playfield proxy follows, above 3 a DbObject_t. See
        /// <see cref="PlayfieldAnarchyFMessage.Version"/>.
        /// </remarks>
        [AoMember(0)]
        public int Version { get; set; }

        /// <summary>
        /// Where the character stands in it.
        /// </summary>
        [AoMember(1)]
        public Vector3 CharacterCoordinates { get; set; }

        /// <summary>
        /// 0x61, and anything else abandons the message with "Invalid
        /// playfieldproxy version".
        /// </summary>
        [AoMember(2)]
        public byte TokenMarker { get; set; }

        /// <summary>
        /// Which playfield this is a copy of. See
        /// <see cref="PlayfieldAnarchyFMessage.ModelId"/>.
        /// </summary>
        [AoMember(3)]
        public Identity ModelId { get; set; }

        /// <summary>
        /// Which group of copies of that model this one belongs to. See
        /// <see cref="PlayfieldAnarchyFMessage.Group"/>.
        /// </summary>
        [AoMember(4)]
        public int Group { get; set; }

        /// <summary>
        /// Which copy within that group. See
        /// <see cref="PlayfieldAnarchyFMessage.Subgroup"/>.
        /// </summary>
        [AoMember(5)]
        public int Subgroup { get; set; }

        /// <summary>
        /// The playfield's own identity. See
        /// <see cref="PlayfieldAnarchyFMessage.PlayfieldId"/>.
        /// </summary>
        [AoMember(6)]
        public Identity PlayfieldId { get; set; }

        /// <summary>
        /// The recipe for a generated playfield, when this is a mission.
        /// </summary>
        [AoMember(7)]
        public BuildingGeneratorData Generator { get; set; }

        /// <summary>
        /// What the playfield is filled with, when it is not a mission.
        /// </summary>
        [AoMember(8)]
        public PlayfieldTemplateGeneratorData TemplateGenerator { get; set; }

        #endregion
    }
}
