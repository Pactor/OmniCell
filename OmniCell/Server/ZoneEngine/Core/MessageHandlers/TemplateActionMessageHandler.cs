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
    using OmniCell.Core.Items;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    #endregion

    /// <summary>
    /// </summary>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class TemplateActionMessageHandler : BaseMessageHandler<TemplateActionMessage, TemplateActionMessageHandler>
    {
        #region Outbound

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <param name="item">
        /// </param>
        /// <param name="container">
        /// </param>
        /// <param name="placement">
        /// </param>
        /// <returns>
        /// </returns>
        private static MessageDataFiller Filler(ICharacter character, Item item, int container, int placement)
        {
            return x =>
            {
                x.Identity = character.Identity;

                // Zero in retail (20260909-141545 seq 731 and 2856). Left unset,
                // this went out as 1.
                x.Unknown = 0;
                x.ItemHighId = item.HighID;
                x.ItemLowId = item.LowID;
                x.Quality = item.Quality;
                x.Placement = new Identity() { Type = (IdentityType)container, Instance = placement };
                x.Unknown1 = 1;
                x.Unknown2 = 3;
                x.Unknown3 = 50000;
                x.Unknown4 = character.Identity.Instance;
            };
        }

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <param name="item">
        /// </param>
        /// <param name="container">
        /// </param>
        /// <param name="placement">
        /// </param>
        public void Send(ICharacter character, Item item, int container, int placement)
        {
            this.Send(character, Filler(character, item, container, placement));
        }

        /// <summary>
        /// An item arriving through the overflow window, as a package's contents
        /// do: Unknown2 87, placement OverflowWindow:0, Unknown3 and Unknown4
        /// zero, in every copy (20260911-171203_s12 seq 1590-1631, 20260909-141545
        /// seq 729 and 2848-2854).
        /// </summary>
        public void SendToOverflow(ICharacter character, Item item)
        {
            this.Send(
                character,
                x =>
                {
                    x.Identity = character.Identity;
                    x.Unknown = 0;
                    x.ItemLowId = item.LowID;
                    x.ItemHighId = item.HighID;
                    x.Quality = item.Quality;
                    x.Unknown1 = 1;
                    x.Unknown2 = 87;
                    x.Placement = new Identity { Type = IdentityType.OverflowWindow, Instance = 0 };
                    x.Unknown3 = 0;
                    x.Unknown4 = 0;
                });
        }

        #endregion
    }
}
