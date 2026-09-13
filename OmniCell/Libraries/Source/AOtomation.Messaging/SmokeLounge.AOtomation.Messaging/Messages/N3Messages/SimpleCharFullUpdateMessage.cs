// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleCharFullUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SimpleCharFullUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.SimpleCharFullUpdate)]
    public class SimpleCharFullUpdateMessage : N3Message
    {
        #region Constructors and Destructors

        public SimpleCharFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.SimpleCharFullUpdate;
            this.Unknown = 0x00;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public byte Version { get; set; }

        [AoMember(1)]
        public SimpleCharFullUpdateFlags Flags { get; set; }

        [AoMember(2)]
        public int? PlayfieldId { get; set; }

        /// <summary>
        /// The width bits exactly as they arrived, or null for a message the
        /// server built rather than read.
        /// </summary>
        /// <remarks>
        /// Level, Health, HealthDamage and RunSpeedBase each go on the wire in
        /// a narrow or a wide form, and a flag says which. The obvious rule -
        /// pick the narrow form whenever the value fits it - is what the sender
        /// does almost always, and it is wrong: fifteen captured updates send a
        /// level of twenty as a short, and ten of those also send a health of
        /// fifty-eight as a full int. The width is the sender's to choose and
        /// nothing in the value predicts it, so a message that came off the
        /// wire has to carry the choice back out.
        ///
        /// Deliberately not an AoMember: it is not a field, it is a record of
        /// how four other fields were framed. Null means derive, which is what
        /// every message the server originates wants.
        /// </remarks>
        public SimpleCharFullUpdateFlags? WireWidths { get; set; }


        /// <summary>
        /// The dynel this character is attached to, read straight after
        /// PlayfieldId behind HasParentDynel. Null puts it in the playfield
        /// itself.
        /// </summary>
        /// <remarks>
        /// Named on 2026-09-11 from what the dispatcher at 0x1007835C does with
        /// it, which is the whole of its use. The reader puts it at the
        /// message's + 0x24, and the dispatcher looks it up with
        /// n3Dynel_t::GetDynel. Then it branches on the Identity being zero:
        ///
        /// * zero, and the new dynel goes to
        ///   n3Playfield_t::AddChildDynel(dynel, Coordinates, Heading) at
        ///   0x1007840E - the playfield found from PlayfieldId;
        /// * non-zero, and the same three arguments go to virtual slot 9 of the
        ///   dynel it just found, at 0x10078424. That slot is
        ///   n3Dynel_t::AddChildDynel(n3Dynel_t*, const Vector3&amp;, const
        ///   Quaternion&amp;) - 0x100059F7 in N3.dll, ninth in all three of the
        ///   dynel vtables that carry it.
        ///
        /// So this is the parent the character is added under, and Coordinates
        /// and Heading are relative to whichever of the two took it. Nothing in
        /// the dispatcher compares it to a combat target, and the field it
        /// lands in is not the one IsUnderAttack fills.
        ///
        /// It was called Target behind HasFightingTarget until then, because
        /// two Identities gated by two flags with combat-sounding names sat
        /// near each other. FightingTarget keeps its own slot and name: the
        /// zone server fills it, and this finding says nothing about it.
        /// </remarks>
        [AoMember(3)]
        public Identity? ParentDynel { get; set; }

        [AoMember(4)]
        public Vector3 Coordinates { get; set; }

        [AoMember(5)]
        public Quaternion Heading { get; set; }

        [AoMember(6)]
        public Appearance Appearance { get; set; }

        [AoMember(7)]
        public string Name { get; set; }

        [AoMember(8)]
        public CharacterFlags CharacterFlags { get; set; }

        [AoMember(9)]
        public short AccountFlags { get; set; }

        [AoMember(10)]
        public short Expansions { get; set; }

        [AoMember(11)]
        public SimpleCharacterInfo CharacterInfo { get; set; }

        [AoMember(12)]
        public short Level { get; set; }

        [AoMember(13)]
        public int Health { get; set; }

        [AoMember(14)]
        public int HealthDamage { get; set; }

        [AoMember(15)]
        public uint MonsterData { get; set; }

        [AoMember(16)]
        public short MonsterScale { get; set; }

        [AoMember(17)]
        public short VisualFlags { get; set; }

        [AoMember(18)]
        public byte VisibleTitle { get; set; }

        /// <summary>
        /// The state of the vehicle that moves this character, length-prefixed
        /// so a reader that does not know its shape can step over it.
        /// </summary>
        /// <remarks>
        /// The client does exactly that. 0x10067B62 reads the int32 length,
        /// copies that many bytes straight out of the message stream into a
        /// BinaryStream it keeps at the message's + 0xC4, and seeks past them -
        /// it never parses them in place. The ribosome's fill method then hands
        /// that stream to the new character's vehicle at 0x10078323:
        /// n3Dynel_t + 0x50 is the Vehicle_t pointer - SetVehicle at N3.dll
        /// 0x10001107 and GetVehicle at 0x1000110E are the two lines that touch
        /// it - and the call is virtual slot 43 of whatever vehicle that is.
        ///
        /// Slot 43 is a reader in all of them, and the shape is:
        ///
        /// * Vector3, three floats, read by the shared 0x1000404E at
        ///   0x1006F5A2 in the base CharVehicle_t reader;
        /// * ten bytes, each widened to a dword into the movement state at
        ///   vehicle + 0x178, read one at a time by 0x10070981;
        /// * an int32, into that same object's + 0x30, at 0x1006CA3D;
        /// * then, for a PlayerVehicle_t, four more floats into + 0x360 to
        ///   + 0x36C at 0x100718B7 onward - or, for an NPCVehicle_t, an int16
        ///   count at 0x10071009 and that many floats.
        ///
        /// Twenty six bytes then, plus sixteen for a player and two for an NPC
        /// whose count is zero. Which is 42 and 28 - the two lengths the
        /// original CellAO zone server hardcoded, one commented "for
        /// PlayerCharacters that is" and the other "NPC's have a shorter one?".
        /// Its thirteenth byte, the one it fills with currentMovementMode, is
        /// the first of the ten, and that is where a movement mode belongs.
        ///
        /// Kept as bytes because the shape is the vehicle's, not the message's,
        /// and nothing in the message says which vehicle the character will get.
        /// </remarks>
        [AoMember(19, SerializeSize = ArraySizeType.Int32)]
        public byte[] VehicleData { get; set; }

        [AoMember(20)]
        public uint? HeadMesh { get; set; }

        [AoMember(21)]
        public short RunSpeedBase { get; set; }

        /// <summary>
        /// An Identity behind IsUnderAttack. Read into the message's + 0xBC
        /// and not touched again by the dispatcher, so unlike
        /// <see cref="ParentDynel"/> its name is still inherited rather than
        /// proven.
        /// </summary>
        [AoMember(22)]
        public Identity? FightingTarget { get; set; }

        /// <summary>
        /// The list behind HasExtendedTextures.
        /// </summary>
        /// <remarks>
        /// An X3F1 counted array of forty four byte entries. This is the field
        /// the flag has always been named for and it was never read: the
        /// standing guess had been that the flag gated four bytes, which was
        /// wrong in kind rather than in detail.
        /// </remarks>
        [AoMember(23, SerializeSize = ArraySizeType.X3F1)]
        public CharacterTexture[] ExtendedTextures { get; set; }

        /// <summary>
        /// Monster scale, as a percentage. Behind IsImmune, which is the flag
        /// this was named for and is not what it carries.
        /// </summary>
        /// <remarks>
        /// The reader puts it at the message's + 0x120 and the dispatcher
        /// takes it from there at 0x10078A19, sign-extends it, and files it
        /// under key 0x168 - 360, monsterscale - in the map at + 8 of the
        /// skill subsystem, which is [character + 0x1BC].
        ///
        /// The three captured values are 110, 90 and 100, which is what a
        /// scale reads as when it is a percentage of normal.
        ///
        /// That map is the same one FullCharacter's skill map is parked in
        /// wholesale by 0x100646B2, keyed the same way, which is a second
        /// reason to think that list's keys are stat ids - the question left
        /// open on FullCharacter's own page.
        ///
        /// The flag name is left alone. IsImmune has been wrong since
        /// SmokeLounge and renaming a flag because the field behind it turned
        /// out to be something else would be trading one guess for another;
        /// what the bit means is still unknown.
        /// </remarks>
        [AoMember(24)]
        public byte? ImmuneData { get; set; }

        /// <summary>
        /// The same stat again, into the other map. Behind UnknownFlag3.
        /// </summary>
        /// <remarks>
        /// + 0x121, taken by the dispatcher at 0x10078A40 and filed under the
        /// same key 0x168 as <see cref="ImmuneData"/> - but through
        /// 0x10064E5C rather than 0x10064EA9, and those two differ in exactly
        /// one thing: the first writes the map at subsystem + 4 and the second
        /// the map at + 8. Two maps, one key, two values, which is the shape
        /// of a base and a modified value; the client exports
        /// N3Msg_GetSkill and N3Msg_SetSkillTmp against this subsystem and
        /// that is the same pairing.
        ///
        /// Which of the two maps is the base one is not settled here.
        /// </remarks>
        [AoMember(25)]
        public byte? UnknownData3 { get; set; }

        [AoMember(26, SerializeSize = ArraySizeType.X3F1)]
        public ActiveNano[] ActiveNanos { get; set; }

        /// <summary>
        /// The path behind HasWaypoints.
        /// </summary>
        [AoMember(27)]
        public WaypointPath Waypoints { get; set; }

        [AoMember(28, SerializeSize = ArraySizeType.X3F1)]
        public Texture[] Textures { get; set; }

        [AoMember(29, SerializeSize = ArraySizeType.X3F1)]
        public Mesh[] Meshes { get; set; }

        /// <summary>
        /// The list behind HasNoWeaponPairs.
        /// </summary>
        [AoMember(30, SerializeSize = ArraySizeType.X3F1)]
        public WeaponPair[] NoWeaponPairs { get; set; }

        /// <summary>
        /// A byte behind UnknownFlag4.
        /// </summary>
        [AoMember(31)]
        public byte? UnknownData4 { get; set; }

        /// <summary>
        /// The textures behind HasCatTextures, swapped onto the character's CAT
        /// mesh one entry at a time.
        /// </summary>
        /// <remarks>
        /// Read as an Identity list until 2026-09-11 - the entries are two
        /// int32s and the client reads them with the shared Identity reader, so
        /// nothing on the wire said otherwise. The dispatcher does: it walks the
        /// list at 0x10078A8F and hands each entry to
        /// VisualCATMesh_t::SetCATTexture through 0x1004B515. See
        /// <see cref="CatTexture"/> for which of the two ints is which.
        ///
        /// Empty in every captured session, so this is a claim about the client
        /// and not about anything that has been seen on the wire.
        /// </remarks>
        [AoMember(32, SerializeSize = ArraySizeType.X3F1)]
        public CatTexture[] CatTextures { get; set; }

        /// <summary>
        /// A second flag word, near the end, gating three more fields.
        /// </summary>
        /// <remarks>
        /// Bit 1 gates StatUpdate, bit 2 gates BattlestationSide and bit 4
        /// gates PetMaster. The client tests them against a local rather than
        /// against anything it stored, so this word exists only to describe the
        /// tail of the message.
        /// </remarks>
        [AoMember(33)]
        public int Flags2 { get; set; }

        /// <summary>
        /// The stat block behind bit 1 of Flags2.
        /// </summary>
        [AoMember(34)]
        public CharacterStatUpdate StatUpdate { get; set; }

        /// <summary>
        /// Which side of a battlestation the character is on - stat 668.
        /// </summary>
        /// <remarks>
        /// The reader puts it at the message's + 0x314 and the dispatcher
        /// sign-extends it at 0x10078B38 and pushes it with 0x29C into the
        /// stat table at [character + 0xE8]. 0x29C is 668, battlestationside.
        ///
        /// The client defaults the member to 2 when it builds one of these
        /// itself, at 0x10078D8F and 0x1007906A, so 2 is the value that means
        /// neither side.
        /// </remarks>
        [AoMember(35)]
        public byte? BattlestationSide { get; set; }

        /// <summary>
        /// The character's pet master - stat 196.
        /// </summary>
        /// <remarks>
        /// The message's + 0x318, pushed with 0xC4 into the same stat table at
        /// 0x10078B70. 0xC4 is 196, petmaster.
        ///
        /// The dispatcher tests it first and applies it only when it is not
        /// zero, so a zero here leaves whatever the character already had
        /// rather than clearing it.
        /// </remarks>
        [AoMember(36)]
        public int? PetMaster { get; set; }

        [AoMember(37)]
        public byte Unknown2 { get; set; }

        #endregion
    }
}