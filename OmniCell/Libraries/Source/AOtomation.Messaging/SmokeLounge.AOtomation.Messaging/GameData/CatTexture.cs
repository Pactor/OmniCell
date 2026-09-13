namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One texture swapped onto a character's CAT mesh.
    /// </summary>
    /// <remarks>
    /// Eight bytes on the wire, two int32s, which is why this was read as an
    /// Identity until 2026-09-11 - the client reads it with the shared Identity
    /// reader at 0x1013D2F9 and the two are the same bytes. What it does with
    /// them is not an Identity at all: the SimpleCharFullUpdate dispatcher
    /// walks the list at 0x10078A8F and calls
    /// VisualCATMesh_t::SetCATTexture(int, unsigned int, TextureLayer,
    /// AlphaMode) for each entry, through 0x1004B515, with the layer fixed at 1
    /// and the alpha mode at 0.
    ///
    /// Which of the two is which comes from SetCATTexture itself - 0x100729D6
    /// in DisplaySystem.dll, funnelling into 0x100700E9. That walks the mesh's
    /// existing texture entries and matches on entry + 0xC against the first
    /// argument, then writes the second into entry + 0x14. So the first selects
    /// the texture being replaced and the second is the replacement.
    /// </remarks>
    public class CatTexture
    {
        #region AoMember Properties

        /// <summary>
        /// Which of the mesh's textures this replaces.
        /// </summary>
        /// <remarks>
        /// Matched against a CAT mesh texture entry's + 0xC at 0x1007013E. The
        /// argument is signed and the client has a second SetCATTexture
        /// overload that takes a name instead, so a negative value here sends
        /// it down the by-name path at 0x10070109 - with the name null, which
        /// is what the dispatcher always passes.
        /// </remarks>
        [AoMember(0)]
        public int CatId { get; set; }

        /// <summary>
        /// The texture to put there, written to the entry's + 0x14.
        /// </summary>
        [AoMember(1)]
        public int TextureId { get; set; }

        #endregion
    }
}
