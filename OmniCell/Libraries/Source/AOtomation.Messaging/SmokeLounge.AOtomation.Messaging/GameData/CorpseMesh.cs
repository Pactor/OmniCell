// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CorpseMesh.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CorpseMesh type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// One mesh of a corpse, named rather than numbered.
    /// </summary>
    /// <remarks>
    /// Forty-four bytes: a thirty-two byte name, NUL padded, then three ints of
    /// which only the first is ever set. Captured names are the model part
    /// names - "mdrone1", "mdrone2" on a cleaning robot, "Material #1" on
    /// others - which is what makes this a mesh rather than a stat.
    ///
    /// It is GameData TextureData_t, the same record SimpleCharFullUpdate
    /// carries as CharacterTexture, and the three ints are named from the same
    /// place: VisualCATMesh_t::SetCATTextures in DisplaySystem.dll walks a
    /// vector of these and its loop at 0x10070247 draws the first at the layer
    /// its caller named with the third as the alpha mode, then the second at
    /// layer 3 with no alpha.
    /// </remarks>
    public class CorpseMesh
    {
        #region Constants

        /// <summary>
        /// The name field is a fixed width, not length prefixed.
        /// </summary>
        public const int NameLength = 32;

        #endregion

        #region Public Properties

        public string Name { get; set; }

        /// <summary>
        /// The texture, in the same range as the item and mesh ids elsewhere.
        /// Drawn at whichever layer the caller asks for.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// A second texture, drawn at layer 3 with no alpha. Zero in all 154
        /// captured corpses.
        /// </summary>
        public int OverlayId { get; set; }

        /// <summary>
        /// How <see cref="Id"/> is blended. Zero in all 154 captured corpses,
        /// and 5 is the only other value the client holds.
        /// </summary>
        public int AlphaMode { get; set; }

        #endregion
    }
}
