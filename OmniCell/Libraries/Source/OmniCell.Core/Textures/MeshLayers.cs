#region License

// Copyright (c) 2005-2014, CellAO Team
//
//
// All rights reserved.
//
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//
//
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//

#endregion

namespace OmniCell.Core.Textures
{
    #region Usings ...

    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Enums;

    #endregion

    /// <summary>
    /// The meshes a character is drawn with, in the form AppearanceUpdate and
    /// SimpleCharFullUpdate put them on the wire.
    /// </summary>
    /// <remarks>
    /// This used to be keyed on position and layer, one mesh for each pair, and
    /// sent one mesh per position. The live server sends more than that. A
    /// character in a helmet goes out with two meshes at position 0 - the helmet
    /// at layer 2 and the head at layer 4 - and a character with a social hood
    /// and a social hat carries both at position 0, layer 2, as well as the head.
    /// Keying on the pair made the helmet overwrite the head, and the one mesh
    /// per position dropped whichever was left. Every retail AppearanceUpdate in
    /// the captures lists its meshes by position and then by layer.
    ///
    /// What the retail captures establish, from the converted item functions of
    /// the items the player was wearing at the time:
    ///   HeadMesh [0, mesh]    position 0, layer 2    (three items, three breeds)
    ///   AttractorMesh [mesh]  position 0, layer 0    (seven characters)
    ///   BackMesh [mesh]       position 5, layer 0
    ///   Shouldermesh [mesh]   position 3 from armor slot 20, 4 from slot 22
    ///   the head itself       position 0, layer 4, always present
    /// </remarks>
    public class MeshLayers
    {
        #region Constants

        /// <summary>
        /// The character's own head, at position 0.
        /// </summary>
        public const int BaseHeadLayer = 4;

        /// <summary>
        /// The first slot of the social page. Slots from here on are social.
        /// </summary>
        public const int FirstSocialSlot = 49;

        /// <summary>
        /// Shoulder pad from armor slot 20.
        /// </summary>
        public const int RightPadPosition = 3;

        /// <summary>
        /// Shoulder pad from armor slot 22.
        /// </summary>
        public const int LeftPadPosition = 4;

        #endregion

        #region Fields

        /// <summary>
        /// </summary>
        private readonly List<AOMeshs> entries = new List<AOMeshs>();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The meshes to send for a character, after its visual flags.
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <param name="showsocial">
        /// </param>
        /// <param name="socialonly">
        /// </param>
        /// <returns>
        /// </returns>
        public static List<AOMeshs> GetMeshs(Character character, bool showsocial, bool socialonly)
        {
            List<AOMeshs> meshs;
            List<AOMeshs> socials;
            int visualFlags;
            int baseHead;

            lock (character)
            {
                meshs = character.MeshLayer.GetMeshs();
                socials = character.SocialMeshLayer.GetMeshs();
                visualFlags = character.Stats[StatIds.visualflags].Value;
                baseHead = (int)character.Stats[StatIds.headmesh].BaseValue;
            }

            return Compose(meshs, socials, visualFlags, baseHead, showsocial, socialonly);
        }

