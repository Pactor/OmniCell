// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AoText.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the AoText type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization
{

    using System.Text;

    /// <summary>
    /// ISO-8859-1, which is the only encoding that survives this wire.
    /// </summary>
    /// <remarks>
    /// This was Encoding.ASCII, which turns every byte from 0x80 up into a
    /// question mark - in both directions. A retail FormatFeedback caught on
    /// 2026-09-11 carries C2 85 inside its text and we wrote back 3F 3F, which
    /// is what that substitution looks like on the wire.
    ///
    /// ISO-8859-1 is the right choice rather than UTF-8 for two reasons. It
    /// maps 0x00 to 0xFF onto U+0000 to U+00FF and back with nothing lost, so
    /// whatever the server sends comes back unchanged whether or not we know
    /// what it means. And it stays one byte per character, which every length
    /// field in this protocol assumes - a multi-byte encoding would make
    /// str.Length and the byte count disagree and break the counts everywhere.
    ///
    /// cp1252 would not do: five of its bytes are undefined and would come
    /// back as question marks again.
    /// </remarks>
    internal static class AoText
    {
        public static readonly Encoding Encoding = Encoding.GetEncoding(28591);
    }
}
