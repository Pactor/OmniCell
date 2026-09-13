// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayfieldDynelRun.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PlayfieldDynelRun type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// One run of consecutively numbered dynels in a playfield.
    /// </summary>
    /// <remarks>
    /// PlayfieldAnarchyF ends with a table of these, and it is a summary of
    /// everything the playfield contains that is not a character: the doors, the
    /// vending machines, and whatever else has an identity the client will be
    /// told about.
    ///
    /// The captures prove the shape arithmetically. Arete Landing sends five
    /// runs, with counts 1, 1, 8, 16 and 1, and start indices 0, 1, 2, 10 and
    /// 26 - each start is the previous start plus the previous count, so they
    /// are cumulative positions in one list twenty seven long. The run of eight
    /// is IdentityType.VendingMachine and the live server sends exactly eight
    /// VendingMachineFullUpdate messages on entering that playfield; the two
    /// runs of one are IdentityType.Door and it sends exactly two
    /// DoorStatusUpdate messages. The table says what is there and the entry
    /// sequence then describes each one.
    ///
    /// Instances are consecutive within a run: the two doors are 280037385 and
    /// 280037386, which is why one identity is enough to describe a run of any
    /// length.
    /// </remarks>
    public class PlayfieldDynelRun
    {
        #region Public Properties

        /// <summary>
        /// What kind of dynel this run is made of.
        /// </summary>
        public IdentityType Type { get; set; }

        /// <summary>
        /// One in every captured run.
        /// </summary>
        /// <remarks>
        /// Twenty seven runs across nine captured copies of the message and not
        /// one of them is anything else. Kept because the four bytes are there.
        /// </remarks>
        public int Unknown { get; set; }

        /// <summary>
        /// Where this run starts in the playfield's dynel list.
        /// </summary>
        /// <remarks>
        /// Cumulative: each is the previous start plus the previous count.
        /// </remarks>
        public int StartIndex { get; set; }

        /// <summary>
        /// How many dynels are in this run.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// The identity instance of the first dynel in the run.
        /// </summary>
        /// <remarks>
        /// The rest follow it consecutively.
        /// </remarks>
        public int FirstInstance { get; set; }

        #endregion
    }
}
