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

    using System.Collections.Generic;

    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;
    using OmniCell.Core.Items;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    #endregion

    /// <summary>
    /// </summary>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class KnuBotRejectedItemsMessageHandler :
        BaseMessageHandler<KnuBotRejectedItemsMessage, KnuBotRejectedItemsMessageHandler>
    {
        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <param name="knubotTarget">
        /// </param>
        /// <param name="items">
        /// </param>
        /// <summary>
        /// The fourth word of a rejected-item record, which the client's
        /// matcher never reads.
        /// </summary>
        /// <remarks>
        /// Both captured records carry 1234567890, this protocol's "not set"
        /// sentinel, so that is what this sends.
        /// </remarks>
        private const int UnsetSentinel = 1234567890;

        public void Send(ICharacter character, Identity knubotTarget, IEnumerable<Item> items)
        {
            this.Send(character, this.RejectedItems(character, knubotTarget, items), false);
        }

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <param name="knubotTarget">
        /// </param>
        /// <param name="items">
        /// </param>
        /// <returns>
        /// </returns>
        private MessageDataFiller RejectedItems(ICharacter character, Identity knubotTarget, IEnumerable<Item> items)
        {
            return x =>
            {
                x.Version = 2;
                x.Target = knubotTarget;
                x.Identity = character.Identity;
                List<KnuBotRejectedItem> temp = new List<KnuBotRejectedItem>();
                foreach (Item item in items)
                {
                    // The first three words are the item's two template ids and
                    // its quality, which is what this sent originally. It was
                    // changed to send nothing when the reader turned out to
                    // take the first eight bytes with the identity reader and
                    // no captured copy carried a record to argue with.
                    //
                    // The matcher settles it. At 0x10128628 the client asks the
                    // item for stats 702, 703 and 54 - acgitemtemplateid,
                    // acgitemtemplateid2 and level - and compares them against
                    // exactly these three words. The original was right.
                    temp.Add(
                        new KnuBotRejectedItem
                            {
                                ItemTemplateId = item.LowID,
                                ItemTemplateId2 = item.HighID,
                                QualityLevel = item.Quality,
                                Unused = UnsetSentinel
                            });
                }

                x.Items = temp.ToArray();
            };
        }
    }
}