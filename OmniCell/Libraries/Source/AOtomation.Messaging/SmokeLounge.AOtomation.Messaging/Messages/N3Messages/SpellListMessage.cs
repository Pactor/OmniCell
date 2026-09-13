// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SpellListMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SpellListMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// What a nano does, and what it is called.
    /// </summary>
    /// <remarks>
    /// This was wrong for every one of the 1685 captured samples, and it was
    /// wrong for two reasons that hid each other.
    ///
    /// The first is that NanoEffect has a criteria array. CriterionCount is
    /// followed by that many requirement triples before Hits. 1067 of the
    /// captured effects carry no criteria at all, so the field looked like a
    /// count of nothing until a deliberate cast of Composite Attribute Boost
    /// produced one - stat 128, value 1639, operator 2.
    ///
    /// The second is that the six Gfx fields this class used to carry were not
    /// fields of the effect at all. They were the start of a trailer belonging
    /// to the message: two identities, a name, and an optional nano identity.
    /// Read as six ints, the name fell into them - GfxFade came out as
    /// 1986358900, which is "vent" in ASCII, the middle of "Event Loop Cedric
    /// Harding".
    ///
    /// What sits between the effect and that trailer is the game function's own
    /// arguments, and its length depends on which function it is. Those lengths
    /// are in the serializer.
    ///
    /// All 1685 captured SpellLists now deserialize and serialize back byte for
    /// byte, single and multiple effect alike.
    /// </remarks>
    [AoContract((int)N3MessageType.SpellList)]
    public class SpellListMessage : N3Message
    {
        #region Constructors and Destructors

        public SpellListMessage()
        {
            this.N3MessageType = N3MessageType.SpellList;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public NanoEffect[] NanoEffects { get; set; }

        /// <summary>
        /// Identity.None in most captures, the caster where it is not.
        /// </summary>
        /// <remarks>
        /// The client skips this when it is Identity.None, and otherwise looks
        /// it up as a dynel and casts it to Beholder_t - a cast that yields
        /// nothing unless the dynel is an item, since every class deriving from
        /// Beholder_t is one: AccessCard, NanoItem, WeaponItem, SimpleItem,
        /// TrapItem, QuestBooth and the rest.
        ///
        /// It is None in 1,286 of 2,551 captured copies and carries a
        /// CanbeAffected identity in the other 1,265, so what the server puts
        /// here is not always something that cast will accept. The handler is
        /// written to take nothing for an answer.
        /// </remarks>
        [AoMember(1)]
        public Identity Source { get; set; }

        /// <summary>
        /// The character the nano is running on.
        /// </summary>
        [AoMember(2)]
        public Identity Character { get; set; }

        /// <summary>
        /// Sets or clears bit 0 of the spell's flag word on the client.
        /// </summary>
        /// <remarks>
        /// Read as a byte and kept as a boolean. Gamecode.dll 0x10002823 turns
        /// bit 0 of the spell object's flags on when it is non-zero and off
        /// when it is zero; what that bit goes on to mean is not established
        /// here.
        ///
        /// This used to be read as the first half of a short, with the second
        /// half taken for a one-byte name length. That is the same three bytes
        /// on the wire, so nothing ever complained about it.
        /// </remarks>
        [AoMember(3)]
        public byte SetSpellFlag { get; set; }

        /// <summary>
        /// The nano's name - "Event Loop Cedric Harding", "Ambient Renewal".
        /// Empty on effects that have no name.
        /// </summary>
        /// <remarks>
        /// Preceded by a two byte length, not a one byte one. Gamecode.dll
        /// 0x10038AF8 is the short-counted string reader the client uses here.
        /// </remarks>
        [AoMember(4)]
        public string Name { get; set; }

        /// <summary>
        /// Whether <see cref="Nano"/> follows.
        /// </summary>
        [AoMember(5)]
        public byte HasNano { get; set; }

        /// <summary>
        /// The nano program itself, when there is one.
        /// </summary>
        [AoMember(6)]
        public Identity Nano { get; set; }

        /// <summary>
        /// A flag the client reads and never uses.
        /// </summary>
        /// <remarks>
        /// Not unknown - followed to the end. The reader at 0x100AAC70 takes a
        /// byte, normalises it with a setne and keeps it at the message's
        /// +0x4D. The dispatcher at 0x100AAEE2 loads it and passes it as the
        /// seventh of nine arguments to Beholder_t's vtable slot 12, which is
        /// 0x100026D0. That function uses it exactly once, at 0x1000294C, where
        /// it goes on the stack as the third argument of 0x100A5F4B - and
        /// 0x100A5F4B never touches its third argument.
        ///
        /// So the trail ends: nothing in this build reads the value. The server
        /// sends 0 in all 766 captured copies.
        /// </remarks>
        [AoMember(7)]
        public byte UnreadFlag { get; set; }

        /// <summary>
        /// Which of the effects the client is to apply.
        /// </summary>
        /// <remarks>
        /// Proven from the gate at Gamecode.dll 0x10002779, which lets an
        /// effect through only if its game function is in a permitted set:
        ///
        ///   0  every effect, with no check at all
        ///   1  only HeadMesh, BackMesh, Shouldermesh, Texture, ChangeBodyMesh
        ///      and AttractorMesh - 53035, 53037, 53038, 53039, 53054, 53055
        ///   2  the same six less Texture
        ///
        /// Every one of those is an appearance function, so a non-zero value
        /// means "change how this looks and nothing else", and 2 means leave
        /// the textures alone as well.
        ///
        /// This used to be read before UnreadFlag rather than after it, which
        /// reproduces the bytes and gets both values wrong.
        /// </remarks>
        [AoMember(8)]
        public int ApplyScope { get; set; }

        #endregion
    }
}
