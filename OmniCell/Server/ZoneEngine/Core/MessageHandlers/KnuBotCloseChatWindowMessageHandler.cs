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

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Controllers;
    using ZoneEngine.Core.KnuBot;

    #endregion

    /// <summary>
    /// </summary>
    [MessageHandler(MessageHandlerDirection.All)]
    public class KnuBotCloseChatWindowMessageHandler :
        BaseMessageHandler<KnuBotCloseChatWindowMessage, KnuBotCloseChatWindowMessageHandler>
    {
        /// <summary>
        /// </summary>
        /// <param name="messageWrapper">
        /// </param>
        public override void Receive(MessageWrapper<KnuBotCloseChatWindowMessage> messageWrapper)
        {
            ICharacter npc =
                Pool.Instance.GetObject<ICharacter>(
                    messageWrapper.Client.Controller.Character.Playfield.Identity,
                    messageWrapper.MessageBody.Target);
            NPCController controller = npc == null ? null : npc.Controller as NPCController;
            BaseKnuBot bot = controller == null
                                 ? null
                                 : controller.KnuBotFor(messageWrapper.Client.Controller.Character);
            if (bot != null && bot.IsTalkingTo(messageWrapper.Client.Controller.Character))
            {
                bot.Answer(KnuBotOptionId.WindowClosed);
                controller.EndKnuBotDialog(messageWrapper.Client.Controller.Character);
                // ((NPCController)npc.Controller).KnuBot.CloseChatWindow();
            }
        }

        public void Send(ICharacter character, Identity knuBotIdentity, int secondsToClose = 3)
        {
            this.Send(character, this.FillClose(character, knuBotIdentity, secondsToClose));
        }

        private MessageDataFiller FillClose(ICharacter character, Identity knuBotIdentity, int secondsToClose)
        {
            return x =>
            {
                x.Identity = character.Identity;
                x.Target = knuBotIdentity;
                x.Unknown = 0;
                x.Version = 2;
                x.Seconds = secondsToClose;

                // No reason given. The live server puts text here when it is
                // closing the window because the player walked off - "You are
                // too far away from ICC Immigration Officer Bill to continue
                // this conversation." - and nothing empty otherwise, which is
                // sixty of the eighty two captured copies.
                x.Reason = string.Empty;
            };
        }
    }
}
