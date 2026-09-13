// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QuestAlternativeEntry.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the QuestAlternativeEntry type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One quest on offer at a mission terminal, and the byte that follows it.
    /// </summary>
    /// <remarks>
    /// The byte is why this type exists. Both messages that carry quests read
    /// the same record - Gamecode 0x100ABEA7 - and each puts a byte after it,
    /// but they do not put it in the same place. QuestFullUpdate reads the
    /// whole list and then takes one byte, once, at 0x100ACF7E.
    /// QuestAlternative takes one after every quest, inside its loop, at
    /// 0x100CB2EE.
    ///
    /// The byte used to sit on QuestInfo itself, which is the same bytes on the
    /// wire for QuestAlternative and for a QuestFullUpdate carrying a single
    /// quest - and wrong for a QuestFullUpdate carrying more. Moving it onto
    /// the message fixed that and broke this, which is how the difference
    /// turned up at all.
    /// </remarks>
    public class QuestAlternativeEntry
    {
        /// <summary>
        /// The quest.
        /// </summary>
        [AoMember(0)]
        public QuestInfo Quest { get; set; }

        /// <summary>
        /// The byte the client reads after each quest in this message.
        /// </summary>
        /// <remarks>
        /// Read at 0x100CB2EE into a stack slot, and followed from there on
        /// 2026-09-12 as far as the interface. The reader appends it to the
        /// vector at the message + 0x70 while the mission itself goes into the
        /// one at + 0x60, and the dispatcher at 0x100CB1B8 walks the two
        /// together, pairing each byte with its mission and with the message's
        /// seed into the records 0x10056EDD hands the mission window - the
        /// list N3Msg_GetMissionSelectionList returns. So it is per mission and
        /// it is used.
        ///
        /// It stops at the interface. GUI.dll 0x100D2A38 copies each record
        /// whole into a MissionListEntry_c at + 0x120, so this byte is that
        /// view's + 0x128, and nothing in the window's code reads it back. Nor
        /// does it return: choosing a mission hands the whole record to
        /// N3Msg_SelectMission at 0x100D0DA5, whose implementation builds its
        /// message through 0x100CB00C from the record's + 0 and + 4 - the
        /// mission's Identity - and nothing else.
        ///
        /// Across the four captured copies the five bytes read 0, 1, 2, then 3
        /// or 6, then 6, 9 or 24 - index-like at the front and not at the back.
        /// Recorded in case a terminal offering fewer than five ever separates
        /// them.
        /// </remarks>
        [AoMember(1)]
        public byte Trailer { get; set; }
    }
}
