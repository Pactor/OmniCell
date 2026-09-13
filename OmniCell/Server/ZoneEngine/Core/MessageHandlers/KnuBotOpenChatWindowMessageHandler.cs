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

    using SmokeLounge.AOtomation.Messaging.GameData;
    using OmniCell.Core.Network;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Controllers;

    #endregion

    /// <summary>
    /// </summary>
    /// <summary>
    /// Opening a conversation with an NPC.
    /// </summary>
    /// <remarks>
    /// This was outbound only, which meant the server could open a chat window
    /// but nothing could ask it to. The client sends one of these when you talk
    /// to an NPC - 44 of them across the captures - and every one was ignored,
    /// so no NPC in the world could be spoken to.
    ///
    /// It now starts that NPC's conversation, if it has one.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.All)]
    public class KnuBotOpenChatWindowMessageHandler :
        BaseMessageHandler<KnuBotOpenChatWindowMessage, KnuBotOpenChatWindowMessageHandler>
    {
        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <param name="knubotTarget">
        /// </param>
        /// <summary>
        /// A character is talking to something.
        /// </summary>
        protected override void Read(KnuBotOpenChatWindowMessage message, IZoneClient client)
        {
            if (client == null || client.Controller == null || client.Controller.Character == null)
            {
                return;
            }

            ICharacter character = client.Controller.Character;
            var npc = Pool.Instance.GetObject<ICharacter>(character.Playfield.Identity, message.Target);

            var controller = npc == null ? null : npc.Controller as NPCController;
            if (controller == null || controller.KnuBot == null)
            {
                // Nothing to say. The client is told the window is closed rather
                // than left waiting on a conversation that will never start.
                KnuBotCloseChatWindowMessageHandler.Default.Send(character, message.Target);
                return;
            }

            if (!controller.StartKnuBotDialog(character))
            {
                KnuBotCloseChatWindowMessageHandler.Default.Send(character, message.Target);
            }
        }

        public void Send(ICharacter character, Identity knubotTarget)
        {
            this.Send(character, this.KnuBotOpenWindow(character, knubotTarget), false);
        }

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <param name="knubotTarget">
        /// </param>
        /// <returns>
        /// </returns>
        private MessageDataFiller KnuBotOpenWindow(ICharacter character, Identity knubotTarget)
        {
            return x =>
            {
                x.Identity = character.Identity;
                x.Target = knubotTarget;
                x.Version = 2;
                x.Unknown2 = 1;
            };
        }
    }
}
