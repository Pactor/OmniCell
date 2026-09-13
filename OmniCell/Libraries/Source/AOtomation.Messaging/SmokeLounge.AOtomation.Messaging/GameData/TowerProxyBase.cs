using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One tower, as the client draws it.
    /// </summary>
    /// <remarks>
    /// The client calls this TowerProxy_t - its vtable at 0x10171994 says so -
    /// and reads it at 0x1012BB14. Four of the five numbers on the end were
    /// Unknowns and the routine that builds the tower's visual, at 0x1012B7C7,
    /// names three of them by what it does with each: it hands the first to
    /// VisualCATMesh_t::SetMesh, picks a texture off the second, and hands the
    /// fourth to VisualCATMesh_t::SetScale. All three are DisplaySystem.dll
    /// exports, so those are Funcom's own words rather than ours.
    /// </remarks>
    public class TowerProxyBase
    {
        /// <summary>
        /// The tower field this tower belongs to.
        /// </summary>
        [AoMember(1)]
        public Identity TowerFieldIdentity { get; set; }

        /// <summary>
        /// Whose tower it is.
        /// </summary>
        [AoMember(2)]
        public Identity OwnerIdentity { get; set; }

        /// <summary>
        /// Where it stands.
        /// </summary>
        [AoMember(3)]
        public Vector3 Coordinates { get; set; }

        /// <summary>
        /// The mesh to draw it with.
        /// </summary>
        /// <remarks>
        /// 0x1012B7C7 builds an Identity out of the constant 0xF696B and this
        /// number and passes it to VisualCATMesh_t::SetMesh. Eight values
        /// across the 105 captured towers, which is eight tower models.
        /// </remarks>
        [AoMember(4)]
        public int MeshId { get; set; }

        /// <summary>
        /// Whose side the tower is on.
        /// </summary>
        /// <remarks>
        /// A three way switch at 0x1012B670 turns this into a texture name and
        /// the names are the evidence: 0 gives side_beam_neutral.png, 1 gives
        /// side_beam_clan.png and 2 gives side_beam_omni.png, which is exactly
        /// how <see cref="GameData.Side"/> already numbers them. The captured
        /// towers carry all three.
        /// </remarks>
        [AoMember(5)]
        public Side Side { get; set; }

        /// <summary>
        /// The animation to play on it.
        /// </summary>
        /// <remarks>
        /// Named the same way <see cref="MeshId"/> was, and by the same export.
        /// 0x1012B81D hands it to the cache at 0x1004E275 over the registry at
        /// 0x101BFCE4; on a miss the loader at 0x1004E174 builds a two word key
        /// on its stack - the constant 0xFDE97 and this value - and passes it
        /// to ResourceDatabase_t::GetBinaryStream(Identity const&amp;). So the
        /// registry is keyed on an Identity and this is its instance half,
        /// exactly as the mesh is the instance half of a 0xF696B. What comes
        /// back is parsed into an animation set, and the caller takes its +0x78
        /// and hands it to VisualCATMesh_t::SetAnimation - the DisplaySystem
        /// export next to the SetMesh and SetScale that named the other two.
        ///
        /// Across 1,470 tower records there are eight distinct values here and
        /// eight distinct meshes, in one-to-one correspondence.
        /// </remarks>
        [AoMember(6)]
        public int AnimationId { get; set; }

        /// <summary>
        /// How big to draw it.
        /// </summary>
        /// <remarks>
        /// Straight to VisualCATMesh_t::SetScale at 0x1012B870. Eleven values
        /// between 1.00 and 1.12 across the captured towers, which is what a
        /// mesh scale looks like.
        /// </remarks>
        [AoMember(7)]
        public Single Scale { get; set; }

        /// <summary>
        /// Three bits the map marker is chosen by.
        /// </summary>
        /// <remarks>
        /// Named for what it is rather than what it means, because the client
        /// shows the first and not the second. 0x1012A271 copies the tower's
        /// position, this, and the side into a twenty byte record and that
        /// record is MapTowerInfo_c - Interfaces.dll exports
        /// N3Msg_GetLandControlTowerList returning a vector of them, and
        /// GUI.dll's vector of markers has exactly that stride. So this and the
        /// side together are what the map draws a tower with.
        ///
        /// The only test the client makes on it is at 0x1012B759, which answers
        /// yes to 3, 5 and 6 and no to everything else - the three values with
        /// exactly two of the low three bits set. 0x1012A095 uses that to pick
        /// which towers count towards a piece of geometry over the field. Every
        /// captured tower carries 1, 2 or 4: exactly one bit each.
        /// </remarks>
        [AoMember(8)]
        public int MarkerFlags { get; set; }
    }
}
