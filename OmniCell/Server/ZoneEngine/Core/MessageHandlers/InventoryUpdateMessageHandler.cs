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
    using OmniCell.Core.Inventory;
    using OmniCell.Core.Items;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Loot;

    #endregion

    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class InventoryUpdateMessageHandler :
        BaseMessageHandler<InventoryUpdateMessage, InventoryUpdateMessageHandler>
    {
        public void Send(ICharacter character, IInventoryPage page)
        {
            this.Send(character, this.FillData(character, page));
        }

        /// <summary>
        /// Opens a corpse with the take-only shape established by the captures.
        /// </summary>
        public void SendForCorpse(ICharacter character, CorpseLoot corpse, int virtualSlot)
        {
            this.Send(character, this.FillCorpseData(character, corpse, virtualSlot));
        }

        /// <summary>
        /// Opens a player-held bag: the shape a real bag open takes on the wire.
        /// </summary>
        /// <remarks>
        /// From the captures (work/decoded/20260923-201448 and 201746): a bag
        /// answers with BagIdentity = its own Container identity, NumberOfSlots
        /// 21, Access CanAdd|CanRemove, Open 1, and SlotnumberInMainInventory =
        /// the handle. The handle is a per-character counter that starts at 112
        /// and climbs one per open; it is the high half of every slot inside the
        /// bag, Backpack (handle &lt;&lt; 16) | slot. Handle 0 is the empty shell
        /// the client is given before a bag has been opened. <paramref
        /// name="contents"/> is null for a bag with nothing in it yet.
        /// </remarks>
        public void SendForBag(ICharacter character, Identity bagIdentity, int handle, IInventoryPage contents)
        {
            this.Send(character, x =>
            {
                x.BagIdentity = bagIdentity;
                x.NumberOfSlots = 21;
                x.SlotnumberInMainInventory = handle;

                var entries = new List<InventoryEntry>();
                if (contents != null)
                {
                    foreach (KeyValuePair<int, IItem> kv in contents.List())
                    {
                        entries.Add(
                            new InventoryEntry
                            {
                                Slotnumber = kv.Key,
                                Identity = Identity.None,
                                Quality = kv.Value.Quality,
                                HighId = kv.Value.HighID,
                                LowId = kv.Value.LowID,
                                Flags = 0x21,
                                Count = (short)kv.Value.MultipleCount,
                                Unused = 0
                            });
                    }
                }

                x.Entries = entries.ToArray();
                x.Open = 1;
                x.Access = InventoryAccess.CanAdd | InventoryAccess.CanRemove;
                x.Identity = character.Identity;
                x.Unknown = 1;
            });
        }

        /// <summary>
        /// A mission chest is not quite a corpse.
        /// </summary>
        /// <remarks>
        /// Same message and same slot count, two fields apart. The chests
        /// opened in the 2026-09-28 recording carry Access CanAdd and
        /// CanRemove where a corpse carries CanRemove only - you can put
        /// something back in a box - and their entries read Flags 33 against
        /// a corpse's 161.
        /// </remarks>
        private const short ChestItemFlags = 0x0021;

        /// <summary>
        /// The identity type a mission chest is sent as.
        /// </summary>
        private const int MissionChestType = 51017;

        public MessageDataFiller FillCorpseData(ICharacter character, CorpseLoot corpse, int virtualSlot)
        {
            bool chest = (int)corpse.Identity.Type == MissionChestType;
            return x =>
                {
                    IInventoryPage page = corpse.BaseInventory[corpse.BaseInventory.StandardPage];
                    x.BagIdentity = corpse.Identity;
                    x.NumberOfSlots = 21;
                    x.SlotnumberInMainInventory = virtualSlot;

                    var entries = new List<InventoryEntry>();
                    foreach (KeyValuePair<int, IItem> kv in page.List())
                    {
                        entries.Add(
                            new InventoryEntry
                                {
                                    Slotnumber = kv.Key,
                                    Identity = Identity.None,
                                    Quality = kv.Value.Quality,
                                    HighId = kv.Value.HighID,
                                    LowId = kv.Value.LowID,
                                    Flags = chest ? ChestItemFlags : unchecked((short)0x00A1),
                                    Count = (short)kv.Value.MultipleCount,
                                    Unused = 0
                                });
                    }

                    x.Entries = entries.ToArray();
                    x.Open = 1;
                    x.Access = chest
                                   ? InventoryAccess.CanAdd | InventoryAccess.CanRemove
                                   : InventoryAccess.CanRemove;
                    x.Identity = character.Identity;
                    x.Unknown = 1;
                };
        }

        public MessageDataFiller FillData(ICharacter character, IInventoryPage page)
        {
            return x =>
            {
                x.BagIdentity = page.Identity;
                x.NumberOfSlots = page.MaxSlots;
                x.SlotnumberInMainInventory = 0;
                List<InventoryEntry> temp = new List<InventoryEntry>();

                foreach (KeyValuePair<int, IItem> kv in page.List())
                {
                    temp.Add(
                        new InventoryEntry()
                        {
                            Slotnumber = kv.Key,
                            Identity = Identity.None,
                            Quality = kv.Value.Quality,
                            HighId = kv.Value.HighID,
                            LowId = kv.Value.LowID,
                            Flags = 0x21,
                            Count = (short)kv.Value.MultipleCount,
                            Unused = 0
                        });
                }
                x.Entries = temp.ToArray();
                x.Open = 1;
                x.Access = InventoryAccess.CanAdd | InventoryAccess.CanRemove;
                x.Identity = character.Identity;
                x.Unknown = 1;
            };
        }
    }
}
