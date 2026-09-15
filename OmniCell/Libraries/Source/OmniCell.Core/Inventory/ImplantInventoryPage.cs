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

namespace OmniCell.Core.Inventory
{
    #region Usings ...

    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Events;
    using OmniCell.Core.Functions;
    using OmniCell.Core.Items;
    using OmniCell.Core.Requirements;
    using OmniCell.Enums;

    using MsgPack;

    using SmokeLounge.AOtomation.Messaging.GameData;

    #endregion

    /// <summary>
    /// </summary>
    public class ImplantInventoryPage : BaseInventoryPage, IItemSlotHandler, IEquipmentPage
    {
        #region Constructors and Destructors

        /// <summary>
        /// </summary>
        /// <param name="ownerInstance">
        /// </param>
        public ImplantInventoryPage(Identity ownerInstance)
            : base((int)IdentityType.ImplantPage, 15, 0x21, ownerInstance)
        {
            this.NeedsItemCheck = true;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Whether an item may go in an implant slot: its Placement (stat 298) must have the bit
        /// for that slot. Slot 33 + n - 1 is bit n, eye 1 to feet 13 (the ImplantSlots values).
        /// </summary>
        /// <remarks>
        /// Every implant and spirit retail was seen equipping fits this: legs 2048 in slot 43,
        /// chest 32 in 37, right arm 16 in 36, waist 256 in 40, feet 8192 in 45
        /// (20260909-142713, 20260914-220505, 20260915-042412). A few implants carry two bits
        /// (both wrists 640, both arms 80) and fit either slot. Slots 46 and 47 take nothing.
        /// </remarks>
        public bool Fits(IItem item, int slot)
        {
            int bit = slot - this.FirstSlotNumber + 1;
            if ((item == null) || (bit < 1) || (bit > 13))
            {
                return false;
            }

            return (item.GetAttribute(298) & (1 << bit)) != 0;
        }

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        public override void CalculateModifiers(Character character)
        {
            for (int itemSlot = this.FirstSlotNumber; itemSlot < this.FirstSlotNumber + this.MaxSlots; itemSlot++)
            {
                IItem item = this[itemSlot];
                if (item != null)
                {
                    foreach (Event events in item.Events.Where(x => x.EventType == EventType.OnWear))
                    {
                        foreach (Function functions in events.Functions)
                        {
                            if (Requirement.CheckAll(functions.Requirements, character))
                            {
                                Function copy = functions.Copy();
                                MessagePackObject mpo = new MessagePackObject();
                                mpo = itemSlot;
                                copy.Arguments.Values.Add(mpo);
                                character.Controller.CallFunction(copy, character);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="statId">
        /// </param>
        /// <returns>
        /// </returns>
        public int Stat(int statId)
        {
            int value = 0;
            foreach (IItem item in this.List().Values)
            {
                value += item.GetAttribute(statId);
            }

            return value;
        }

        #endregion
    }
}