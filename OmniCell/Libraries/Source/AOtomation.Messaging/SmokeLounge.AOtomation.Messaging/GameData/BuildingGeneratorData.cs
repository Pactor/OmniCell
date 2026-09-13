// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BuildingGeneratorData.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the BuildingGeneratorData type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// The recipe for a generated building - which is to say, a mission.
    /// </summary>
    /// <remarks>
    /// PlayfieldAnarchyF ends with a DbObject that reads itself, and for an
    /// instanced playfield the class DbObject_t::CreateObject builds for
    /// identity type 51103 is ACGBuildingGeneratorData_t. Its ReadBlob is
    /// Gamecode.dll 0x100C773E and its WriteBlob at 0x100C6E40 writes the same
    /// fields in the same order.
    ///
    /// Every mission is one of these, which is why a capture of someone walking
    /// around a static playfield never contained one: an ordinary playfield
    /// sends an empty identity where this begins and the message stops there.
    ///
    /// The names are the client's own. Its record dump at 0x100C746B prints the
    /// whole thing field by field - "width", "height", "template pf",
    /// "ambient colour" - and the offsets it prints from are the offsets
    /// ReadBlob writes to.
    ///
    /// The reader refuses a Version other than 3, a zero Width or Height, a
    /// WorldHeight above 1000, a TemplatePlayfield of zero, and a room count
    /// outside 1 to 10,000. The captured mission is thirty by thirty with
    /// thirty two rooms.
    /// </remarks>
    public class BuildingGeneratorData
    {
        #region AoMember Properties

        /// <summary>
        /// The identity the message peeked before rewinding, read again here by
        /// the object itself.
        /// </summary>
        [AoMember(0)]
        public Identity Identity { get; set; }

        /// <summary>
        /// The row revision, the third of the three columns every DbObject has.
        /// </summary>
        /// <remarks>
        /// DbObject_t::ReadBlob at DatabaseController.dll 0x100012B2 reads an
        /// Identity and then this int32, and DbObject_t::GetDbInfo at
        /// 0x100013BE names all three columns outright: TYPE, INSTANCE and
        /// REVISION. The first two are the Identity above.
        /// </remarks>
        [AoMember(1)]
        public int Revision { get; set; }

        /// <summary>
        /// 3, and the reader accepts nothing else.
        /// </summary>
        /// <remarks>
        /// A mismatch prints "Invalid version %u in stream, current is %d" and
        /// abandons the object.
        /// </remarks>
        [AoMember(2)]
        public short Version { get; set; }

        /// <summary>
        /// How many cells wide the grid is. 30 in the captured mission, and the
        /// reader refuses zero.
        /// </summary>
        /// <remarks>
        /// Cells, not world units: the generator at 0x100C9BDA multiplies it by
        /// ten before handing it to the geometry.
        /// </remarks>
        [AoMember(3)]
        public short Width { get; set; }

        /// <summary>
        /// How many cells deep the grid is. 30 in the captured mission, and the
        /// reader refuses zero.
        /// </summary>
        /// <remarks>
        /// Also multiplied by ten, at 0x100C9BD4.
        /// </remarks>
        [AoMember(4)]
        public short Height { get; set; }

        /// <summary>
        /// How tall the building is, in world units. 64 in the captured
        /// mission, and the reader refuses anything above 1000.
        /// </summary>
        /// <remarks>
        /// This one is world units already: the generator hands it to the
        /// geometry at 0x100C9BD7 alongside Width and Height, which are scaled
        /// on the way and this is not. It is the only one of the three shorts
        /// the client's own record dump does not print, so the name is read off
        /// that difference rather than off a string.
        /// </remarks>
        [AoMember(5)]
        public short WorldHeight { get; set; }

        /// <summary>
        /// The playfield the mission is generated from. 341 in the captured
        /// mission, and the reader refuses zero.
        /// </summary>
        /// <remarks>
        /// The client's record dump calls it "template pf".
        /// </remarks>
        [AoMember(6)]
        public int TemplatePlayfield { get; set; }

        /// <summary>
        /// The red channel of the building's ambient light. 30 in the captured
        /// mission, which is a dark interior.
        /// </summary>
        /// <remarks>
        /// These three are "ambient colour = (%u,%u,%u)" in the record dump at
        /// 0x100C7403, read from the same three bytes ReadBlob fills.
        /// </remarks>
        [AoMember(7)]
        public byte AmbientRed { get; set; }

        /// <summary>
        /// The green channel of the building's ambient light.
        /// </summary>
        [AoMember(8)]
        public byte AmbientGreen { get; set; }

        /// <summary>
        /// The blue channel of the building's ambient light.
        /// </summary>
        [AoMember(9)]
        public byte AmbientBlue { get; set; }

        /// <summary>
        /// The rooms the building is assembled from.
        /// </summary>
        /// <remarks>
        /// A plain int32 count and then that many six byte records. The count
        /// is checked against 1 to 10,000 and thirty two of them build the
        /// captured mission.
        /// </remarks>
        [AoMember(10)]
        public BuildingRoomInfo[] Rooms { get; set; }

        #endregion
    }
}
