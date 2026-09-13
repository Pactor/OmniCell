// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MarketSendMessage.cs" company="OmniCell">
//   Copyright (c) 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the MarketSendMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    #region Usings ...

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    #endregion

    /// <summary>
    /// A deposit to the market: up to eight items and an amount of credits.
    /// </summary>
    /// <remarks>
    /// Extracted-client MarketSendIIR_c has vtable 0x10159BCC, reader
    /// 0x1002D83D, writer 0x1002D65A and dispatcher 0x1002D5BA. The reader
    /// clears its vector, takes an Identity, an int32 and an X3F1-counted list
    /// of Identities, and trims that list to eight entries at 0x1002D888 - so
    /// eight is the most that can arrive whatever the count says. The writer
    /// emits the same three in the same order.
    ///
    /// The constructor at 0x1002D787 is called from one place,
    /// N3Msg_SendMarketItem(vector&lt;Identity_t&gt; const&amp;, int) at
    /// 0x1001E175, and the GUI calls that from one place too - the window whose
    /// own strings a few hundred bytes on are "Deposit to Market", "SendButton"
    /// and Views/MarketSendWindow.xml.
    ///
    /// The dispatcher resolves the Identity to a character, raises
    /// "Feedback_MarketItemSent" on it, and emits GlobalSignals_c + 0x288 with
    /// no arguments at all - a bare notification - so nothing on the client
    /// reads the amount or the list back. Everything here is named from the
    /// sending side.
    ///
    /// No capture contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.MarketSend)]
    public class MarketSendMessage : N3Message
    {
        public MarketSendMessage()
        {
            this.N3MessageType = N3MessageType.MarketSend;
        }

        /// <summary>
        /// The character making the deposit.
        /// </summary>
        /// <remarks>
        /// A copy of the message's own identity: N3Msg_SendMarketItem takes it
        /// from GetClientControlDynel, and the dispatcher resolves it to the
        /// character it raises "Feedback_MarketItemSent" on.
        /// </remarks>
        [AoMember(0)]
        public Identity Sender { get; set; }

        /// <summary>
        /// Credits going in with the items. Stat 61, cash.
        /// </summary>
        /// <remarks>
        /// The second argument of N3Msg_SendMarketItem, whose signature carries
        /// no parameter names, and nothing on the receiving side reads it back.
        /// The GUI names it, at GUI.dll 0x10009C40.
        ///
        /// The window reads the text field at its + 0x78 and puts it through
        /// atoi, or takes zero when the field is empty. A negative amount is
        /// refused outright: the handler fetches LDB text 0x6E,
        /// "Feedback_MarketNoNegative", pushes it at the chat signal
        /// GlobalSignals_c + 0x184 and returns without sending anything. A
        /// positive one is then clamped - N3Msg_GetSkill(0x3D, 2), which is
        /// stat 61, cash, and <c>if (amount &gt; cash) amount = cash</c> at
        /// 0x10009D2A - so the client will not offer more than the character
        /// has.
        ///
        /// The send itself happens when there is either an item or an amount:
        /// the guard at 0x10009D8E lets a deposit of pure credits through with
        /// an empty list, which is what says this is not an item's price.
        /// </remarks>
        [AoMember(1)]
        public int Credits { get; set; }

        /// <summary>
        /// The items being deposited, at most eight.
        /// </summary>
        /// <remarks>
        /// The vector N3Msg_SendMarketItem is handed, which the window builds
        /// at 0x10009D57 by walking its own slots and skipping the empty ones.
        /// The reader trims whatever arrives to eight entries afterwards
        /// regardless of the count on the wire.
        /// </remarks>
        [AoMember(2, SerializeSize = ArraySizeType.X3F1)]
        public Identity[] Items { get; set; }
    }
}
