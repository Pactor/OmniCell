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

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    #region Usings ...

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    #endregion

    [AoContract((int)N3MessageType.ChestItemFullUpdate)]
    public class ChestItemFullUpdateMessage : N3Message
    {
        public Identity Owner { get; set; }

        private int identityType;

        private int instance;

        public ChestItemFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.ChestItemFullUpdate;
        }

        [AoMember(1)]
        public int MsgVersion { get; set; }

        [AoMember(2)]
        public int Identitytype
        {
            get
            {
                return this.identityType;
            }
            set
            {
                this.identityType = value;
                this.Owner = new Identity() { Type = (IdentityType)value, Instance = this.instance };
            }
        }

        [AoMember(3)]
        [AoFlags("flag")]
        public int Instance
        {
            get
            {
                return this.instance;
            }
            set
            {
                this.instance = value;
                this.Owner = new Identity() { Type = (IdentityType)this.identityType, Instance = value };
            }
        }

        [AoMember(4)]
        [AoUsesFlags("flag", typeof(Vector3), FlagsCriteria.HasNone, new[] { int.MaxValue })]
        public Vector3 Coordinates { get; set; }

        [AoMember(5)]
        [AoUsesFlags("flag", typeof(Quaternion), FlagsCriteria.HasNone, new[] { int.MaxValue })]
        public Quaternion Heading { get; set; }

        /// <summary>
        /// Which playfield the chest is in.
        /// </summary>
        /// <remarks>
        /// The same two values VendingMachineFullUpdate carries in the same
        /// place, 655 and 2150461, which is unsurprising now that the two are
        /// known to be one wire format.
        /// </remarks>
        [AoMember(6)]
        public int PlayfieldId { get; set; }

        /// <summary>
        /// The shared item-message constant, 1000015, with a zero instance.
        /// </summary>
        /// <remarks>
        /// See ItemMessageConstants. In SimpleItemFullUpdate - which this
        /// message is built on - the client reads this Identity and never reads
        /// it back anywhere in the class.
        /// </remarks>
        [AoMember(7)]
        public Identity Marker { get; set; }

        /// <summary>
        /// Which inventory the chest is in. Stat 55, inventoryid.
        /// </summary>
        /// <remarks>
        /// Gamecode.dll 0x100A1B83 sign-extends this byte into stat 0x37. The
        /// captured values are 1, 3, 14 and 113, which are inventory ids.
        /// </remarks>
        [AoMember(8)]
        public byte InventoryId { get; set; }
        /// <summary>
        /// Where it sits. Stat 220, currbodylocation.
        /// </summary>
        /// <remarks>
        /// Gamecode.dll 0x100A1B94 sign-extends this byte into stat 0xDC,
        /// immediately after the inventory id above.
        /// </remarks>
        [AoMember(9)]
        public byte BodyLocation { get; set; }

        [AoMember(10, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<CharacterStat, uint>[] Stats { get; set; }

        [AoMember(12, SerializeSize = ArraySizeType.Int32Terminated)]
        public string Name { get; set; }

        /// <summary>
        /// Two in every captured copy.
        /// </summary>
        /// <remarks>
        /// Nothing read the sixteen bytes that close this message until
        /// 2026-09-10, so every corpse container went out that much short.
        ///
        /// The client checks it against the static at Gamecode.dll 0x101C2084,
        /// which holds 2, and abandons the message if it differs; the 3 at the
        /// end is the same thing against 0x101C1F80, and the 50 between them is
        /// stat 299, lockdifficulty.
        ///
        /// The one copy that differs is not a different tail. Its Name reads as
        /// two characters where every other is empty, which walks the reader
        /// into the tail and out the other side; it wants looking at on its own.
        /// </remarks>
        [AoMember(13)]
        public int TailVersion { get; set; }

        /// <summary>
        /// Stat 299, lockdifficulty. 50 in every captured copy.
        /// </summary>
        /// <remarks>
        /// Gamecode.dll 0x100A0CBA reads this int32 into
        /// **(message + 0x60) + 0x4AC. That address resolves: message + 0x60
        /// holds the stat table the message fills in, built at 0x10009D97 as a
        /// vector of 1,200 ints every one of which is seeded with 1234567890 -
        /// the stat unset sentinel. The table is indexed by stat id directly,
        /// so a byte offset of 0x4AC is element 299.
        ///
        /// Stat 299 is lockdifficulty, which is exactly what a container has,
        /// and 50 is exactly what one looks like.
        /// </remarks>
        [AoMember(14)]
        public int LockDifficulty { get; set; }

        /// <summary>
        /// Who can lock and unlock this. Empty in every captured copy.
        /// </summary>
        /// <remarks>
        /// The list belongs to the class this message inherits from, and the
        /// class says what it is for. LockableItemFullUpdateIIR_t::ReadSubClass
        /// at Gamecode.dll 0x100A0C87 reads it - the three fields it adds are a
        /// version, lockdifficulty and this - and its dispatcher at 0x100A0BEF
        /// casts the target to a LockableItem_t and adds every entry to the
        /// object's own list at +0x98.
        ///
        /// That list is read back at 0x10085890, which walks it comparing each
        /// entry against the identity of whatever is acting on the item, and on
        /// a match toggles the lock and prints Feedback_YouUnlockedTheItem or
        /// Feedback_YouLockedTheItem. So an entry here is an identity that may
        /// open the thing.
        ///
        /// Nothing has ever been seen in one, so the count is all the captures
        /// prove; the element width is the client's Identity reader at
        /// 0x1013D2F9, which is what 0x1002BAC0 calls for each entry.
        /// </remarks>
        [AoMember(15, SerializeSize = ArraySizeType.X3F1)]
        public Identity[] Keyholders { get; set; }

        /// <summary>
        /// A third version, 3, and the reader accepts nothing else.
        /// </summary>
        /// <remarks>
        /// The last field of the message and the last of three version checks
        /// in it - 11 at the front, 2 opening the lockable tail, and this one.
        /// ChestFullUpdateIIR_t::ReadSubClass at Gamecode.dll 0x1009F88A calls
        /// the lockable reader and then reads this, comparing it against the
        /// static at 0x101C1F80; a mismatch abandons the message.
        /// </remarks>
        [AoMember(16)]
        public int TailEndVersion { get; set; }
    }
}