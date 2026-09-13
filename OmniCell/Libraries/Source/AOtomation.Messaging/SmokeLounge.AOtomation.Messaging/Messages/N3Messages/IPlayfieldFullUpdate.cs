// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPlayfieldFullUpdate.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the IPlayfieldFullUpdate type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// The body n3PlayfieldFullUpdateIIR_t reads.
    /// </summary>
    /// <remarks>
    /// Two messages carry it. N3PlayfieldFullUpdate is the class itself, and
    /// PlayfieldAnarchyF is that class with two int32s on the end: its reader
    /// at Gamecode 0x101258E3 calls n3PlayfieldFullUpdateIIR_t::ReadSubClass,
    /// exported from N3.dll at 0x10029C24, and then reads those two. This is
    /// what the two share, so that one reader serves both rather than the
    /// twenty lines being written out twice.
    /// </remarks>
    public interface IPlayfieldFullUpdate
    {
        #region Public Properties

        int Version { get; set; }

        Vector3 CharacterCoordinates { get; set; }

        byte TokenMarker { get; set; }

        Identity ModelId { get; set; }

        int Group { get; set; }

        int Subgroup { get; set; }

        Identity PlayfieldId { get; set; }

        BuildingGeneratorData Generator { get; set; }

        PlayfieldTemplateGeneratorData TemplateGenerator { get; set; }

        #endregion
    }
}
