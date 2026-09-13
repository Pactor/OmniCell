// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ItemStatPair.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ItemStatPair type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// A stat id and the value an item carries for it.
    /// </summary>
    /// <remarks>
    /// The optional tail of WeaponItemFullUpdate. The two seen in the captures
    /// are itemdelay (294) and rechargedelay (210) - how long a weapon takes to
    /// swing and how long before it can swing again.
    /// </remarks>
    public class ItemStatPair
    {
        #region Public Properties

        /// <summary>
        /// The stat id, as in StatIds.
        /// </summary>
        public int Stat { get; set; }

        /// <summary>
        /// What the item sets it to.
        /// </summary>
        public int Value { get; set; }

        #endregion
    }
}
