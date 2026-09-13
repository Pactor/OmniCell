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

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Playfields;

    #endregion

    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class VendingMachineFullUpdateMessageHandler :
        BaseMessageHandler<VendingMachineFullUpdateMessage, VendingMachineFullUpdateMessageHandler>
    {
        public void Send(ICharacter character, Vendor vendor)
        {
            this.Send(character, this.Filler(vendor));
        }

        private MessageDataFiller Filler(Vendor vendor)
        {
            return x =>
            {
                x.Identity = vendor.Identity;
                x.Unknown = 0;
                x.Coordinates = new Vector3()
                                {
                                    X = vendor.Coordinates().x,
                                    Y = vendor.Coordinates().y,
                                    Z = vendor.Coordinates().z
                                };

                x.Heading = new Quaternion()
                            {
                                X = vendor.Heading.xf,
                                Y = vendor.Heading.yf,
                                Z = vendor.Heading.zf,
                                W = vendor.Heading.wf
                            };

                // Whose shop it is. A machine's is None; a shopkeeper's is the
                // character, and it is how the client knows to open this stock
                // when that character is clicked rather than looking for
                // something to walk up to.
                x.NpcIdentity = vendor.NpcIdentity;
                x.TypeIdentifier = 0x0b;
                // The instance, like everything else the client is told about
                // this playfield. See PlayfieldAnarchyFMessageHandler.
                x.PlayfieldId = Playfields.GetClientInstance(vendor.Playfield.Identity.Instance);
                x.MarkerType = ItemMessageConstants.ItemMessageMarker;
                x.MarkerInstance = 0;
                // 111 for a machine, 64 for a shopkeeper's stock. Both are as
                // captured; the difference is that one is a thing in the world
                // and the other is carried.
                x.InventoryIdAndBodyLocation = (short)(vendor.NpcIdentity.Equals(Identity.None) ? 111 : 64);
                List<GameTuple<CharacterStat, uint>> temp = new List<GameTuple<CharacterStat, uint>>();
                Dictionary<int, uint> templist = vendor.Stats.GetStatValues();
                SortedDictionary<int, uint> templist2 = new SortedDictionary<int, uint>();
                foreach (KeyValuePair<int, uint> kv in templist)
                {
                    templist2.Add(kv.Key, kv.Value);
                }

                foreach (KeyValuePair<int, uint> stat in templist2)
                    /*foreach (IStat stat in vendor.Stats.All)
                {
                    if (stat.NotDefault())
                    {*/
                {
                    temp.Add(
                        new GameTuple<CharacterStat, uint>()
                        {
                            Value1 = (CharacterStat)stat.Key,
                            Value2 = (uint)stat.Value
                        });
                }
                /*}
                }*/
                x.Stats = temp.ToArray();
                x.Name = vendor.Name + "\0";
                x.TailVersion = 2;
                x.LockDifficulty = 50;
                x.Keyholders = new Identity[0];
                x.TailEndVersion = 3;
            };
        }
    }
}