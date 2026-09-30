// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MissionType.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the MissionType type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// What a generated mission asks the player to do.
    /// </summary>
    /// <remarks>
    /// This is the value a mission carries in <see cref="QuestInfo.MissionIconId"/>,
    /// and it is the same number the quest record uses for the mission's type
    /// where the two have been compared. An authored quest carries an ordinary
    /// icon there instead - 244818 and 158429 are two of them - so the field is
    /// an icon that happens to be the type for the missions a terminal
    /// generates, rather than a type field in its own right.
    ///
    /// The five names come from the offers themselves. Twenty one rolls across
    /// two retail captures - 20260910-200346 stream 2 and 20260923-201746
    /// stream 8 - were put back through this project's serializer, and each
    /// offer's icon was set against the assignment text the same record
    /// carries. The texts are formulaic and the split is clean: every 11330
    /// says "kill this monster" or "must be cleansed", every 11342 asks for one
    /// item to be used on another, and so on through the five.
    ///
    /// Three of the five agree with the type codes read out of the quest record
    /// in a mission the player actually entered, which were found separately
    /// and earlier: 0x2C47 for find person, 0x2C49 for find item and 0x2C4E for
    /// repair. Those are 11335, 11337 and 11342. The other two here are new.
    ///
    /// One more value belongs to this set and is not named. 11340 appears in
    /// the captured quest records but in none of the 105 offers, so there is
    /// nothing to read its meaning off.
    /// </remarks>
    public enum MissionType
    {
        /// <summary>
        /// Fetch an item and bring it back to the terminal. 0x2C41.
        /// </summary>
        /// <remarks>
        /// "Please hurry to Omni-1 HQ and find it in the Omni-Tek Headquarters
        /// office, then bring it back here", and forty six more like it. The
        /// return leg is what separates this from <see cref="FindItem"/>.
        /// </remarks>
        ReturnItem = 11329,

        /// <summary>
        /// Kill a named character, or everything in the building. 0x2C42.
        /// </summary>
        /// <remarks>
        /// Both wordings carry this one value: "If you travel to Mort you will
        /// find the hideout - and kill this monster!" and "Please cleanse the
        /// Gritty Homes in Belial Forest of all monsters". So a single kill and
        /// a clear-out are one type here, however differently they read.
        /// </remarks>
        KillPerson = 11330,

        /// <summary>
        /// Find a named character and look at them. 0x2C47.
        /// </summary>
        /// <remarks>
        /// "please track him/her down, and with the utmost care, approach this
        /// character, observe it and tell us what happens". Observing is the
        /// whole of it: in the 20260923-114223 capture the mission finished
        /// 0.47 s after the target was selected, with no attack.
        /// </remarks>
        FindPerson = 11335,

        /// <summary>
        /// Find an item in the building. 0x2C49.
        /// </summary>
        /// <remarks>
        /// "Fight your way inside, pick up the Radioactive Isotope Container,
        /// and destroy it safely in your sub-space containment field". The
        /// client does not pick anything up for this: one LookAt on the item
        /// finished it in the 20260923-125821 capture and the item stayed on
        /// the floor.
        /// </remarks>
        FindItem = 11337,

        /// <summary>
        /// Use one item on another. 0x2C4E.
        /// </summary>
        /// <remarks>
        /// "Add some good old Spiked Food Sacks to the Theft Secure Food
        /// Dispenser to make them docile enough through this breeding season".
        /// On the wire that is a GenericCmd UseItemOnItem naming the inventory
        /// item and then the fixture.
        /// </remarks>
        Repair = 11342,
    }
}
