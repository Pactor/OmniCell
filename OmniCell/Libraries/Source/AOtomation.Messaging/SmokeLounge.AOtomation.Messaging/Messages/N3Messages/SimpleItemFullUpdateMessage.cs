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

    [AoContract((int)N3MessageType.SimpleItemFullUpdate)]
    public class SimpleItemFullUpdateMessage : N3Message
    {

        public Identity Owner { get; set; }

        private int identityType;

        private int instance;

        public SimpleItemFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.SimpleItemFullUpdate;
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

        /// <summary>
        /// Zero is what makes the position fields present.
        /// </summary>
        /// <remarks>
        /// The gate is on the instance, not on the type beside it. In this
        /// message the two are always zero together - all 647 copies with a
        /// position have both - so it could not be told apart here, and it was
        /// the instance all along. VendingMachineFullUpdate, which is the same
        /// wire format, has 81 copies with a zero type and a non-zero instance
        /// and no position in them, and reading those with the type decided the
        /// question.
        /// </remarks>
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
        public Vector3 Coordinate { get; set; }

        [AoMember(5)]
        [AoUsesFlags("flag", typeof(Quaternion), FlagsCriteria.HasNone, new[] { int.MaxValue })]
        public Quaternion Heading { get; set; }

        // For items with owner (not dropped in world)
        [AoMember(6)]
        public int Playfield { get; set; }

        /// <summary>
        /// The shared item-message constant, which the client reads and drops.
        /// </summary>
        /// <remarks>
        /// Identity.Type is always <see cref="ItemMessageConstants.ItemMessageMarker"/>
        /// and Identity.Instance is always zero, in all 747 captured copies.
        ///
        /// The client does nothing with it whatsoever. Across the whole of
        /// SimpleItemFullUpdateIIR_t - Gamecode.dll 0x100A0F9E to 0x100A1C60,
        /// every method the class has - the only code that touches object
        /// offsets 0x4C and 0x50 is the constructor, which zeroes them, and the
        /// reader at 0x100A1703, which fills them. Nothing reads them back.
        ///
        /// So what the client does with it is settled, and only the server's
        /// reason for sending it is not. See ItemMessageConstants: three other
        /// messages carry the same value.
        /// </remarks>
        [AoMember(7)]
        public Identity Marker { get; set; }

        /// <summary>
        /// Which inventory the item is in. Stat 55, inventoryid.
        /// </summary>
        /// <remarks>
        /// Gamecode.dll 0x100A1B83 sign-extends this byte and writes it into
        /// stat 0x37 on the item it has just built. Signed, so the type here is
        /// wrong in principle and right in every captured copy - the values seen
        /// are 0, 1, 14 and 113.
        /// </remarks>
        [AoMember(8)]
        public byte InventoryId { get; set; }

        /// <summary>
        /// Where on the body or in the page it sits. Stat 220,
        /// currbodylocation.
        /// </summary>
        /// <remarks>
        /// Gamecode.dll 0x100A1B94 sign-extends this byte and writes it into
        /// stat 0xDC, immediately after the inventory id above. The captures
        /// carry 111, 73, 67 and 72.
        /// </remarks>
        [AoMember(9)]
        public byte BodyLocation { get; set; }

        // 3f1
        [AoMember(10, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<CharacterStat, uint>[] Stats { get; set; }

        [AoMember(11, SerializeSize = ArraySizeType.Int32Terminated)]
        public string Name { get; set; }

    }
}