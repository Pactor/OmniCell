// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayfieldTemplateGeneratorData.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PlayfieldTemplateGeneratorData type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// What a template-generated playfield is filled with.
    /// </summary>
    /// <remarks>
    /// The other thing PlayfieldAnarchyF can end with. Where a mission carries
    /// an ACGBuildingGeneratorData_t, a playfield like Arete Landing carries a
    /// TemplatePlayfieldGeneratorData_t, which DbObject_t::CreateObject builds
    /// for identity type 51069. Its ReadBlob is Gamecode.dll 0x10124DC4 and it
    /// refuses a Version other than 1.
    ///
    /// The runs were already modelled, as PlayfieldDynelRun, and read correctly
    /// - but as a loose table hanging off the end of the message rather than as
    /// the body of an object, which is why the two leading fields here were
    /// being taken for PlayfieldX and PlayfieldZ.
    /// </remarks>
    public class PlayfieldTemplateGeneratorData
    {
        #region AoMember Properties

        /// <summary>
        /// The identity the message peeked before rewinding.
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
        /// 1, and the reader accepts nothing else.
        /// </summary>
        [AoMember(2)]
        public int Version { get; set; }

        /// <summary>
        /// Everything in the playfield that is not a character, in runs.
        /// </summary>
        /// <remarks>
        /// A plain int32 count and then that many twenty byte records. Five of
        /// them describe Arete Landing.
        /// </remarks>
        [AoMember(3)]
        public PlayfieldDynelRun[] Runs { get; set; }

        #endregion
    }
}
