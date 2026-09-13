// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TowerField.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TowerField type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One entry of the list three messages share.
    /// </summary>
    /// <remarks>
    /// The reader is 0x1012905A, reached through the X3F1 loop at 0x10129471,
    /// and it takes an int32, an Identity, a counted string through the int16
    /// helper at 0x10038AF8, and two more int32s. InfoPacket's city land list,
    /// OrgServer kind 2 and GridDestinationSelect all use it.
    ///
    /// GridDestinationSelect is the one with a consumer that can be read.
    /// GUI.dll calls N3Msg_GetGridDestinationList once, from the list builder
    /// at 0x101003D8, and that walks the vector at stride 0x30 and touches
    /// three things: the int32 at + 0, which it hands to N3Msg_GetPFName; the
    /// Identity at + 4, which it carries into GridSelected; and the string at
    /// + 0xC, which it displays. It touches nothing else, so the two trailing
    /// int32s have no consumer on that path at all.
    ///
    /// That last question - whether InfoPacket's copies mean the same thing -
    /// was answered on 2026-09-11 by the client's own getter. InfoPacket's list
    /// lives at the record's + 0xB8, and the export that returns it is
    /// N3Msg_GetGridDestinationList: 0x10016CA5 resolves the examined dynel,
    /// takes the InfoPacket at + 0x7C and returns that member plus 0xB8. The
    /// client calls the member a grid destination list, in the same words it
    /// uses for the list GridDestinationSelect carries.
    ///
    /// The 695 and 635 that looked wrong for a playfield id were being compared
    /// against 6010, which is a city instance. Ordinary playfields are three
    /// digit numbers, and the two entries beside them are named Stret River
    /// Island and Aprils Rock Offense.
    ///
    /// The two trailing int32s kept neutral names on the strength of that walk
    /// until 2026-09-12, when a second consumer turned up on a path nobody had
    /// looked at. OrgServer carries this same record - kind 2's list, read by
    /// the same 0x1012905A at the same stride - and its dispatcher does not
    /// ignore them. At 0x101273A0 it asks ldb for category 0x1FC key
    /// "LC_AreaInfo", which is
    ///
    ///   In:    "%s"
    ///   Area:  %s
    ///   Type:  %s
    ///   Level: %d
    ///
    /// and feeds it four things in order: the playfield id at + 0 through the
    /// playfield-name lookup at 0x10036982, the string at + 0xC, the int32 at
    /// + 0x28 run through 0x10126BCD, and the int32 at + 0x2C as a plain
    /// number. So the client's own labels for the two are Type for + 0x28 and
    /// Level for + 0x2C - which is the opposite of their wire order, because
    /// the reader at 0x1012905A takes + 0x2C first.
    ///
    /// 0x10126BCD is worth knowing what it is, because the name it produces is
    /// not a lookup: it walks the table at 0x102C6F68 - 1000 M, 900 CM, 500 D,
    /// 400 CD, 100 C, 90 XC, 50 L, 40 XL, 10 X, 9 IX and down - and builds a
    /// Roman numeral, refusing anything above 3999 and falling back to "%d".
    /// So the type is a small number the interface shows as I, II, III.
    ///
    /// Nothing here contradicts the grid destination walk. That path really
    /// does touch only three of the five; this is a different consumer of the
    /// same record, which is why the fields had looked inert.
    /// </remarks>
    public class GridDestination
    {
        #region AoMember Properties

        /// <summary>
        /// The playfield the destination is in.
        /// </summary>
        /// <remarks>
        /// GridDestinationSelect's list builder hands it to N3Msg_GetPFName,
        /// and InfoPacket's two captured entries carry 695 and 635 beside the
        /// names Stret River Island and Aprils Rock Offense.
        /// </remarks>
        [AoMember(0)]
        public int PlayfieldId { get; set; }

        /// <summary>
        /// The Identity at + 4. GridDestinationSelect carries this one into
        /// the GridSelected it sends back.
        /// </summary>
        [AoMember(1)]
        public Identity Identity { get; set; }

        /// <summary>
        /// The counted string at + 0xC - the line the client displays.
        /// </summary>
        /// <remarks>
        /// "Stret River Island" and "Aprils Rock Offense" in InfoPacket's two
        /// captured entries, and the destination label in
        /// GridDestinationSelect.
        /// </remarks>
        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public string Name { get; set; }

        /// <summary>
        /// The int32 at + 0x2C. 30 and 67 in InfoPacket's two entries.
        /// </summary>
        /// <remarks>
        /// Untouched by the one consumer that can be traced - the grid list
        /// builder reads + 0, + 4 and + 0xC and stops.
        /// </remarks>
        /// <summary>
        /// The area's level, shown as a plain number.
        /// </summary>
        /// <remarks>
        /// Third from last on the wire and stored at record + 0x2C - the
        /// reader at 0x1012905A takes this one before the type, which sits at
        /// + 0x28, so the two are swapped between the wire and the record the
        /// same way WeaponPair's last two are.
        ///
        /// Named on 2026-09-12: OrgServer's kind 2 feeds it straight into the
        /// "Level" conversion of the LC_AreaInfo template. See the class
        /// remark. The grid destination list never reads it, which is why it
        /// was called reserved metadata until then.
        /// </remarks>
        [AoMember(3)]
        public int AreaLevel { get; set; }

        /// <summary>
        /// The area's type, which the interface shows as a Roman numeral.
        /// </summary>
        /// <remarks>
        /// Last on the wire, stored at record + 0x28. The same dispatcher
        /// feeds it to the "Type" conversion of LC_AreaInfo after turning it
        /// into a Roman numeral at 0x10126BCD, so it is a small number.
        /// See <see cref="AreaLevel"/>.
        /// </remarks>
        [AoMember(4)]
        public int AreaType { get; set; }

        #endregion
    }
}