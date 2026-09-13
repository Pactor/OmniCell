// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArraySizeType.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ArraySizeType type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization
{
    public enum ArraySizeType
    {
        NoSerialization, 

        Byte, 

        Int16, 

        Int32, 

        X3F1,

        /// <summary>
        /// A 32 bit length that counts a terminating null, when there is a
        /// string at all.
        /// </summary>
        /// <remarks>
        /// Two conventions live in this protocol. Most length-prefixed strings
        /// are the length and exactly that many characters - FormatFeedback is
        /// one. An item name is the length, the characters, and a terminator,
        /// with the length counting it: "Mission key to Alien Mothership" is
        /// thirty one characters and goes out with a length of thirty two.
        ///
        /// An empty name is written as a length of nothing rather than a length
        /// of one and a lone terminator. Every captured empty item name is four
        /// zero bytes, and the two that are not empty carry the terminator, so
        /// the rule is that the terminator comes with the string and not
        /// instead of it.
        ///
        /// This has to be said per field rather than fixed in the string reader.
        /// SimpleCharFullUpdate and CorpseFullUpdate write their own terminator
        /// in their own serializers, and a reader that kept one would hand them
        /// a string that terminated itself twice.
        /// </remarks>
        Int32Terminated,

        NullTerminated
    }
}