        /// <summary>
        /// Combines the normal and social meshes into what goes on the wire.
        /// </summary>
        /// <remarks>
        /// With social items shown, a position that any social item draws at is
        /// drawn from the social items alone, and every other position keeps its
        /// armor. That is the one retail capture of a character wearing both:
        /// the social hood and hat replaced the armor helmet at position 0 while
        /// the armor back and both armor shoulder pads stayed. The head stays in
        /// either case.
        ///
        /// Of the pad flags, only 0x8 is established: with flags 63 the left pad
        /// was repeated at the right position and nothing was repeated the other
        /// way, although 0x10 was set too. What 0x10 does is not known, so it
        /// does nothing here. Hiding the helmet (0x4 clear) and the pads (0x1,
        /// 0x2 clear) is carried over from CellAO; no capture has any of those
        /// bits clear.
        /// </remarks>
        public static List<AOMeshs> Compose(
            List<AOMeshs> meshs,
            List<AOMeshs> socials,
            int visualFlags,
            int baseHead,
            bool showsocial,
            bool socialonly)
        {
            bool rightPadVisible = (visualFlags & 0x1) > 0;
            bool leftPadVisible = (visualFlags & 0x2) > 0;
            bool showHelmet = (visualFlags & 0x4) > 0;
            bool doubleLeftPad = (visualFlags & 0x8) > 0;

            socialonly &= showsocial; // Disable socialonly flag if showsocial is false

            IEnumerable<AOMeshs> chosen;
            if (!showsocial)
            {
                chosen = meshs;
            }
            else if (socialonly)
            {
                chosen = socials;
            }
            else
            {
                List<AOMeshs> socialItems = socials.Where(x => !IsBaseHead(x, baseHead)).ToList();
                var covered = new HashSet<int>(socialItems.Select(x => x.Position));
                chosen = meshs.Where(x => IsBaseHead(x, baseHead) || !covered.Contains(x.Position))
                    .Concat(socialItems);
            }

            List<AOMeshs> output =
                chosen.Where(
                    x =>
                        !((x.Position == 0) && !showHelmet && !IsBaseHead(x, baseHead))
                        && !((x.Position == RightPadPosition) && !rightPadVisible)
                        && !((x.Position == LeftPadPosition) && !leftPadVisible)).ToList();

            if (doubleLeftPad)
            {
                // The repeat goes out ahead of the pad already at that position.
                output.InsertRange(
                    0,
                    output.Where(x => x.Position == LeftPadPosition)
                        .Select(
                            x =>
                                new AOMeshs
                                {
                                    Position = RightPadPosition,
                                    Layer = x.Layer,
                                    Mesh = x.Mesh,
                                    OverrideTexture = x.OverrideTexture
                                })
                        .ToList());
            }

            // OrderBy is stable, so meshes sharing a position and layer keep the
            // order they were added in.
            return output.OrderBy(x => x.Position).ThenBy(x => x.Layer).ToList();
        }

        /// <summary>
        /// Adds a mesh, alongside any already at that position and layer.
        /// </summary>
        /// <param name="position">
        /// </param>
        /// <param name="meshToAdd">
        /// </param>
        /// <param name="overridetexture">
        /// </param>
        /// <param name="layer">
        /// </param>
        public void AddMesh(int position, int meshToAdd, int overridetexture, int layer)
        {
            this.entries.Add(
                new AOMeshs { Position = position, Mesh = meshToAdd, OverrideTexture = overridetexture, Layer = layer });
        }

        /// <summary>
        /// Replaces whatever is at a position and layer with this one mesh. For
        /// the character's own head, which there is only ever one of.
        /// </summary>
        /// <param name="position">
        /// </param>
        /// <param name="mesh">
        /// </param>
        /// <param name="overridetexture">
        /// </param>
        /// <param name="layer">
        /// </param>
        public void SetMesh(int position, int mesh, int overridetexture, int layer)
        {
            this.entries.RemoveAll(x => (x.Position == position) && (x.Layer == layer));
            this.AddMesh(position, mesh, overridetexture, layer);
        }

        /// <summary>
        /// </summary>
        public void Clear()
        {
            this.entries.Clear();
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        public int Count()
        {
            return this.entries.Count;
        }

        /// <summary>
        /// A copy of every mesh, in the order they were added.
        /// </summary>
        /// <returns>
        /// </returns>
        public List<AOMeshs> GetMeshs()
        {
            return
                this.entries.Select(
                    x => new AOMeshs { Position = x.Position, Mesh = x.Mesh, OverrideTexture = x.OverrideTexture, Layer = x.Layer })
                    .ToList();
        }

        /// <summary>
        /// </summary>
        /// <param name="position">
        /// </param>
        /// <param name="meshToRemove">
        /// </param>
        /// <param name="overridetexture">
        /// </param>
        /// <param name="layer">
        /// </param>
        public void RemoveMesh(int position, int meshToRemove, int overridetexture, int layer)
        {
            this.entries.RemoveAll(
                x =>
                    (x.Position == position) && (x.Mesh == meshToRemove) && (x.OverrideTexture == overridetexture)
                    && (x.Layer == layer));
        }

        #endregion

        #region Methods

        private static bool IsBaseHead(AOMeshs mesh, int baseHead)
        {
            return (mesh.Position == 0) && (mesh.Layer == BaseHeadLayer) && (mesh.Mesh == baseHead);
        }

        #endregion
    }
}
