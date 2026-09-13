// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KnuBotTradeAction.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the KnuBotTradeAction type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// Which way an item is moving in a conversation's trade window.
    /// </summary>
    /// <remarks>
    /// Both values come from the two exported functions that send a
    /// <see cref="Messages.N3Messages.KnuBotTradeMessage"/>. They are otherwise
    /// identical calls into the same constructor, and this is the argument that
    /// differs.
    /// </remarks>
    public enum KnuBotTradeAction
    {
        /// <summary>
        /// N3Msg_NPCChatAddTradeItem, which pushes 0 at 0x10017F5C.
        /// </summary>
        AddItem = 0,

        /// <summary>
        /// N3Msg_NPCChatRemoveTradeItem, which pushes 1 at 0x10017FD7.
        /// </summary>
        RemoveItem = 1
    }
}
