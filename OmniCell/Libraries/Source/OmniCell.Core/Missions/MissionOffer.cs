#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace OmniCell.Core.Missions
{
    #region Usings ...

    using SmokeLounge.AOtomation.Messaging.GameData;

    #endregion

    /// <summary>
    /// One mission a terminal is offering, or one a character has taken.
    /// </summary>
    /// <remarks>
    /// A mission is not a quest. It is generated on the spot, it is never
    /// written down anywhere but here, and it has no entry in the quest tables,
    /// which is why it does not go through <see cref="Quests.QuestManager"/>.
    /// What it shares with a quest is the wire: the client is shown both
    /// through <c>QuestInfo</c>, and the difference on the wire is that a
    /// mission's <c>Flags</c> is 0 where an authored quest's is 2.
    ///
    /// Everything here is filled from what a captured offer carries. See
    /// Documentation/Missions.md.
    /// </remarks>
    public class MissionOffer
    {
        /// <summary>
        /// The identity the client names this mission by.
        /// </summary>
        /// <remarks>
        /// While it is on offer this is the identity the terminal listed it
        /// under; once accepted the server allocates a fresh one, which is what
        /// retail does - the five offers of 2026-09-26 were instances ...3380
        /// to ...3384, and the one accepted came back as ...3385.
        /// </remarks>
        public int Instance { get; set; }

        /// <summary>
        /// What the player is here to do.
        /// </summary>
        public MissionType Type { get; set; }

        /// <summary>
        /// The mission's quality, which is the character's level times the
        /// multiplier the difficulty sets.
        /// </summary>
        public int Quality { get; set; }

        /// <summary>
        /// The difficulty the terminal was asked at, 1 to 11.
        /// </summary>
        public int Difficulty { get; set; }

        /// <summary>
        /// Whether this is a team mission.
        /// </summary>
        /// <remarks>
        /// The originator says so: 1 is a solo booth and 2 its team version,
        /// and IsTeamOriginator at GameData.dll 0x10002D23 pairs the even
        /// values with the odd ones all the way up. It matters because a team
        /// mission's building is not the same shape as a solo one - see
        /// <see cref="Floors"/>.
        /// </remarks>
        public bool Team { get; set; }

        /// <summary>
        /// How many floors the building has.
        /// </summary>
        /// <remarks>
        /// Of 276 captured buildings, 260 are flat and 16 have three or four
        /// contiguous floors; the ones with floors are the team missions. So a
        /// solo mission is one floor and a team mission three or four, and
        /// nothing captured shows a team building flat or a solo one stacked.
        /// </remarks>
        public int Floors { get; set; }

        /// <summary>
        /// The terminal that offered it.
        /// </summary>
        public Identity Terminal { get; set; }

        /// <summary>
        /// The room pool the building will be generated from, as the playfield
        /// id the zone-in packet names it with.
        /// </summary>
        public int Pool { get; set; }

        /// <summary>
        /// The playfield the building stands in.
        /// </summary>
        public int Playfield { get; set; }

        /// <summary>
        /// Where in that playfield, for the marker the client draws.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Where in that playfield, for the marker the client draws.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Where in that playfield, for the marker the client draws.
        /// </summary>
        public float Z { get; set; }

        /// <summary>
        /// The building's name, as the key and the assignment text call it -
        /// "A building in Borealis".
        /// </summary>
        public string Building { get; set; }

        /// <summary>
        /// The playfield's own name, for the assignment text.
        /// </summary>
        public string PlayfieldName { get; set; }

        /// <summary>
        /// The reference the assignment quotes itself by - "7429-323-4" and
        /// "MXIL" are two captured ones. Cosmetic, and built from the seed.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The one line the mission list shows.
        /// </summary>
        public string ShortInfo { get; set; }

        /// <summary>
        /// The whole assignment.
        /// </summary>
        public string Info { get; set; }

        /// <summary>
        /// What the objective is called - the person to find, the monster to
        /// kill, the item to fetch or the part to fit.
        /// </summary>
        public string Objective { get; set; }

        public int CashReward { get; set; }

        public int ExperienceReward { get; set; }

        /// <summary>
        /// The item the terminal pays out, as a template pair and a quality.
        /// </summary>
        public int RewardLowId { get; set; }

        /// <summary>
        /// The item the terminal pays out, as a template pair and a quality.
        /// </summary>
        public int RewardHighId { get; set; }

        /// <summary>
        /// The part a repair mission hands over, which is the only thing that
        /// mends its fixture.
        /// </summary>
        /// <remarks>
        /// The assignment names it - "Add some Spiked Food Sacks to the Theft
        /// Secure Food Dispenser" - and says to use it: "For the repair task,
        /// use this component." Which item retail picks is not recorded, so
        /// this is the one the assignment was written around.
        /// </remarks>
        public int ComponentLowId { get; set; }

        /// <summary>
        /// The instance of the part once it is in the player's inventory.
        /// </summary>
        public int ComponentInstance { get; set; }

        /// <summary>
        /// The quality the reward item is offered at.
        /// </summary>
        /// <remarks>
        /// Usually the mission's, but not always: it is clamped into the
        /// band's own quality range, and a family of one template cannot
        /// move. A QL 39 mission was captured offering a QL 30 nano crystal
        /// and a QL 1 Trenchcoat.
        /// </remarks>
        public int RewardQuality { get; set; }

        /// <summary>
        /// The resource code the captured offers carry in the quest action -
        /// four characters in an identity of type 70099, such as "MRBO".
        /// </summary>
        /// <remarks>
        /// Every captured offer has one and the client is given it; what it
        /// keys is not known, so a generated mission carries a code built from
        /// its own seed rather than a value copied from a capture.
        /// </remarks>
        public int ObjectiveCode { get; set; }

        /// <summary>
        /// The instance of the key item handed over when this was accepted, or
        /// 0 while the mission is still on offer.
        /// </summary>
        public int KeyInstance { get; set; }

        /// <summary>
        /// The building and everything in it, once somebody has taken this on.
        /// Null while it is only an offer.
        /// </summary>
        /// <remarks>
        /// Made from the offer and nothing else, so the same mission always
        /// builds the same building - which is what lets a second character
        /// holding a duplicated key walk into the one the first is standing in.
        /// </remarks>
        public Mission Built { get; set; }

        /// <summary>
        /// The instance the building goes by, which the zone-in packet names
        /// as identity type 51103.
        /// </summary>
        public int BuildingInstance { get; set; }

        /// <summary>
        /// The instance the mission's playfield goes by.
        /// </summary>
        public int PlayfieldInstance { get; set; }

        /// <summary>
        /// The objective, once it has been picked up, as the instance of the
        /// item in the inventory. 0 while it is still on the floor.
        /// </summary>
        /// <remarks>
        /// Only a return item mission has one: a find item objective is looked
        /// at and left where it is.
        /// </remarks>
        public int Carried { get; set; }
    }
}
