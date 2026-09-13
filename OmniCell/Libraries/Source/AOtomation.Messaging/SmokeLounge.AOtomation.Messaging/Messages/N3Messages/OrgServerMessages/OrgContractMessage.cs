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

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages.OrgServerMessages
{
    #region Usings ...

    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    #endregion

    [AoContract((byte)OrgServerMessageType.OrgContract)]
    public class OrgContractMessage : OrgServerMessage
    {
        public OrgContractMessage()
        {
            this.OrgServerMessageType = OrgServerMessageType.OrgContract;
        }

        /// <summary>
        /// The contract item's quality.
        /// </summary>
        /// <remarks>
        /// This was a short, with the two bytes in front of it eaten by the
        /// base class's organization name. It is a full int32: the branch at
        /// Gamecode 0x10126E13 reads three int32s in the order quality, low id,
        /// high id and hands them to GameData::ACGItem_t::ACGItem_t(unsigned
        /// int, unsigned int, int) as (low, high, quality), which is the same
        /// item record MarketSend and Mail carry.
        /// </remarks>
        [AoMember(1)]
        public int Quality { get; set; }

        [AoMember(2)]
        public int ItemLowId { get; set; }

        [AoMember(3)]
        public int ItemHighId { get; set; }

        /// <summary>
        /// A byte the reader normalises to 0 or 1.
        /// </summary>
        /// <remarks>
        /// Read through 0x100A0138, the same helper DoorStatusUpdate's flags go
        /// through, and stored at the message's +0x108.
        /// </remarks>
        [AoMember(4)]
        public byte Active { get; set; }
    }
}