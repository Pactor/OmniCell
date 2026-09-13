// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ItemMessageConstants.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ItemMessageConstants type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// Values that several item-bearing messages share.
    /// </summary>
    /// <remarks>
    /// Every one of these was recorded separately, in two or four different
    /// message classes, as a separate Unknown. None of them is understood. What
    /// is gained by putting them together is that the next person to look sees
    /// one question instead of six, and sees how much evidence is behind each -
    /// which is a great deal, and all of it saying the value never moves.
    /// </remarks>
    public static class ItemMessageConstants
    {
        #region Constants

        /// <summary>
        /// A constant every item-bearing message carries: 1,000,015.
        /// </summary>
        /// <remarks>
        /// Four messages carry it and none of them has ever carried anything
        /// else - WeaponItemFullUpdate in 3,906 captured copies,
        /// SimpleItemFullUpdate in 747, VendingMachineFullUpdate in 690 and
        /// ChestItemFullUpdate in 167. In two of the four it sits in the type
        /// half of an Identity whose instance is always zero, which is why it
        /// was written down four separate times as four separate unknowns.
        ///
        /// It is one thing, not four, and that is the whole of what is known.
        /// It is not in IdentityType, it is not a template id that varies with
        /// the item, and nothing in five thousand captured copies moves it.
        /// Naming it for what it might be would be a guess; naming it for where
        /// it appears at least stops the next person counting it as four
        /// problems.
        ///
        /// One thing has since been settled, in SimpleItemFullUpdate at least:
        /// the client does nothing with it. Across the whole of
        /// SimpleItemFullUpdateIIR_t - Gamecode.dll 0x100A0F9E to 0x100A1C60,
        /// every method the class has - the only code touching the two object
        /// offsets it lands in is the constructor, which zeroes them, and the
        /// reader, which fills them. Nothing reads them back.
        ///
        /// So the question is not what the client makes of it. It is why the
        /// server bothers to send it, and that is not answerable from the
        /// client at all.
        /// </remarks>
        public const int ItemMessageMarker = 1000015;

        /// <summary>
        /// The four fields that close an item-bearing message: 2, 50, an empty
        /// array, 3.
        /// </summary>
        /// <remarks>
        /// ChestItemFullUpdate and VendingMachineFullUpdate both end this way,
        /// with the same four values in the same order, in every one of their
        /// 167 and 690 captured copies. ChestItemFullUpdate was written sixteen
        /// bytes short until 2026-09-10 because nothing read them.
        ///
        /// Recorded here because two messages ending identically is a fact
        /// about the protocol rather than a coincidence of two packets, and
        /// because whatever explains one explains both.
        /// </remarks>
        public const int ItemTrailerFirst = 2;

        public const int ItemTrailerSecond = 50;

        public const int ItemTrailerLast = 3;

        #endregion
    }
}
