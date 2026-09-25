// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OwnedBuildingGeneratorData.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the OwnedBuildingGeneratorData type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// A building somebody owns - which is to say, an apartment.
    /// </summary>
    /// <remarks>
    /// The third thing PlayfieldAnarchyF can end with. An ordinary playfield
    /// sends an empty identity, an instanced one like Arete Landing a
    /// TemplatePlayfieldGeneratorData_t at 51069, a mission an
    /// ACGBuildingGeneratorData_t at 51103 - and a private apartment this, at
    /// identity type 51067. Until 2026-09-25 the reader knew the first three and
    /// refused the fourth outright, so entering an apartment was a packet this
    /// project could not read at all.
    ///
    /// 51067 is registered in the same table as 51069, at Gamecode 0x101521F0,
    /// which pushes the type beside the factory at 0x1000D31A; that allocates
    /// 0x5C bytes and constructs through 0x101218EF, whose vtable is 0x10170BA8.
    /// The client names three generator classes and only three -
    /// AVTemplatePlayfieldGeneratorData_t, AVACGBuildingGeneratorData_t and
    /// AVOwnedBuildingGeneratorData_t - and an apartment is a building somebody
    /// owns, so that is where the name here comes from. The name is not tied to
    /// the number by anything stronger than that.
    ///
    /// Every field's shape below is the client's own, read off WriteBlob at
    /// vtable slot 7, Gamecode 0x10121709, which writes in this order: the
    /// literal 4, the int32 at +0x28, the Identity at +0x2C, the int32 at +0x34,
    /// the Vector3 at +0x38 through the three-float helper at 0x1000401B, then
    /// the helper at 0x1000AA2D on +0x44, then +0x4C, +0x58 and +0x48, and
    /// finally the list. Only the door among them has a meaning yet; the rest
    /// are named Unknown for it.
    ///
    /// Two captured copies, both Sunrise Station luxury apartments and both
    /// 246 bytes: 20260925-134417 stream 11 and 20260925-141548 stream 19, taken
    /// by different characters through different doors. Both read and write back
    /// byte for byte. Everything except the instances, the door and
    /// <see cref="Unknown3"/> is identical between them, which is what lets the
    /// fixed parts be called fixed.
    /// </remarks>
    public class OwnedBuildingGeneratorData
    {
        #region AoMember Properties

        /// <summary>
        /// The identity the message peeked before rewinding.
        /// </summary>
        [AoMember(0)]
        public Identity Identity { get; set; }

        /// <summary>
        /// The DbObject row revision, the third of its three columns.
        /// </summary>
        [AoMember(1)]
        public int Revision { get; set; }

        /// <summary>
        /// 4 in both captured copies, and the writer pushes that as a literal.
        /// </summary>
        [AoMember(2)]
        public int Version { get; set; }

        /// <summary>
        /// Not identified. 6001 in both captured copies, one less than the
        /// playfield <see cref="Model"/> names.
        /// </summary>
        [AoMember(3)]
        public int Unknown1 { get; set; }

        /// <summary>
        /// The playfield the apartment's front door stands in. Playfield1:6002
        /// in both captured copies, which is Sunrise Station.
        /// </summary>
        [AoMember(4)]
        public Identity Model { get; set; }

        /// <summary>
        /// The instance of the door that opens this apartment.
        /// </summary>
        /// <remarks>
        /// Settled by the second capture. At 20260925-141548 stream 18 the
        /// player used his key on the door with a GenericCmd UseItemOnItem whose
        /// second target is identity 51016:0xC0021772 - and 0xC0021772 is
        /// exactly what this field holds in the apartment he then arrived in.
        /// The first apartment, entered through a different door, carries
        /// 0xC0001772.
        ///
        /// The low half of both is 6002, the Sunrise Station playfield that
        /// <see cref="Model"/> also names, so this is a static object's identity
        /// in the usual shape: the playfield in the low sixteen bits and the
        /// object's own number above it. The two doors are 0 and 2.
        /// </remarks>
        [AoMember(5)]
        public int EntranceDoor { get; set; }

        /// <summary>
        /// A position. Zero in both captured copies.
        /// </summary>
        /// <remarks>
        /// Three floats, written through GameData's own three-float helper at
        /// 0x1000401B, which is what says it is a vector rather than three
        /// loose fields.
        /// </remarks>
        [AoMember(6)]
        public Vector3 Position { get; set; }

        /// <summary>
        /// 100000, and the client writes it from a global.
        /// </summary>
        /// <remarks>
        /// The helper at 0x1000AA2D pushes the dword at 0x101BE3A8 before
        /// anything else, and that global holds 100000. The same value turns up
        /// as a sentinel in the quest action record's world position, where 173
        /// of 193 records carry 100000 twice over. Kept as a field rather than
        /// written as a constant so a copy that disagrees still round trips.
        /// </remarks>
        [AoMember(7)]
        public int Marker { get; set; }

        /// <summary>
        /// Not identified, and the only fixed-position field that differs
        /// between the two captured copies: 0x0C6377B0 in one and 0x6027A4C0 in
        /// the other.
        /// </summary>
        [AoMember(8)]
        public int Unknown3 { get; set; }

        /// <summary>
        /// Not identified. Zero in both captured copies.
        /// </summary>
        [AoMember(9)]
        public int Unknown4 { get; set; }

        /// <summary>
        /// Not identified. 30 in both captured copies.
        /// </summary>
        [AoMember(10)]
        public int Unknown5 { get; set; }

        /// <summary>
        /// Everything in the apartment that is not a character, by type.
        /// </summary>
        /// <remarks>
        /// Five in both captured copies, the same five types in the same
        /// order with the same start indexes and counts - only the instances
        /// differ. They account for the terminals the player then used: in the
        /// first, a run of three from instance 250173593 covers the market and
        /// the bank he opened, and 250173596 is the grid terminal.
        /// </remarks>
        [AoMember(11)]
        public OwnedBuildingDynelRun[] Runs { get; set; }

        #endregion
    }
}
