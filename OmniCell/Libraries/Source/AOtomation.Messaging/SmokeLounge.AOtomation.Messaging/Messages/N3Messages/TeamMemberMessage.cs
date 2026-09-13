// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TeamMemberMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TeamMemberMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.TeamMember)]
    public class TeamMemberMessage : N3Message
    {
        #region Constructors and Destructors

        public TeamMemberMessage()
        {
            this.N3MessageType = N3MessageType.TeamMember;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The member this message is about.
        /// </summary>
        /// <remarks>
        /// The dispatcher at Gamecode.dll 0x1007A580 resolves this identity in
        /// the playfield before it does anything else, and hands it on as the
        /// member being added or removed.
        /// </remarks>
        [AoMember(0)]
        public Identity Character { get; set; }

        /// <summary>
        /// The team the member belongs to, or 0:0 to take them out of one.
        /// </summary>
        /// <remarks>
        /// Assigned to the member rather than read: the dispatcher passes it to
        /// 0x10065C8B, which stores it on the member when its instance half is
        /// set and writes a null identity over three fields and clears a flag
        /// when it is not. That is what makes the empty identity a removal
        /// rather than a missing value.
        /// </remarks>
        [AoMember(1)]
        public Identity Team { get; set; }

        /// <summary>
        /// Which group of a raid the member is in, or -1 for a plain team.
        /// </summary>
        /// <remarks>
        /// The dispatcher hands this to the team's add-member routine at
        /// 0x10066361, which keeps it at the team's +0x38 when the member is
        /// the local player - so the team remembers one of these, its own.
        /// What it indexes is the array of six group pointers at the team's
        /// +0x20: the accessors at 0x10065CE1, 0x10065CFC and 0x10066512 all
        /// take a group index, fall back to +0x38 when it is negative, and
        /// clamp to the first group when that is negative too, and the loop at
        /// 0x100664A2 walks six of them. 0x10065D2F answers "am I in a group"
        /// with +0x38 >= 0, and only after 0x10065D17 has agreed the team
        /// identity is a TeamWindow. A raid is six teams; an ordinary team is
        /// the one that is not in one, and sends -1.
        /// </remarks>
        [AoMember(2)]
        public int RaidGroup { get; set; }

        /// <summary>
        /// The member's level.
        /// </summary>
        /// <remarks>
        /// Read off a two account capture: the two members carried 10 and 46
        /// here, and 10 and 46 are exactly stat 54 in each account's own
        /// FullCharacter.
        /// </remarks>
        [AoMember(3)]
        public int Level { get; set; }

        /// <summary>
        /// The member's profession, as <see cref="GameData.Profession"/>
        /// numbers them - but sixteen bits on the wire rather than the enum's
        /// thirty two, which is why this is a short.
        /// </summary>
        /// <remarks>
        /// Same capture: 4 and 14 here, and 4 and 14 are exactly stat 60 in
        /// each account's own FullCharacter. A Fixer and a Keeper.
        /// </remarks>
        [AoMember(4)]
        public short Profession { get; set; }

        /// <summary>
        /// The member's name. At most a hundred characters.
        /// </summary>
        /// <remarks>
        /// The reader refuses a length above 0x64 and the writer clamps to the
        /// same number, so a hundred is the limit from both sides.
        /// </remarks>
        [AoMember(5, SerializeSize = ArraySizeType.Int32)]
        public string Name { get; set; }

        #endregion
    }
}