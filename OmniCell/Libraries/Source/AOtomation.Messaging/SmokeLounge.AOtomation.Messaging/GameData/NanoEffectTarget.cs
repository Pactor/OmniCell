// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NanoEffectTarget.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the NanoEffectTarget type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// Who an effect applies to.
    /// </summary>
    /// <remarks>
    /// SpellStat 32, the third of the four arguments SpellFormat_c gives every
    /// effect. The names are the client's own. Category 2015 of its text
    /// database is this list, and category 506 is the five of them the item
    /// description builder prints as headings - "On Self:", "On User:", "On
    /// Target:", "On Item:" and "On Fighting Target:", at ids 1, 2, 3, 4 and 14,
    /// which is how the two tables were tied together.
    ///
    /// The builder is at Gamecode.dll 0x100215C1: when it meets an effect whose
    /// target differs from the last one it printed, it fetches the heading for
    /// the new value and prints it above the effects that follow.
    ///
    /// Zero is not in either table. It is the default the format gives the
    /// argument and the value the description builder skips on. Across 5,175
    /// captured effects only None, Self, User and Target occur.
    /// </remarks>
    public enum NanoEffectTarget
    {
        /// <summary>
        /// Neither table names this one. It is the default the format gives
        /// the argument, and the value the description builder skips on.
        /// </summary>
        None = 0,

        /// <summary>
        /// "self"
        /// </summary>
        Self = 1,

        /// <summary>
        /// "user"
        /// </summary>
        User = 2,

        /// <summary>
        /// "target"
        /// </summary>
        Target = 3,

        /// <summary>
        /// "item"
        /// </summary>
        Item = 4,

        /// <summary>
        /// "transfer"
        /// </summary>
        Transfer = 5,

        /// <summary>
        /// "ground"
        /// </summary>
        Ground = 6,

        /// <summary>
        /// "person spotted"
        /// </summary>
        PersonSpotted = 7,

        /// <summary>
        /// "attacker"
        /// </summary>
        Attacker = 8,

        /// <summary>
        /// "victim"
        /// </summary>
        Victim = 9,

        /// <summary>
        /// "master"
        /// </summary>
        Master = 10,

        /// <summary>
        /// "enemy healer"
        /// </summary>
        EnemyHealer = 11,

        /// <summary>
        /// "friend attacker"
        /// </summary>
        FriendAttacker = 12,

        /// <summary>
        /// "command target"
        /// </summary>
        CommandTarget = 13,

        /// <summary>
        /// "fight target"
        /// </summary>
        FightTarget = 14,

        /// <summary>
        /// "scary enemy"
        /// </summary>
        ScaryEnemy = 15,

        /// <summary>
        /// "follow target"
        /// </summary>
        FollowTarget = 16,

        /// <summary>
        /// "last opponent"
        /// </summary>
        LastOpponent = 17,

        /// <summary>
        /// "person leaving"
        /// </summary>
        PersonLeaving = 18,

        /// <summary>
        /// "person lost"
        /// </summary>
        PersonLost = 19,

        /// <summary>
        /// "pet"
        /// </summary>
        Pet = 20,

        /// <summary>
        /// "area"
        /// </summary>
        Area = 21,

        /// <summary>
        /// "commander"
        /// </summary>
        Commander = 22,

        /// <summary>
        /// "selected target"
        /// </summary>
        SelectedTarget = 23,

        /// <summary>
        /// "last follow target"
        /// </summary>
        LastFollowTarget = 24
    }
}
