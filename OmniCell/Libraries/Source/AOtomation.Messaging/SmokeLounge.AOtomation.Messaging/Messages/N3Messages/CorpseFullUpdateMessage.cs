// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CorpseFullUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CorpseFullUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// What is left where something died.
    /// </summary>
    /// <remarks>
    /// This was one line - MsgVersion, and a comment saying the packet was not
    /// done. It is done now, and checked: all 154 corpses in the captured
    /// sessions, across ten kinds of creature and ten different lengths from 416
    /// to 522 bytes, are read and written back byte for byte.
    ///
    /// Two things had kept it unread. The first is InventoryIdAndBodyLocation, a short sitting
    /// between the playfield id and the stat list - reading it as anything else
    /// puts every field after it two bytes out, which is why the body looked
    /// like nonsense past that point. The second is that the stat list is an
    /// X3F1 array whose first entry is stat 0, the flags, so the count is always
    /// one higher than the visible stats and looked wrong.
    ///
    /// The one conditional: Meshes is preceded by an int which is 0 on eight of
    /// the 154 - two humanoid corpses - and 1 on the rest. Where it is 0 the
    /// message simply ends, with no array header at all. Where it is 1 an X3F1
    /// array of one or two meshes follows. Read as a flag rather than a count,
    /// since 1 precedes both one mesh and two.
    /// </remarks>
    [AoContract((int)N3MessageType.CorpseFullUpdate)]
    public class CorpseFullUpdateMessage : N3Message
    {
        #region Constructors and Destructors

        public CorpseFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.CorpseFullUpdate;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// 8 in every capture.
        /// </summary>
        [AoMember(0)]
        public int MsgVersion { get; set; }

        /// <summary>
        /// 11 in every capture.
        /// </summary>
        /// <summary>
        /// 11 - the item version, not the corpse one.
        /// </summary>
        /// <remarks>
        /// Everything from here to ChestVersion is ChestItemFullUpdate, field
        /// for field. CorpseFullUpdateIIR_t::ReadSubClass at Gamecode.dll
        /// 0x1009FA4C reads the int32 above, refuses anything but 8, and then
        /// calls ChestFullUpdateIIR_t::ReadSubClass for the whole of it before
        /// reading a tail of its own.
        /// </remarks>
        [AoMember(1)]
        public int ItemVersion { get; set; }

        /// <summary>
        /// The type half of who is holding the corpse. Nobody, in every
        /// captured copy - a corpse lies on the ground.
        /// </summary>
        [AoMember(2)]
        public int HolderType { get; set; }

        /// <summary>
        /// The instance half, and the gate on the position below: zero means
        /// the position is on the wire. Zero in every captured copy.
        /// </summary>
        /// <remarks>
        /// Not to be confused with Owner further down, which is the character
        /// the corpse used to be.
        /// </remarks>
        [AoMember(3)]
        public int HolderInstance { get; set; }

        [AoMember(4)]
        public Vector3 Coordinates { get; set; }

        [AoMember(5)]
        public Quaternion Heading { get; set; }

        [AoMember(6)]
        public int PlayfieldId { get; set; }

        /// <summary>
        /// The type half of the state machine identity, and zero here.
        /// </summary>
        /// <remarks>
        /// The rest of the item family carries 1000015 in this field, the
        /// identity type of a state machine. A corpse carries a plain zero, so
        /// the 1000015 is a property of those items rather than of the format.
        /// </remarks>
        [AoMember(7)]
        public int StateMachineType { get; set; }

        /// <summary>
        /// The instance half. Zero in every captured copy.
        /// </summary>
        [AoMember(8)]
        public int StateMachineInstance { get; set; }

        /// <summary>
        /// 111 in every capture, and the field that hid the rest of this message.
        /// </summary>
        /// <summary>
        /// Stat 55 inventoryid in the high byte and stat 220 currbodylocation
        /// in the low one. 111 in every captured copy, which is inventory id 0
        /// and body location 111.
        /// </summary>
        /// <remarks>
        /// One int16 here and two separate bytes on its relatives; the wire is
        /// the same either way.
        /// </remarks>
        [AoMember(9)]
        public short InventoryIdAndBodyLocation { get; set; }

        /// <summary>
        /// Stat id and value, the first entry being stat 0, the flags.
        /// </summary>
        [AoMember(10)]
        public GameTuple<int, int>[] Stats { get; set; }

        /// <summary>
        /// "Remains of ..." - length prefixed, NUL terminated, the length
        /// counting the terminator.
        /// </summary>
        [AoMember(11)]
        public string Name { get; set; }

        /// <summary>
        /// 2 in every capture.
        /// </summary>
        /// <summary>
        /// 2 - LockableItemFullUpdate's version, checked against the static at
        /// 0x101C2084.
        /// </summary>
        [AoMember(12)]
        public int LockableVersion { get; set; }

        /// <summary>
        /// 50 in every capture.
        /// </summary>
        /// <summary>
        /// Stat 299, lockdifficulty. 50 in every captured copy.
        /// </summary>
        [AoMember(13)]
        public int LockDifficulty { get; set; }

        /// <summary>
        /// Empty in all 154. Its element shape is therefore unknown, which does
        /// not matter while it stays empty and will matter the day it does not.
        /// </summary>
        /// <summary>
        /// Who can lock and unlock the corpse. Empty in every captured copy.
        /// </summary>
        /// <remarks>
        /// The LockableItem_t keyholder list - see the same field on
        /// ChestItemFullUpdate. It was modelled as int32s and the client reads
        /// Identities; no captured copy carries one, so the two are the same
        /// four zero bytes of count and nothing after it.
        /// </remarks>
        [AoMember(14)]
        public Identity[] Keyholders { get; set; }

        /// <summary>
        /// 3 in every capture.
        /// </summary>
        /// <summary>
        /// 3 - ChestFullUpdate's own version, checked against the static at
        /// 0x101C1F80, and the last field before the corpse tail begins.
        /// </summary>
        [AoMember(15)]
        public int ChestVersion { get; set; }

        /// <summary>
        /// One entry in every capture, the first value being 0xcf27.
        /// </summary>
        /// <summary>
        /// What was cast on the corpse. One effect in every captured copy.
        /// </summary>
        /// <remarks>
        /// The same record SpellList carries, read by the same function -
        /// Gamecode.dll 0x100A71AE - and now by the same code here. It used to
        /// be eight numbered fields: a list of paired int32s that caught the
        /// effect identity, then the version, then seven more int32s, then five
        /// singles. That is one effect laid out flat, and it only held together
        /// because every captured corpse carries exactly one of them and none
        /// of them carries a criterion.
        ///
        /// Reading it properly needed one addition to the tail-length table:
        /// game function 53031 carries twenty eight bytes, which the captures
        /// give directly - the effect runs from the count to the owner identity
        /// after it, and everything between is accounted for.
        /// </remarks>
        [AoMember(16)]
        public NanoEffect[] NanoEffects { get; set; }

        [AoMember(24)]
        public Identity Owner { get; set; }

        /// <summary>
        /// Five entries in every capture, places 0 to 4, ids all zero.
        /// </summary>
        [AoMember(25)]
        public Texture[] Textures { get; set; }

        /// <summary>
        /// Whether <see cref="Meshes"/> follows at all. See the class remarks.
        /// </summary>
        [AoMember(26)]
        public int HasMeshes { get; set; }

        [AoMember(27)]
        public CorpseMesh[] Meshes { get; set; }

        #endregion
    }
}
