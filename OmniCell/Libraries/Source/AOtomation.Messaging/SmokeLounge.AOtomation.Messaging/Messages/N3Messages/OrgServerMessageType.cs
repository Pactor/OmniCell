// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OrgServerMessageType.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the OrgServerMessageType type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    /// <summary>
    /// Which of the nine shapes an OrgServer carries.
    /// </summary>
    /// <remarks>
    /// The reader at Gamecode 0x10126D95 reads this byte, widens it, and fails
    /// the whole message unless it is between 1 and 9. Three of the nine were
    /// named here and the other six were absent until 2026-09-11 - and absent
    /// means unread, because the subclass table is keyed on this value, so a
    /// kind with no entry matched nothing and its body was never taken off the
    /// wire. Every one of the 404 captured copies is an OrgContract, which is
    /// why nothing had ever shown it.
    ///
    /// The six added carry no name because none of them has ever been
    /// captured. What each one reads is in its own class.
    /// </remarks>
    public enum OrgServerMessageType : byte
    {
        OrgKind1 = 0x01,

        OrgInfo = 0x02,

        OrgKind3 = 0x03,

        OrgKind4 = 0x04,

        OrgInvite = 0x05,

        OrgContract = 0x06,

        OrgKind7 = 0x07,

        OrgKind8 = 0x08,

        OrgKind9 = 0x09,
    }
}