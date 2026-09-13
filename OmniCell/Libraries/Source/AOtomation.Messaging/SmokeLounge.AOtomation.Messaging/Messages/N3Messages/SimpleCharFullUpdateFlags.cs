// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleCharFullUpdateFlags.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SimpleCharFullUpdateFlags type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;

    [Flags]
    public enum SimpleCharFullUpdateFlags
    {
        // 0000 0000 0000 0000 0000 0000 0000 0000
        None = 0x00000000, 

        // 0000 0000 0000 0000 0000 0000 0000 0001
        IsNpc = 0x00000001, 

        // 0000 0000 0000 0000 0000 0000 0000 0010
        UnknownFlag = 0x00000002,

        /// <summary>
        /// A bit that often travels with a head mesh and is not caused by one.
        /// </summary>
        /// <remarks>
        /// The name is a leftover from a measurement made over 2,331 character
        /// updates in five sessions, where HasHeadMesh never appeared without
        /// this bit. Over 18,530 updates it appears without it 944 times, so
        /// the two are correlated and nothing more, and the writer used to force
        /// this bit alongside HasHeadMesh - inventing a byte the sender had not
        /// sent. It no longer does: the bit is preserved from whatever was read
        /// and decided by nothing.
        ///
        /// What it means in its own right is still not known. Naming it for the
        /// thing it usually accompanies would be a guess dressed as a fact, and
        /// the last such guess is what this remark is correcting.
        /// </remarks>
        AccompaniesHeadMesh = 0x00000008,

        // 0000 0000 0000 0000 0000 0001 0000 0000
        HasExtendedTextures = 0x00000010,

        /// <summary>
        /// An Identity follows the playfield id: the dynel this character is
        /// parented to.
        /// </summary>
        /// <remarks>
        /// Named HasFightingTarget until 2026-09-11, on nothing but the company
        /// the bit keeps. The dispatcher settles it - see
        /// <see cref="SimpleCharFullUpdateMessage.ParentDynel"/>.
        /// </remarks>
        HasParentDynel = 0x00000020, 

        // 0000 0000 0000 0000 0000 0000 0100 0000
        HasPlayfieldId = 0x00000040, 

        // 0000 0000 0000 0000 0000 0000 1000 0000
        HasHeadMesh = 0x00000080,

        // 0000 0000 0000 0000 0000 0001 0000 0000
        HasNoWeaponPairs = 0x00000100,

        // 0000 0000 0000 0000 0000 0010 0000 0000
        HasHeading = 0x00000200, 

        // 0000 0000 0000 0000 0000 0100 0000 0000
        // TODO: only temporary name, needs research
        IsUnderAttack = 0x00000400,

        // 0000 0000 0000 0000 0000 1000 0000 0000
        HasSmallHealth = 0x00000800, 

        // 0000 0000 0000 0000 0001 0000 0000 0000
        HasExtendedLevel = 0x00001000, 

        // 0000 0000 0000 0000 0010 0000 0000 0000
        HasExtendedRunSpeed = 0x00002000, 

        // 0000 0000 0000 0000 0100 0000 0000 0000
        HasSmallHealthDamage = 0x00004000,

        // 0000 0000 0000 0001 0000 0000 0000 0000
        HasWaypoints = 0x00010000,

        // 0000 0000 0000 0010 0000 0000 0000 0000
        HasSmallNpcFamily = 0x00020000, 

        // 0000 0000 0000 1000 0000 0000 0000 0000
        HasSmallNpcLosHeight = 0x00080000, 

        // 0000 0000 0010 0000 0000 0000 0000 0000
        UnknownFlag2 = 0x00200000,

        // 0000 0000 1000 0000 0000 0000 0000 0000
        // Blue name and not attackable
        IsImmune = 0x00800000,

        // 0000 0001 0000 0000 0000 0000 0000 0000
        UnknownFlag3 = 0x01000000,

        // 0000 0010 0000 0000 0000 0000 0000 0000
        UnknownDataFlag = 0x02000000,

        // 0000 0100 0000 0000 0000 0000 0000 0000
        HasOrgName = 0x04000000,

        // 0000 1000 0000 0000 0000 0000 0000 0000
        IsPet = 0x08000000,

        // 0001 0000 0000 0000 0000 0000 0000 0000
        UnknownFlag5 = 0x10000000,

        // 0010 0000 0000 0000 0000 0000 0000 0000
        UnknownFlag4 = 0x20000000,

        /// <summary>
        /// Gates a list of Identities at the very end of the message.
        /// </summary>
        /// <remarks>
        /// The last flag the reader tests, at Gamecode.dll 0x1007970C, and the
        /// only one that had no entry here at all. It reads an X3F1 counted
        /// list of texture swaps - see
        /// <see cref="SmokeLounge.AOtomation.Messaging.GameData.CatTexture"/>.
        ///
        /// It appears in none of the captured sessions, so the list has never
        /// been seen with anything in it; what it is for comes from the
        /// dispatcher rather than from a capture.
        /// </remarks>
        HasCatTextures = 0x40000000,
    }
}