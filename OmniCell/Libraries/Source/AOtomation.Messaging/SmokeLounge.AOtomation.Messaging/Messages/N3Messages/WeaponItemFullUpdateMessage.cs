// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WeaponItemFullUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the WeaponItemFullUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// A weapon. Byte for byte, a SimpleItemFullUpdate.
    /// </summary>
    /// <remarks>
    /// WeaponItemFullUpdateIIR_t::ReadSubClass at Gamecode.dll 0x100A2CAB is
    /// three instructions long: it calls SimpleItemFullUpdateIIR_t::ReadSubClass
    /// at 0x100A1661 and returns what it says. There is no tail, no version of
    /// its own and no extra field - the two messages are the same format under
    /// two ids. Its WriteSubClass is a bare return, so this side never sends it.
    ///
    /// It was not modelled that way. The stat list was written out as a fixed
    /// run of named fields, which froze one particular weapon's stats into the
    /// format: AcgItemLevel, AcgItemTemplateId, MultipleCount and StaticInstance
    /// were not values at all but the stat ids 701, 702, 412 and 23, and
    /// Unknown6 was the X3F1 count in front of them. That is why the doc note on
    /// the old ExtraStats field could see the two populations lining up exactly
    /// with Unknown6, 10090 against 8072 - those are the counts for nine stats
    /// and for seven.
    ///
    /// It round-tripped anyway, because the leftover fields soaked up whatever
    /// was left and the bytes came back in the same order. It would not have
    /// survived a weapon whose stats came in a different order, nor one lying on
    /// the ground: the old model had no position block at all, and every one of
    /// the 4,386 captured copies is a weapon somebody is holding.
    /// </remarks>
    [AoContract((int)N3MessageType.WeaponItemFullUpdate)]
    public class WeaponItemFullUpdateMessage : N3Message
    {
        #region Fields

        private int identityType;

        private int instance;

        #endregion

        #region Constructors and Destructors

        public WeaponItemFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.WeaponItemFullUpdate;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Who is holding the weapon, or nothing when it is on the ground.
        /// </summary>
        public Identity Owner { get; set; }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// 11, and the reader accepts nothing else.
        /// </summary>
        /// <remarks>
        /// Checked against the static at Gamecode.dll 0x101C2160 by the shared
        /// SimpleItemFullUpdate reader.
        /// </remarks>
        [AoMember(1)]
        public int MsgVersion { get; set; }

        [AoMember(2)]
        public int Identitytype
        {
            get
            {
                return this.identityType;
            }

            set
            {
                this.identityType = value;
                this.Owner = new Identity { Type = (IdentityType)value, Instance = this.instance };
            }
        }

        /// <summary>
        /// Zero is what makes the position fields present.
        /// </summary>
        /// <remarks>
        /// The gate is the instance and not the type beside it, which
        /// VendingMachineFullUpdate settled for the whole family. No captured
        /// weapon has a zero instance - they are all being carried - so the
        /// position block is untested here and is modelled the way its relatives
        /// are rather than the way the captures happen to look.
        /// </remarks>
        [AoMember(3)]
        [AoFlags("flag")]
        public int Instance
        {
            get
            {
                return this.instance;
            }

            set
            {
                this.instance = value;
                this.Owner = new Identity { Type = (IdentityType)this.identityType, Instance = value };
            }
        }

        [AoMember(4)]
        [AoUsesFlags("flag", typeof(Vector3), FlagsCriteria.HasNone, new[] { int.MaxValue })]
        public Vector3 Coordinate { get; set; }

        [AoMember(5)]
        [AoUsesFlags("flag", typeof(Quaternion), FlagsCriteria.HasNone, new[] { int.MaxValue })]
        public Quaternion Heading { get; set; }

        [AoMember(6)]
        public int Playfield { get; set; }

        /// <summary>
        /// The weapon's state machine. Type 1000015, instance 0 in every
        /// captured copy, which is a weapon with no state machine.
        /// </summary>
        /// <remarks>
        /// 1000015 is the identity type of a state machine - the client builds
        /// that identity from stat 450, statemachine, at 0x10087B15 and again in
        /// SimpleItemFullUpdate's own dispatcher at 0x100A1C31. The doors are
        /// what showed it is not a marker: they carry instance 1.
        /// </remarks>
        [AoMember(7)]
        public Identity StateMachine { get; set; }

        /// <summary>
        /// Stat 55, inventoryid. 0 or 1 across the captures.
        /// </summary>
        [AoMember(8)]
        public byte InventoryId { get; set; }

        /// <summary>
        /// Stat 220, currbodylocation - which hand the weapon is in. 6 and 8
        /// are the two that matter.
        /// </summary>
        [AoMember(9)]
        public byte BodyLocation { get; set; }

        /// <summary>
        /// The weapon's stats, as ids and values.
        /// </summary>
        /// <remarks>
        /// Seven in most captured copies and nine in the rest. The seven are
        /// flags, staticinstance, 701, 702, 703, multiplecount and energy; the
        /// nine add itemdelay and rechargedelay, which is how long the weapon
        /// takes to swing and how long before it can swing again.
        /// </remarks>
        [AoMember(10, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<CharacterStat, uint>[] Stats { get; set; }

        /// <summary>
        /// The weapon's name. Empty in every captured copy.
        /// </summary>
        /// <remarks>
        /// The length counts a terminating null when there is a string at all,
        /// which is why this is Int32Terminated and not Int32. An empty name is
        /// four zero bytes.
        /// </remarks>
        [AoMember(11, SerializeSize = ArraySizeType.Int32Terminated)]
        public string Name { get; set; }

        #endregion
    }
}
