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

    [AoContract((int)N3MessageType.VendingMachineFullUpdate)]
    public class VendingMachineFullUpdateMessage : N3Message
    {
        #region Constructors and Destructors

        public VendingMachineFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.VendingMachineFullUpdate;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// A version, 11, that the client refuses the message without.
        /// </summary>
        /// <remarks>
        /// This message is a SimpleItemFullUpdate with a tail on it - its
        /// reader calls SimpleItemFullUpdateIIR_t::ReadSubClass at Gamecode.dll
        /// 0x100A1661 and then reads four more fields - so everything down to
        /// Name below is that message, field for field, and this is its version
        /// check against the static at 0x101C2160, which holds 11.
        /// </remarks>
        [AoMember(1)]
        public int TypeIdentifier { get; set; }

        // If NpcIdentity is Identity.None then dont serialize/deserialize Coordinates and Heading
        [AoMember(2)]
        public Identity NpcIdentity { get; set; }

        /// <summary>
        /// Present only when NpcIdentity.Instance is zero.
        /// </summary>
        /// <remarks>
        /// The instance, not the type beside it, and this message is where
        /// that was decided.
        ///
        /// Coordinates appear in exactly 281 of 690 copies, which is exactly
        /// the number whose NpcIdentity.Instance is zero. 362 have a zero type,
        /// and that is not the same set - so the two rules disagree about 81
        /// copies, and those 81 settle it. Read one of them with the type: a
        /// captured message with type 0 and instance 0x57371D is 143 bytes,
        /// which is this layout with no position in it and twenty eight bytes
        /// short of what it gives with one, and parsing it the other way puts
        /// 0xD8CC0000 where the marker constant has to be.
        ///
        /// ChestItemFullUpdate and SimpleItemFullUpdate are the same wire
        /// format and were both reading the type. Moving them onto the instance
        /// recovered thirty seven packets that had not been parseable at all.
        ///
        /// The listing still reads the other way to me - SimpleItemFullUpdate's
        /// reader tests the first of the identity's two words at Gamecode.dll
        /// 0x100A1696 - so there is something about it I have not understood.
        /// The bytes are not in doubt.
        /// </remarks>
        [AoMember(3)]
        public Vector3 Coordinates { get; set; }

        [AoMember(4)]
        public Quaternion Heading { get; set; }

        [AoMember(5)]
        public int PlayfieldId { get; set; }

        /// <summary>
        /// Always ItemMessageConstants.ItemMessageMarker.
        /// </summary>
        /// <remarks>
        /// The same 1,000,015 that three other item messages carry. See
        /// ItemMessageConstants, which says how much evidence there is and how
        /// little meaning.
        /// </remarks>
        /// <summary>
        /// The shared item-message constant and its always-zero instance.
        /// </summary>
        /// <remarks>
        /// The same Identity SimpleItemFullUpdate carries, written down here as
        /// two ints because that is how it was found. See ItemMessageConstants:
        /// the client reads it and never reads it back.
        /// </remarks>
        [AoMember(6)]
        public int MarkerType { get; set; }

        [AoMember(7)]
        public int MarkerInstance { get; set; }

        /// <summary>
        /// Two bytes: the inventory id and the body location.
        /// </summary>
        /// <remarks>
        /// Modelled as a short because that is how it was found, and it stays
        /// one because the two bytes have never been anything but 0 and a
        /// placement - 64, 65 and 111 across 690 captured copies, which is 0x40,
        /// 0x41 and 0x6F with a zero high byte.
        ///
        /// They are the same pair SimpleItemFullUpdate carries, and the same
        /// code reads them: Gamecode.dll 0x100A1B83 sign-extends the first into
        /// stat 55, inventoryid, and 0x100A1B94 the second into stat 220,
        /// currbodylocation.
        /// </remarks>
        [AoMember(8)]
        public short InventoryIdAndBodyLocation { get; set; }

        [AoMember(9, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<CharacterStat, uint>[] Stats { get; set; }

        /// <summary>
        /// The machine's name. Empty in every captured copy.
        /// </summary>
        [AoMember(10, SerializeSize = ArraySizeType.Int32Terminated)]
        public string Name { get; set; }

        /// <summary>
        /// A second version, 2, checked against the static at 0x101C2084.
        /// </summary>
        /// <remarks>
        /// The tail this message adds to SimpleItemFullUpdate opens and closes
        /// with a version check. A mismatch here abandons the message.
        /// </remarks>
        [AoMember(11)]
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
        [AoMember(12)]
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
        [AoMember(13, SerializeSize = ArraySizeType.X3F1)]
        public Identity[] Keyholders { get; set; }

        /// <summary>
        /// A third version, 3, checked against the static at 0x101C1F80.
        /// </summary>
        /// <remarks>
        /// The last field of the message and the last of three version checks
        /// in it - 11 at the front, 2 opening the tail, and 3 closing it. Each
        /// one abandons the message on a mismatch.
        /// </remarks>
        [AoMember(14)]
        public int TailEndVersion { get; set; }

        #endregion
    }
}