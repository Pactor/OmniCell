#region License

// Copyright (c) 2005-2014, CellAO Team
// 
// 
// All rights reserved.
// 
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

#endregion

namespace ZoneEngine.Core.MessageHandlers
{
    #region Usings ...

    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;
    using OmniCell.Core.Network;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.KnuBot;

    #endregion

    /// <summary>
    /// An item going into or out of a character's trade window.
    /// </summary>
    [MessageHandler(MessageHandlerDirection.InboundOnly)]
    public class KnuBotTradeMessageHandler : BaseMessageHandler<KnuBotTradeMessage, KnuBotTradeMessageHandler>
    {
        /// <summary>
        /// </summary>
        protected override void Read(KnuBotTradeMessage message, IZoneClient client)
        {
            ICharacter npc = Pool.Instance.GetObject<ICharacter>(
                client.Controller.Character.Playfield.Identity,
                message.Target);

            BaseKnuBot bot = BaseKnuBot.Of(npc, client.Controller.Character);
            if (bot == null || !bot.IsTalkingTo(client.Controller.Character))
            {
                return;
            }

            // The client sends this both ways round and the action says which.
            // It used to take the item out of the inventory whichever way it
            // was going, which is right for AddItem and backwards for
            // RemoveItem - the player pulling an item back out of the window
            // lost it.
            //
            // Both ways now go to the character being traded with, which holds
            // the item until the trade is settled and can give it back.
            if (message.Action == KnuBotTradeAction.AddItem)
            {
                bot.TradeAdd(message.Item);
            }
            else
            {
                bot.TradeRemove(message.Item);
            }
        }
    }
}
