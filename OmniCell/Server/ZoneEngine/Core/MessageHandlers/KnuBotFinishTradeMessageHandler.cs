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
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.KnuBot;

    #endregion

    /// <summary>
    /// The player has closed a trade window, one way or the other.
    /// </summary>
    [MessageHandler(MessageHandlerDirection.InboundOnly)]
    public class KnuBotFinishTradeMessageHandler :
        BaseMessageHandler<KnuBotFinishTradeMessage, KnuBotFinishTradeMessageHandler>
    {
        /// <summary>
        /// </summary>
        public override void Receive(MessageWrapper<KnuBotFinishTradeMessage> messageWrapper)
        {
            ICharacter npc = Pool.Instance.GetObject<ICharacter>(
                messageWrapper.Client.Controller.Character.Playfield.Identity,
                messageWrapper.MessageBody.Target);

            BaseKnuBot bot = BaseKnuBot.Of(npc, messageWrapper.Client.Controller.Character);
            if (bot == null || !bot.IsTalkingTo(messageWrapper.Client.Controller.Character))
            {
                return;
            }

            // Declined is the player shutting the window rather than agreeing
            // to it. Either way the window is gone by the time this arrives, so
            // whatever was in it has to go somewhere.
            bot.TradeFinish(messageWrapper.MessageBody.Declined != 0);
        }
    }
}
