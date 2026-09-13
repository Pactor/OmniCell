// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FlagsCriteria.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the FlagsCriteria type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization
{
    public enum FlagsCriteria
    {
        HasAll, 

        HasAny, 

        EqualsToAny, 

        /// <summary>
        /// Present when the flag equals none of the listed values.
        /// </summary>
        /// <remarks>
        /// Added for the mail record, whose reader compares a byte against
        /// 1 and reads its tail when it does not match. HasNone is not the
        /// same test - it asks whether bits are set, so a byte of 3 would
        /// take the wrong branch - and inverting EqualsToAny by listing
        /// every other value is not a test at all.
        /// </remarks>
        NotEqualsToAny,

        /// <summary>
        /// Present when at least one of the listed bits is clear.
        /// </summary>
        /// <remarks>
        /// The complement of HasAll, and added for FullCharacter's research
        /// entries, whose reader branches on whether the top twenty four bits
        /// of a word are all set. HasNone is not it - that asks whether any of
        /// the bits are set, and here all but one of them can be.
        /// </remarks>
        NotHasAll,

        HasNone,

        Default
    }
}