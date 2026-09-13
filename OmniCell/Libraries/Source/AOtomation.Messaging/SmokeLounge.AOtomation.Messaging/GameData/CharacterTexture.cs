// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharacterTexture.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CharacterTexture type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One entry of the texture list SimpleCharFullUpdate carries behind
    /// HasExtendedTextures.
    /// </summary>
    /// <remarks>
    /// This is the TextureData_t of the client. Its reader is exported by name
    /// from GameData.dll as
    ///
    ///     ??5@YAAAVBinaryStream@@AAV0@AAVTextureData_t@@@Z
    ///
    /// at RVA 0xC0B4, and it reads forty four bytes: a fixed thirty two byte
    /// block taken raw off the stream, then three int32s. The client turns the
    /// last of the three into 5 or 0 - a flag, evidently - but the value on the
    /// wire is a whole int32 and is kept whole here, because anything narrower
    /// would not write back what was read.
    ///
    /// The thirty two byte block is read with BinaryStream::read rather than
    /// through any string reader, so it is a fixed size field and not a counted
    /// one. It is almost certainly a name; nothing here depends on that.
    ///
    /// The three int32s are named from the other end, in DisplaySystem.dll:
    /// VisualCATMesh_t::SetCATTextures walks a vector of these records, and its
    /// loop at 0x10070247 draws the first int32 at the layer its caller named,
    /// with the third as the alpha mode, and then draws the second at layer 3
    /// with no alpha. A cloth entry ends up here - Gamecode.dll 0x1004BBAE
    /// copies its three int32s into these three, in order - which is why the
    /// same three names fit both records.
    /// </remarks>
    public class CharacterTexture
    {
        #region AoMember Properties

        /// <summary>
        /// The thirty two bytes the client reads raw. Always exactly that long.
        /// </summary>
        [AoMember(0)]
        public byte[] Name { get; set; }

        /// <summary>
        /// The texture, drawn at whichever layer the caller asks for.
        /// </summary>
        [AoMember(1)]
        public int TextureId { get; set; }

        /// <summary>
        /// A second texture, drawn at layer 3 with no alpha.
        /// </summary>
        [AoMember(2)]
        public int OverlayId { get; set; }

        /// <summary>
        /// How <see cref="TextureId"/> is blended. The client keeps 5 when this
        /// is non zero and 0 when it is zero.
        /// </summary>
        [AoMember(3)]
        public int AlphaMode { get; set; }

        #endregion
    }
}
